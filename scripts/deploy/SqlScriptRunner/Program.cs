using System.Text;
using Microsoft.Data.SqlClient;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: SqlScriptRunner <script-path>");
    return 2;
}

var scriptPath = args[0];
if (!File.Exists(scriptPath))
{
    Console.Error.WriteLine("Migration SQL script was not found.");
    return 2;
}

var scriptInfo = new FileInfo(scriptPath);
if (scriptInfo.Length == 0)
{
    Console.Error.WriteLine("Migration SQL script is empty.");
    return 2;
}

var connectionString = Environment.GetEnvironmentVariable("PRODUCTION_DATABASE_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("PRODUCTION_DATABASE_CONNECTION_STRING is required.");
    return 2;
}

Console.WriteLine("Production migration execution started.");

try
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    var batchCount = 0;
    foreach (var batch in SplitSqlBatches(await File.ReadAllTextAsync(scriptPath)))
    {
        if (string.IsNullOrWhiteSpace(batch))
        {
            continue;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = batch;
        command.CommandTimeout = 300;
        await command.ExecuteNonQueryAsync();
        batchCount++;
    }

    Console.WriteLine($"Production migration execution succeeded. Batches executed: {batchCount}.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine("Production migration execution failed.");
    Console.Error.WriteLine(exception.GetType().Name + ": " + exception.Message);
    return 1;
}

static IEnumerable<string> SplitSqlBatches(string sql)
{
    using var reader = new StringReader(sql);
    var batch = new StringBuilder();

    string? line;
    while ((line = reader.ReadLine()) is not null)
    {
        if (IsGoSeparator(line))
        {
            yield return batch.ToString();
            batch.Clear();
            continue;
        }

        batch.AppendLine(line);
    }

    if (batch.Length > 0)
    {
        yield return batch.ToString();
    }
}

static bool IsGoSeparator(string line)
{
    var trimmed = line.Trim();
    return trimmed.Equals("GO", StringComparison.OrdinalIgnoreCase);
}
