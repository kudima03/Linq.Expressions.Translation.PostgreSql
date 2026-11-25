using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// The in-memory side of the PostgreSQL markers, with PostgreSQL's semantics, which
// InMemory puts in their place: Over and WithRecursive over sources that read no
// lambda parameter become their rows, computed when they are read, and the
// statistics of PostgreSqlFunctions become ReferenceStatistics.
public static partial class Reference
{
    public static Expression? Rewrite(
        MethodCallExpression call,
        Func<Expression, Expression> visit
    )
    {
        Type? owner = call.Method.DeclaringType;
        if (owner == typeof(PostgreSqlFunctions))
        {
            MethodInfo method = typeof(ReferenceStatistics)
                .GetMethods()
                .Single(candidate =>
                    candidate.Name == call.Method.Name
                    && candidate
                        .GetParameters()
                        .Select(p => p.ParameterType.Name)
                        .SequenceEqual(
                            call.Method.GetGenericMethodDefinition()
                                .GetParameters()
                                .Select(p => p.ParameterType.Name)
                        )
                    && candidate.ReturnType
                        == call.Method.GetGenericMethodDefinition().ReturnType
                )
                .MakeGenericMethod(call.Method.GetGenericArguments());
            return Expression.Call(method, call.Arguments.Select(visit));
        }
        if (
            owner == typeof(PostgreSqlWindows)
            && call.Method.Name == nameof(PostgreSqlWindows.Over)
        )
        {
            Type[] types = call.Method.GetGenericArguments();
            return (Expression)
                typeof(Reference)
                    .GetMethod(
                        nameof(Over),
                        BindingFlags.NonPublic | BindingFlags.Static
                    )!
                    .MakeGenericMethod(types)
                    .Invoke(null, [call, visit])!;
        }
        return owner == typeof(PostgreSqlRecursion)
            ? (Expression)
                typeof(Reference)
                    .GetMethod(
                        nameof(Recursive),
                        BindingFlags.NonPublic | BindingFlags.Static
                    )!
                    .MakeGenericMethod(call.Method.GetGenericArguments())
                    .Invoke(null, [call, visit])!
            : null;
    }

    private static LambdaExpression? Unquoted(Expression argument)
    {
        return argument switch
        {
            UnaryExpression { NodeType: ExpressionType.Quote } quote => (LambdaExpression)
                quote.Operand,
            LambdaExpression lambda => lambda,
            _ => null,
        };
    }

    private static List<T> Rows<T>(Expression source)
    {
        return [.. new EnumerableQuery<T>(source)];
    }

    // ------------------------------------------------------------------ recursion

    private static Expression Recursive<T>(
        MethodCallExpression call,
        Func<Expression, Expression> visit
    )
    {
        Expression seed = visit(call.Arguments[0]);
        LambdaExpression step = Unquoted(call.Arguments[1])!;
        Expression body = visit(step.Body);
        long? limit =
            call.Arguments.Count > 2
                ? (long)((ConstantExpression)call.Arguments[2]).Value!
                : null;
        IEnumerable<T> Levels()
        {
            List<T> level = Rows<T>(seed);
            for (long depth = 0; level.Count > 0; depth++)
            {
                if (depth > limit)
                {
                    throw new InvalidOperationException(
                        $"WITH RECURSIVE: a row deeper than the maximum depth of {limit}"
                    );
                }
                foreach (T row in level)
                {
                    yield return row;
                }
                Expression next = new Replacer(
                    step.Parameters[0],
                    Expression.Constant(level.AsQueryable(), typeof(IQueryable<T>))
                ).Visit(body);
                level = Rows<T>(next);
            }
        }
        return Expression.Constant(
            new EnumerableQuery<T>(new Deferred<T>(() => [.. Levels()])),
            typeof(IQueryable<T>)
        );
    }

