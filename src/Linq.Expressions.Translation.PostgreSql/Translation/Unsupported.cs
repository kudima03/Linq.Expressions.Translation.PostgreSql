using System.Linq.Expressions;
using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal static class Unsupported
{
    public static NotSupportedException Method(MethodInfo method)
    {
        return new NotSupportedException(
            $"The method '{method.DeclaringType!.Name}.{method.Name}' is not supported."
        );
    }

    public static NotSupportedException Member(MemberInfo member)
    {
        return new NotSupportedException(
            $"The member '{member.DeclaringType!.Name}.{member.Name}' is not supported."
        );
    }

    public static NotSupportedException Binding(MemberBinding binding)
    {
        return new NotSupportedException(
            $"Nested object and collection initializers are not supported; assign '{binding.Member.DeclaringType!.Name}.{binding.Member.Name}' a value instead."
        );
    }

    public static NotSupportedException Node(Expression expression)
    {
        return new NotSupportedException(
            $"The expression node '{expression.NodeType}' is not supported: {expression}."
        );
    }
}
