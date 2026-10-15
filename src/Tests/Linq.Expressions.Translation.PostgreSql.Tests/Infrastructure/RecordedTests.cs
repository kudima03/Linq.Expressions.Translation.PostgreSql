using System.Collections;
using System.Reflection;
using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public sealed class RecordedTests(Type excluded) : IEnumerable<RecordedTest>
{
    public IEnumerator<RecordedTest> GetEnumerator()
    {
        IEnumerable<Type> classes = typeof(RecordedTests)
            .Assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract && (type != excluded));
        foreach (Type type in classes)
        {
            IEnumerable<MethodInfo> methods = type.GetMethods(
                    BindingFlags.Public
                        | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly
                )
                .Where(method => method.GetCustomAttribute<FactAttribute>() is not null);
            foreach (MethodInfo method in methods)
            {
                object instance = Activator.CreateInstance(type)!;
                int row = 0;
                foreach (object?[] arguments in Arguments(method))
                {
                    yield return new RecordedTest(
                        $"{type.Name}.{method.Name}[{row}]",
                        SqlAssert.Record(() => method.Invoke(instance, arguments))
                    );
                    row++;
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static IEnumerable<object?[]> Arguments(MethodInfo method)
    {
        return method.GetParameters().Length == 0
            ?
            [
                [],
            ]
            : method
                .GetCustomAttributes<DataAttribute>()
                .SelectMany(attribute => attribute.GetData(method));
    }
}
