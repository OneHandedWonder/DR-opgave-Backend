using DR.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data;
using System.Data.Common;

namespace DR_Repo.Services;

public interface IHealthStatusService
{
    Task<HealthStatusResponseDto> GetStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class HealthStatusService : IHealthStatusService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly RecordDbContext _dbContext;

    public HealthStatusService(HealthCheckService healthCheckService, RecordDbContext dbContext)
    {
        _healthCheckService = healthCheckService;
        _dbContext = dbContext;
    }

    public async Task<HealthStatusResponseDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

        var checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new HealthCheckItemDto(
                entry.Value.Status.ToString(),
                entry.Value.Duration.TotalMilliseconds,
                entry.Value.Description));

        var tableStatus = await GetTableStatusAsync(cancellationToken);

        return new HealthStatusResponseDto(
            report.Status.ToString(),
            DateTime.UtcNow,
            report.TotalDuration.TotalMilliseconds,
            checks,
            tableStatus);
    }

    private async Task<Dictionary<string, TableStatusItemDto>> GetTableStatusAsync(CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, TableStatusItemDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["Records"] = await GetSingleTableStatusAsync("Records", null, cancellationToken),
            ["tracks"] = await GetSingleTableStatusAsync("tracks", "playedAt", cancellationToken)
        };

        return results;
    }

    private async Task<TableStatusItemDto> GetSingleTableStatusAsync(
        string tableName,
        string? freshnessColumn,
        CancellationToken cancellationToken)
    {
        var quotedTable = QuoteIdentifier(tableName);

        try
        {
            var schemaQualifiedTable = $"public.{quotedTable}";
            var exists = await ExecuteScalarAsync<bool>(
                "SELECT to_regclass(@tableName) IS NOT NULL",
                cancellationToken,
                ("@tableName", schemaQualifiedTable));

            if (!exists)
            {
                return new TableStatusItemDto(false, null, null, "Table was not found.");
            }

            var rowCount = await ExecuteScalarAsync<long>(
                $"SELECT COUNT(*) FROM {quotedTable}",
                cancellationToken);

            DateTime? newestRowTimestampUtc = null;
            if (!string.IsNullOrWhiteSpace(freshnessColumn))
            {
                var quotedColumn = QuoteIdentifier(freshnessColumn);

                newestRowTimestampUtc = await ExecuteScalarNullableDateTimeAsync(
                    $"SELECT MAX({quotedColumn}) FROM {quotedTable}",
                    cancellationToken);
            }

            return new TableStatusItemDto(true, rowCount, newestRowTimestampUtc, null);
        }
        catch (Exception ex)
        {
            return new TableStatusItemDto(false, null, null, ex.Message);
        }
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    private async Task<T> ExecuteScalarAsync<T>(
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken, parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException("Expected scalar result but got null.");
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    private async Task<DateTime?> ExecuteScalarNullableDateTimeAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result is DBNull)
        {
            return null;
        }

        if (result is DateTime dateTime)
        {
            return dateTime;
        }

        return DateTime.TryParse(result.ToString(), out var parsed) ? parsed : null;
    }

    private async Task<DbCommand> CreateCommandAsync(
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value;
            command.Parameters.Add(dbParameter);
        }

        return command;
    }
}

public sealed record HealthStatusResponseDto(
    string Status,
    DateTime CheckedAtUtc,
    double TotalDurationMs,
    Dictionary<string, HealthCheckItemDto> Checks,
    Dictionary<string, TableStatusItemDto> Tables);

public sealed record HealthCheckItemDto(
    string Status,
    double DurationMs,
    string? Description);

public sealed record TableStatusItemDto(
    bool Exists,
    long? RowCount,
    DateTime? NewestRowTimestampUtc,
    string? Error);
