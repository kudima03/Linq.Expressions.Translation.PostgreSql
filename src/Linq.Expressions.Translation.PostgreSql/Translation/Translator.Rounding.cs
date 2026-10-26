using System.Linq.Expressions;
using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    // ROUND rounds a numeric half away from zero and a double by the platform's rule,
    // so the half-way case, an exact .5 fraction of the value scaled to the digits, is
    // decided explicitly: half to even, as .NET does by default, or away from zero.
    private static SqlExpression Rounded(MethodCallExpression call, SqlExpression value)
    {
        ParameterInfo[] parameters = call.Method.GetParameters();
        MidpointRounding mode =
            parameters[^1].ParameterType == typeof(MidpointRounding)
                ? Mode(call)
                : MidpointRounding.ToEven;
        int digits =
            parameters.Length > 1 && parameters[1].ParameterType == typeof(int)
                ? Count(call.Arguments[1])
                : 0;
        Type type = call.Type;
        bool floating = type == typeof(double);
        bool scaledResult = floating || digits == 0;
        SqlExpression scaled =
            digits == 0 ? value : Calculate("*", value, Factor(digits, type));
        SqlExpression truncated = new SqlFunction("TRUNC", [scaled], type);
        SqlExpression half = Compare(
            "=",
            new SqlFunction("ABS", [Calculate("-", scaled, truncated)], type),
            new SqlConstant(floating ? 0.5 : 0.5m, type)
        );
        SqlExpression condition =
            mode == MidpointRounding.AwayFromZero
                ? half
                : Logical(
                    "AND",
                    half,
                    Compare(
                        "=",
                        new SqlFunction(
                            "MOD",
                            [
                                floating
                                    ? new SqlCast(truncated, "numeric", typeof(decimal))
                                    : truncated,
                                Integer(2),
                            ],
                            typeof(decimal)
                        ),
                        Integer(0)
                    )
                );
        SqlExpression tie =
            mode == MidpointRounding.AwayFromZero
                ? Calculate("+", truncated, new SqlFunction("SIGN", [scaled], type))
            : scaledResult ? truncated
            : new SqlFunction("TRUNC", [value, Integer(digits)], type);
        SqlExpression rounded = new SqlCase(
            [new SqlWhen(condition, tie)],
            scaledResult
                ? new SqlFunction("ROUND", [scaled], type)
                : new SqlFunction("ROUND", [value, Integer(digits)], type),
            type
        );
        return floating && digits != 0
            ? Calculate("/", rounded, Factor(digits, type))
            : rounded;
    }

    private static MidpointRounding Mode(MethodCallExpression call)
    {
        return
            call.Arguments[^1] is ConstantExpression { Value: MidpointRounding mode }
            && mode is MidpointRounding.ToEven or MidpointRounding.AwayFromZero
            ? mode
            : throw Unsupported.Method(call.Method);
    }

    private static SqlConstant Factor(int digits, Type type)
    {
        return type == typeof(double)
            ? new SqlConstant(Math.Pow(10, digits), typeof(double))
            : new SqlConstant(Scale(digits), typeof(decimal));
    }

    private static decimal Scale(int digits)
    {
        decimal factor = 1m;
        for (int index = 0; index < Math.Abs(digits); index++)
        {
            factor *= 10m;
        }
        return digits < 0 ? 1m / factor : factor;
    }
}
