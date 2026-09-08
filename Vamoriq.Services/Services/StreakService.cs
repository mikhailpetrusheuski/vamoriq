using Vamoriq.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Services.Services;

public class StreakService : IStreakService
{
    private readonly ILogger<StreakService> _logger;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public StreakService(ILogger<StreakService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(baseDir, "Vamoriq");
        Directory.CreateDirectory(appDir);
        _dbPath = Path.Combine(appDir, "missions.db");
    }

    private async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized) return;
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync(ct).ConfigureAwait(false);
            var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS streaks (mission_local_date TEXT PRIMARY KEY, mission_id TEXT NOT NULL)";
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<int> GetCurrentStreakAsync(string todayLocalDate, CancellationToken ct = default)
    {
        try
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            if (!DateTime.TryParseExact(todayLocalDate, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var today))
            {
                _logger.LogWarning("Invalid date format: {Date}", todayLocalDate);
                return 0;
            }

            var dates = new List<DateTime>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT mission_local_date FROM streaks ORDER BY mission_local_date DESC";
            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var raw = reader.GetString(0);
                if (DateTime.TryParseExact(raw, "yyyy-MM-dd", null,
                        System.Globalization.DateTimeStyles.None, out var d))
                {
                    dates.Add(d);
                }
            }

            if (dates.Count == 0) return 0;

            var sorted = dates.OrderByDescending(d => d).Distinct().ToList();

            if ((today - sorted[0]).Days > 1) return 0;

            int streak = 1;
            for (int i = 1; i < sorted.Count; i++)
            {
                if ((sorted[i - 1] - sorted[i]).Days == 1)
                    streak++;
                else
                    break;
            }

            return streak;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current streak");
            return 0;
        }
    }

    public async Task<int> GetLongestStreakAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            var dates = new List<DateTime>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT mission_local_date FROM streaks ORDER BY mission_local_date ASC";
            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var raw = reader.GetString(0);
                if (DateTime.TryParseExact(raw, "yyyy-MM-dd", null,
                        System.Globalization.DateTimeStyles.None, out var d))
                {
                    dates.Add(d);
                }
            }

            if (dates.Count == 0) return 0;

            var sorted = dates.Distinct().OrderBy(d => d).ToList();
            int longest = 1;
            int current = 1;

            for (int i = 1; i < sorted.Count; i++)
            {
                if ((sorted[i] - sorted[i - 1]).Days == 1)
                {
                    current++;
                    if (current > longest) longest = current;
                }
                else
                {
                    current = 1;
                }
            }

            return longest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get longest streak");
            return 0;
        }
    }

    public async Task<int> GetTotalCompletedAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM streaks";
            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total completed count from streaks");
            return 0;
        }
    }
}
