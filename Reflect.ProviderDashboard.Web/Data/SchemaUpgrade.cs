using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Reflect.ProviderDashboard.Web.Data;

/// <summary>
/// EnsureCreated never adds tables to a database that already exists. Until versioned migrations
/// replace it (see docs/TODO.md), create only the named, newly added tables when they are missing
/// by running the matching statements from the model's own create script. Never alters existing tables.
/// </summary>
public static class SchemaUpgrade
{
    public static async Task EnsureTablesAsync(DbContext db, CancellationToken ct, params string[] tables)
    {
        var missing = new List<string>();
        foreach (var table in tables)
        {
            try { await db.Database.ExecuteSqlRawAsync($"SELECT 1 FROM \"{table}\" WHERE 1 = 0", ct); }
            catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or Npgsql.PostgresException) { missing.Add(table); }
        }
        if (missing.Count == 0) return;

        var statements = Regex.Split(db.Database.GenerateCreateScript(), @";\s*\r?\n");
        foreach (var statement in statements)
        {
            var match = Regex.Match(statement, "^\\s*CREATE\\s+(?:UNIQUE\\s+)?(?:TABLE|INDEX)\\s+(?:IF NOT EXISTS\\s+)?\"?\\w+\"?(?:\\s+ON\\s+\"?(\\w+)\"?)?", RegexOptions.IgnoreCase);
            if (!match.Success) continue;
            var target = match.Groups[1].Success ? match.Groups[1].Value
                : Regex.Match(statement, "TABLE\\s+\"?(\\w+)\"?", RegexOptions.IgnoreCase).Groups[1].Value;
            if (!missing.Contains(target, StringComparer.OrdinalIgnoreCase)) continue;
            await db.Database.ExecuteSqlRawAsync(statement.Trim().TrimEnd(';'), ct);
        }
    }
}
