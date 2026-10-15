using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private SqlExpression Method(MethodCallExpression call)
    {
        if (
            call.Method.Name == nameof(Equals)
            && call.Method.GetParameters()
                .All(parameter => parameter.ParameterType != typeof(StringComparison))
        )
        {
            return call.Object is null
                ? Equality(Typed(call.Arguments[0]), Typed(call.Arguments[1]), false)
                : Equality(call.Object, Typed(call.Arguments[0]), false);
        }
        if (
            call.Method.Name == nameof(Nullable<>.GetValueOrDefault)
            && Nullable.GetUnderlyingType(call.Object!.Type) is not null
        )
        {
            SqlExpression fallback =
                call.Arguments.Count == 0
                    ? new SqlConstant(Activator.CreateInstance(call.Type), call.Type)
                    : Sql(call.Arguments[0]);
            return new SqlFunction("COALESCE", [Sql(call.Object), fallback], call.Type);
        }
        throw Unsupported.Method(call.Method);
    }

    private static Expression Typed(Expression argument)
    {
        return argument is ConstantExpression { Value: object value }
            ? Expression.Constant(value)
            : argument;
    }
}