    private sealed class Deferred<T>(Func<List<T>> rows) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            return rows().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class Replacer(ParameterExpression parameter, Expression value)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == parameter ? value : node;
        }
    }

    // -------------------------------------------------------------------- windows

    private static Expression Over<TSource, TResult>(
        MethodCallExpression call,
        Func<Expression, Expression> visit
    )
    {
        Expression source = visit(call.Arguments[0]);
        Func<TSource, object?> partition = Unquoted(call.Arguments[1])
            is LambdaExpression by
            ? ((Expression<Func<TSource, object?>>)visit(by)).Compile()
            : _ => 0;
        LambdaExpression? orderBy = Unquoted(call.Arguments[2]);
        Func<IEnumerable<TSource>, IEnumerable<TSource>> sort = orderBy is null
            ? rows => rows
            : (
                (Expression<Func<IEnumerable<TSource>, IOrderedEnumerable<TSource>>>)
                    visit(orderBy)
            ).Compile();
        List<(Func<TSource, object?> Key, bool Descending)> keys = orderBy is null
            ? []
            : Keys<TSource>(orderBy, visit);
        Frame? frame = call.Arguments[3]
            is ConstantExpression { Value: PostgreSqlFrame value }
            ? Frame.Parse(value.TextValue)
            : null;
        LambdaExpression selector = Unquoted(call.Arguments[4])!;
        ParameterExpression window = Expression.Parameter(
            typeof(MemoryWindow<TSource>),
            "window"
        );
        Func<TSource, MemoryWindow<TSource>, TResult> select = Expression
            .Lambda<Func<TSource, MemoryWindow<TSource>, TResult>>(
                new Windowed(selector.Parameters[1], window).Visit(visit(selector.Body)),
                selector.Parameters[0],
                window
            )
            .Compile();
        List<TResult> Computed(List<TSource> rows)
        {
            TResult[] results = new TResult[rows.Count];
            bool[] assigned = new bool[rows.Count];
            foreach (IGrouping<object?, TSource> group in rows.GroupBy(partition))
            {
                List<TSource> sorted = [.. sort(group)];
                object?[][] peers =
                [
                    .. sorted.Select(row => keys.Select(key => key.Key(row)).ToArray()),
                ];
                int[] peerStart = new int[sorted.Count];
                int[] peerEnd = new int[sorted.Count];
                int[] groups = new int[sorted.Count];
                for (int index = 0, start = 0, count = 0; index < sorted.Count; index++)
                {
                    if (index > 0 && !peers[index].SequenceEqual(peers[index - 1]))
                    {
                        start = index;
                        count++;
                    }
                    peerStart[index] = start;
                    groups[index] = count;
                }
                for (
                    int index = sorted.Count - 1, end = sorted.Count;
                    index >= 0;
                    index--
                )
                {
                    peerEnd[index] = end;
                    if (peerStart[index] == index)
                    {
                        end = index;
                    }
                }
                for (int index = 0; index < sorted.Count; index++)
                {
                    (int first, int last) = frame is null
                        ? (0, keys.Count == 0 ? sorted.Count - 1 : peerEnd[index] - 1)
                        : frame.Bounds(
                            index,
                            sorted.Count,
                            peerStart,
                            peerEnd,
                            groups,
                            peers,
                            keys
                        );
                    // The source row of a sorted one: an equal row not yet assigned.
                    int position = Enumerable
                        .Range(0, rows.Count)
                        .First(candidate =>
                            !assigned[candidate]
                            && EqualityComparer<TSource>.Default.Equals(
                                rows[candidate],
                                sorted[index]
                            )
                        );
                    assigned[position] = true;
                    results[position] = select(
                        sorted[index],
                        new MemoryWindow<TSource>(
                            sorted,
                            index,
                            peerStart,
                            peerEnd,
                            groups,
                            first,
                            last
                        )
                    );
                }
            }
            return [.. results];
        }
        // The source may read the parameter of an enclosing lambda: the rows are
        // computed from it where the tree is run.
        return Expression.Call(
            Expression.Constant(new Computation<TSource, TResult>(Computed)),
            nameof(Computation<,>.Run),
            null,
            Expression.Convert(source, typeof(IEnumerable<TSource>))
        );
    }

    public sealed class Computation<TSource, TResult>(
        Func<List<TSource>, List<TResult>> compute
    )
    {
        public IQueryable<TResult> Run(IEnumerable<TSource> rows)
        {
            return compute([.. rows]).AsQueryable();
        }
    }

    // The keys of OrderBy and ThenBy, in order, each with its direction.
    private static List<(Func<T, object?> Key, bool Descending)> Keys<T>(
        LambdaExpression orderBy,
        Func<Expression, Expression> visit
    )
    {
        List<(Func<T, object?>, bool)> keys = [];
        Expression body = orderBy.Body;
        while (body is MethodCallExpression call)
        {
            bool descending = call.Method.Name.EndsWith(
                "Descending",
                StringComparison.Ordinal
            );
            Func<T, object?> key =
                call.Method.Name.StartsWith("Order", StringComparison.Ordinal)
                && !call.Method.Name.StartsWith("OrderBy", StringComparison.Ordinal)
                    ? row => row
                    : Key<T>((LambdaExpression)call.Arguments[1], visit);
            keys.Insert(0, (key, descending));
            body = call.Arguments[0];
        }
        return keys;
    }

    private static Func<T, object?> Key<T>(
        LambdaExpression lambda,
        Func<Expression, Expression> visit
    )
    {
        return Expression
            .Lambda<Func<T, object?>>(
                Expression.Convert(visit(lambda.Body), typeof(object)),
                lambda.Parameters
            )
            .Compile();
    }

    // The window parameter becomes a MemoryWindow, its functions its methods.
    private sealed class Windowed(ParameterExpression window, ParameterExpression memory)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == window ? memory : node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return node.Expression == window
                ? Expression.Property(memory, node.Member.Name)
                : base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType != typeof(PostgreSqlWindows))
            {
                return base.VisitMethodCall(node);
            }
            MethodInfo method = memory
                .Type.GetMethods()
                .Single(candidate =>
                    candidate.Name == node.Method.Name
                    && candidate.GetParameters().Length == node.Arguments.Count - 1
                );
            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(node.Method.GetGenericArguments()[1]);
            }
            return Expression.Call(memory, method, node.Arguments.Skip(1).Select(Visit)!);
        }
    }

    // A frame clause as PostgreSqlFrame writes it.
    private sealed partial class Frame
    {
        private readonly string _unit;

        private readonly Bound _start;

        private readonly Bound _end;

        private Frame(string unit, Bound start, Bound end)
        {
            _unit = unit;
            _start = start;
            _end = end;
        }

        public static Frame Parse(string text)
        {
            Match match = Clause().Match(text);
            return new Frame(
                match.Groups["unit"].Value,
                Bound.Parse(match.Groups["start"].Value),
                Bound.Parse(match.Groups["end"].Value)
            );
        }

        [GeneratedRegex(
            "^(?<unit>ROWS|RANGE|GROUPS) BETWEEN (?<start>.+) AND (?<end>.+)$"
        )]
        private static partial Regex Clause();

        public (int First, int Last) Bounds<T>(
            int index,
            int count,
            int[] peerStart,
            int[] peerEnd,
            int[] groups,
            object?[][] peers,
            List<(Func<T, object?> Key, bool Descending)> keys
        )
        {
            return _unit switch
            {
                "ROWS" => (
                    Math.Max(_start.Rows(index, count), 0),
                    Math.Min(_end.Rows(index, count), count - 1)
                ),
                "GROUPS" => (
                    _start.GroupStart(index, peerStart, groups, count),
                    _end.GroupEnd(index, peerEnd, groups, count)
                ),
                _ => (
                    _start.RangeStart(
                        index,
                        peerStart,
                        peers,
                        keys.FirstOrDefault().Descending
                    ),
                    _end.RangeEnd(index, peerEnd, peers, keys.FirstOrDefault().Descending)
                ),
            };
        }
    }

    private sealed partial class Bound
    {
        // -2 UNBOUNDED PRECEDING, -1 PRECEDING, 0 CURRENT ROW, 1 FOLLOWING, 2 UNBOUNDED.
        private readonly int _side;

        private readonly decimal _number;

        private readonly TimeSpan? _span;

        private readonly int? _months;

        private Bound(int side, decimal number, TimeSpan? span, int? months)
        {
            _side = side;
            _number = number;
            _span = span;
            _months = months;
        }

        public static Bound Parse(string text)
        {
            if (text == "UNBOUNDED PRECEDING")
            {
                return new Bound(-2, 0, null, null);
            }
            if (text == "UNBOUNDED FOLLOWING")
            {
                return new Bound(2, 0, null, null);
            }
            if (text == "CURRENT ROW")
            {
                return new Bound(0, 0, null, null);
            }
            int side = text.EndsWith(" PRECEDING", StringComparison.Ordinal) ? -1 : 1;
            string distance = text[..text.LastIndexOf(' ')];
            Match interval = Interval().Match(distance);
            if (!interval.Success)
            {
                return new Bound(
                    side,
                    decimal.Parse(distance, CultureInfo.InvariantCulture),
                    null,
                    null
                );
            }
            decimal amount = decimal.Parse(
                interval.Groups["amount"].Value,
                CultureInfo.InvariantCulture
            );
            return interval.Groups["unit"].Value == "months"
                ? new Bound(side, 0, null, (int)amount)
                : new Bound(
                    side,
                    0,
                    TimeSpan.FromTicks((long)(amount * TimeSpan.TicksPerSecond)),
                    null
                );
        }

        [GeneratedRegex("^INTERVAL '(?<amount>[0-9.]+) (?<unit>seconds|months)'$")]
        private static partial Regex Interval();

        public int Rows(int index, int count)
        {
            return _side switch
            {
                -2 => 0,
                2 => count - 1,
                0 => index,
                _ => (int)Math.Clamp(index + (_side * _number), -1, count),
            };
        }

        public int GroupStart(int index, int[] peerStart, int[] groups, int count)
        {
            if (_side == -2)
            {
                return 0;
            }
            if (_side == 0)
            {
                return peerStart[index];
            }
            long target = groups[index] + (_side * (long)_number);
            int first = Array.FindIndex(groups, group => group >= target);
            return first < 0 ? count : first;
        }

        public int GroupEnd(int index, int[] peerEnd, int[] groups, int count)
        {
            if (_side == 2)
            {
                return count - 1;
            }
            if (_side == 0)
            {
                return peerEnd[index] - 1;
            }
            long target = groups[index] + (_side * (long)_number);
            return Array.FindLastIndex(groups, group => group <= target);
        }

        // Rows sort by the key, NULL before every value ascending and after every value
        // descending; a row whose key is NULL has its peers for an offset bound.
        public int RangeStart(
            int index,
            int[] peerStart,
            object?[][] peers,
            bool descending
        )
        {
            if (_side == -2)
            {
                return 0;
            }
            object? key = _side == 0 ? null : peers[index][0];
            if (key is null)
            {
                return peerStart[index];
            }
            int sign = descending ? -1 : 1;
            object? bound = Moved(key, _side * sign);
            if (bound is null)
            {
                return _side < 0 ? 0 : peers.Length;
            }
            int first = Array.FindIndex(peers, row => sign * Compare(row[0], bound) >= 0);
            return first < 0 ? peers.Length : first;
        }

        public int RangeEnd(int index, int[] peerEnd, object?[][] peers, bool descending)
        {
            if (_side == 2)
            {
                return peers.Length - 1;
            }
            object? key = _side == 0 ? null : peers[index][0];
            if (key is null)
            {
                return peerEnd[index] - 1;
            }
            int sign = descending ? -1 : 1;
            object? bound = Moved(key, _side * sign);
            return bound is null
                ? (_side > 0 ? peers.Length - 1 : -1)
                : Array.FindLastIndex(peers, row => sign * Compare(row[0], bound) <= 0);
        }

        // The key moved by the distance towards greater (1) or smaller (-1) values;
        // null beyond the range of its type.
        private object? Moved(object key, int direction)
        {
            try
            {
                return key switch
                {
                    double number => number + (direction * (double)_number),
                    float number => (double)number + (direction * (double)_number),
                    DateTime moment => _months is int months
                        ? moment.AddMonths(direction * months)
                        : moment + (direction * _span!.Value),
                    DateOnly day => _months is int months
                        ? day.ToDateTime(TimeOnly.MinValue).AddMonths(direction * months)
                        : day.ToDateTime(TimeOnly.MinValue) + (direction * _span!.Value),
                    TimeOnly time => time.ToTimeSpan() + (direction * _span!.Value),
                    TimeSpan span => span + (direction * _span!.Value),
                    _ => Convert.ToDecimal(key, CultureInfo.InvariantCulture)
                        + (direction * _number),
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        // NULL is less than every value.
        private static int Compare(object? value, object bound)
        {
            return value switch
            {
                null => -1,
                double or float => Convert
                    .ToDouble(value, CultureInfo.InvariantCulture)
                    .CompareTo((double)bound),
                DateTime moment => moment.CompareTo((DateTime)bound),
                DateOnly day => day.ToDateTime(TimeOnly.MinValue)
                    .CompareTo((DateTime)bound),
                TimeOnly time => time.ToTimeSpan().CompareTo((TimeSpan)bound),
                TimeSpan span => span.CompareTo((TimeSpan)bound),
                _ => Convert
                    .ToDecimal(value, CultureInfo.InvariantCulture)
                    .CompareTo((decimal)bound),
            };
        }
    }
}
