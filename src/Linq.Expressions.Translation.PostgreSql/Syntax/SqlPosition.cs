namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The position of a row of an in-memory sequence, which its VALUES list writes
// only when the statement reads it: rendering this into the statement asks for it.
internal sealed class SqlPosition(ValuesSource source) : SqlExpression(typeof(int))
{
    public override void Render(SqlWriter writer, SqlMode mode)
    {
        source.Positioned |= writer.Emits;
        _ = writer.Append(source.Alias + ".").Identifier(ValuesSource.Position);
    }
}
