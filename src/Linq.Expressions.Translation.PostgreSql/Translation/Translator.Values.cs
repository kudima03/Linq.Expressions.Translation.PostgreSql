using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    // The column types of VALUES other than numbers, which ScalarTypes names.
    private static readonly Dictionary<Type, string> ValueTypes = new()
    {
        [typeof(bool)] = "boolean",
        [typeof(string)] = "text",
        [typeof(Guid)] = "uuid",
        [typeof(DateTime)] = "timestamp",
        [typeof(DateTimeOffset)] = "timestamptz",
        [typeof(DateOnly)] = "date",
        [typeof(TimeOnly)] = "time",
        [typeof(TimeSpan)] = "interval",
        [typeof(byte[])] = "bytea",
    };

    // The literals that are of their column type as they are written.
    private static readonly HashSet<Type> SelfTyped =
    [
        typeof(bool),
        typeof(int),
        typeof(Guid),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(TimeSpan),
        typeof(byte[]),
    ];

    private static bool IsInMemory(Expression expression)
    {
        object? value = (expression as ConstantExpression)?.Value;
        return value is IEnumerable and not IQueryable and not string;
    }

    // An in-memory sequence of scalars or tuples of scalars is a VALUES list, one row
    // per element in the order of the sequence. The type of each column is written
    // on the first row, as VALUES would otherwise take an integer for a bigint, a
    // whole decimal or a double, and text for a column of NULLs.
    private SelectQuery Values(IEnumerable items, Type element)
    {
        List<(string Name, Type Type)> columns = [];
        Columns(element, null, columns);
        List<List<object?>> rows = [];
        foreach (object? item in items)
        {
            List<object?> cells = [];
            Cells(item, element, 0, cells);
            rows.Add(cells);
        }
        if (rows.Count == 0)
        {
            throw new NotSupportedException(
                "An empty in-memory sequence cannot be translated into SQL: VALUES has at least one row."
            );
        }
        string alias = NextAlias();
        Queue<SqlColumn> read = new(
            columns.Select(
                (column, index) =>
                    new SqlColumn(
                        alias,
                        column.Name,
                        column.Type,
                        rows.Any(row => row[index] is null)
                    )
            )
        );
        return new SelectQuery(
            new ValuesSource(
                [
                    .. rows.Select(
                        (row, number) =>
                            (IReadOnlyList<SqlExpression>)
                                [
                                    .. row.Select(
                                        (cell, index) =>
                                            Cell(cell, columns[index].Type, number == 0)
                                    ),
                                ]
                    ),
                ],
                [.. columns.Select(column => column.Name)],
                alias
            ),
            Shaped(element, read)
        );
    }

    private static SqlExpression Cell(object? value, Type type, bool first)
    {
        SqlConstant constant = new(value, type);
        Type underlying = ScalarTypes.Underlying(type);
        return !first || (value is not null && SelfTyped.Contains(underlying))
            ? constant
            : new SqlCast(constant, ColumnType(underlying), type);
    }

    private static string ColumnType(Type underlying)
    {
        return ScalarTypes.IsNumber(underlying)
            ? ScalarTypes.Number(underlying)
            : ValueTypes[underlying];
    }

    private static bool IsTuple(Type type)
    {
        return type.IsGenericType && typeof(ITuple).IsAssignableFrom(type);
    }

    // The members of a tuple are Item1 to Item7, and Rest holds the eighth and later.
    private static string TupleMember(int index)
    {
        return index < 7 ? "Item" + (index + 1) : "Rest";
    }

    private static void Columns(
        Type type,
        string? path,
        List<(string Name, Type Type)> columns
    )
    {
        if (IsTuple(type))
        {
            Type[] members = type.GetGenericArguments();
            for (int index = 0; index < members.Length; index++)
            {
                string name = TupleMember(index);
                Columns(members[index], path is null ? name : path + "." + name, columns);
            }
            return;
        }
        Type underlying = ScalarTypes.Underlying(type);
        if (!ScalarTypes.IsNumber(underlying) && !ValueTypes.ContainsKey(underlying))
        {
            throw new NotSupportedException(
                $"In-memory rows of type '{type.Name}' cannot be translated into SQL: VALUES holds scalars and tuples of scalars."
            );
        }
        columns.Add((path ?? "Value", type));
    }

    // ITuple reads the members of Rest as if they followed the seventh, so the cells
    // of Rest are read from the same tuple further on.
    private static void Cells(object? value, Type type, int offset, List<object?> cells)
    {
        if (!IsTuple(type))
        {
            cells.Add(value);
            return;
        }
        ITuple tuple =
            value as ITuple
            ?? throw new NotSupportedException(
                "A null tuple of an in-memory sequence cannot be translated into SQL."
            );
        Type[] members = type.GetGenericArguments();
        for (int index = 0; index < members.Length; index++)
        {
            if (index == 7)
            {
                Cells(tuple, members[index], offset + 7, cells);
            }
            else if (IsTuple(members[index]))
            {
                Cells(tuple[offset + index], members[index], 0, cells);
            }
            else
            {
                cells.Add(tuple[offset + index]);
            }
        }
    }

    private static Shape Shaped(Type type, Queue<SqlColumn> columns)
    {
        if (!IsTuple(type))
        {
            return new ScalarShape(columns.Dequeue());
        }
        Type[] members = type.GetGenericArguments();
        return new ObjectShape([
            .. members.Select(
                (member, index) =>
                    new ObjectMember(TupleMember(index), Shaped(member, columns), member)
            ),
        ]);
    }

    // The element type of an in-memory sequence: of an array, a list or a sequence
    // interface; other collections may not yield their elements in a defined order.
    private static readonly HashSet<Type> Sequences =
    [
        typeof(List<>),
        typeof(IEnumerable<>),
        typeof(ICollection<>),
        typeof(IList<>),
        typeof(IReadOnlyCollection<>),
        typeof(IReadOnlyList<>),
    ];

    private static Type ElementOf(ConstantExpression constant)
    {
        Type type = constant.Value!.GetType();
        return type.IsArray ? type.GetElementType()!
            : type.IsGenericType && Sequences.Contains(type.GetGenericTypeDefinition())
                ? type.GetGenericArguments()[0]
            : constant.Type.IsGenericType
            && Sequences.Contains(constant.Type.GetGenericTypeDefinition())
                ? constant.Type.GetGenericArguments()[0]
            : throw new NotSupportedException(
                $"The in-memory source '{type.Name}' cannot be translated into SQL: only arrays and lists are."
            );
    }
}
