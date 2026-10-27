using Npgsql;
using Testcontainers.PostgreSql;
using Xunit.Sdk;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// One PostgreSQL server for all execution tests, holding the Seed tables.
public sealed class Database : IAsyncLifetime
{
    // The builtin C.UTF-8 locale orders text by code point and maps case by Unicode,
    // whatever locales the image has.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithEnvironment(
            "POSTGRES_INITDB_ARGS",
            "--locale-provider=builtin --builtin-locale=C.UTF-8"
        )
        .Build();

    private NpgsqlDataSource? _source;

    private NpgsqlDataSource Source =>
        _source ?? throw new InvalidOperationException("The database is not started.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        NpgsqlConnectionStringBuilder connection = new(_container.GetConnectionString())
        {
            // A DateTime converts to a DateTimeOffset through the local zone in .NET and
            // a timestamp to a timestamptz through the session zone in PostgreSQL.
            Timezone = LocalZone(),
        };
        _source = NpgsqlDataSource.Create(connection.ConnectionString);
        await using NpgsqlConnection open = await _source
            .OpenConnectionAsync()
            .ConfigureAwait(false);
        foreach (TableDefinition table in Seed.Tables)
        {
            await using NpgsqlBatch batch = new(open);
            if (table.Schema is not null)
            {
                batch.BatchCommands.Add(
                    new NpgsqlBatchCommand(
                        $"CREATE SCHEMA IF NOT EXISTS \"{table.Schema}\""
                    )
                );
            }
            batch.BatchCommands.Add(new NpgsqlBatchCommand(table.Create));
            foreach (object row in table.Rows)
            {
                NpgsqlBatchCommand insert = new(table.Insert);
                foreach (object value in table.Values(row))
                {
                    _ = insert.Parameters.Add(new NpgsqlParameter { Value = value });
                }
                batch.BatchCommands.Add(insert);
            }
            _ = await batch.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    public async Task DisposeAsync()
    {
        if (_source is not null)
        {
            await _source.DisposeAsync().ConfigureAwait(false);
        }
        await _container.DisposeAsync().ConfigureAwait(false);
    }

    // Runs a statement and reads every row; a failing statement fails the test with
    // the statement in the message.
    public ResultSet Query(string sql)
    {
        try
        {
            using NpgsqlConnection connection = Source.OpenConnection();
            using NpgsqlCommand command = new(sql, connection);
            using NpgsqlDataReader reader = command.ExecuteReader();
            List<string> columns =
            [
                .. Enumerable.Range(0, reader.FieldCount).Select(reader.GetName),
            ];
            List<object?[]> rows = [];
            while (reader.Read())
            {
                object?[] row = new object?[reader.FieldCount];
                for (int index = 0; index < row.Length; index++)
                {
                    row[index] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                }
                rows.Add(row);
            }
            return new ResultSet(sql, columns, rows);
        }
        catch (PostgresException exception)
        {
            throw new XunitException($"{exception.Message}{Environment.NewLine}{sql}");
        }
    }

    private static string LocalZone()
    {
        TimeZoneInfo local = TimeZoneInfo.Local;
        return local.HasIanaId ? local.Id
            : TimeZoneInfo.TryConvertWindowsIdToIanaId(local.Id, out string? zone) ? zone
            : throw new InvalidOperationException(
                $"The local time zone '{local.Id}' has no IANA name for PostgreSQL."
            );
    }
}
