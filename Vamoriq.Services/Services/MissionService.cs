using Vamoriq.Models;
using Vamoriq.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Vamoriq.Services.Services;

public class MissionService : IMissionService
{
    private readonly ILogger<MissionService> _logger;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public MissionService(ILogger<MissionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(baseDir, "Vamoriq");
        Directory.CreateDirectory(appDir);
        _dbPath = Path.Combine(appDir, "missions.db");
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync().ConfigureAwait(false);

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS missions (
    id TEXT PRIMARY KEY,
    curated_id TEXT,
    title TEXT NOT NULL,
    description TEXT,
    instructions_json TEXT,
    category INTEGER NOT NULL,
    difficulty INTEGER NOT NULL,
    goal_track INTEGER NOT NULL,
    estimated_minutes INTEGER,
    tags_json TEXT,
    proof_suggestion TEXT,
    status INTEGER NOT NULL DEFAULT 0,
    mission_local_date TEXT NOT NULL UNIQUE,
    created_at_utc TEXT NOT NULL,
    completed_at_utc TEXT,
    expired_at_utc TEXT,
    skip_reason TEXT
);

CREATE TABLE IF NOT EXISTS proofs (
    id TEXT PRIMARY KEY,
    mission_id TEXT NOT NULL,
    text_content TEXT NOT NULL,
    photo_path TEXT,
    completed_at_utc TEXT NOT NULL,
    FOREIGN KEY (mission_id) REFERENCES missions(id)
);

CREATE TABLE IF NOT EXISTS streaks (
    mission_local_date TEXT PRIMARY KEY,
    mission_id TEXT NOT NULL
);";
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            _initialized = true;
            _logger.LogInformation("MissionService database initialized at {Path}", _dbPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize missions database");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<Mission?> GetMissionByLocalDateAsync(string localDate, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM missions WHERE mission_local_date = $date LIMIT 1";
        cmd.Parameters.AddWithValue("$date", localDate);

        using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await reader.ReadAsync(ct).ConfigureAwait(false))
            return ReadMission(reader);

        return null;
    }

    public async Task<Mission?> GetOverdueMissionAsync(string todayLocalDate, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT * FROM missions
WHERE mission_local_date < $today AND status = 0
ORDER BY mission_local_date DESC
LIMIT 1";
        cmd.Parameters.AddWithValue("$today", todayLocalDate);

        using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await reader.ReadAsync(ct).ConfigureAwait(false))
            return ReadMission(reader);

        return null;
    }

    public async Task<Mission> SaveMissionAsync(Mission mission, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO missions
    (id, curated_id, title, description, instructions_json, category, difficulty,
     goal_track, estimated_minutes, tags_json, proof_suggestion, status,
     mission_local_date, created_at_utc)
VALUES
    ($id, $curated_id, $title, $desc, $instr, $cat, $diff,
     $track, $mins, $tags, $proof_sug, $status,
     $local_date, $created)";
        BindMissionInsertParams(cmd, mission);

        try
        {
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Mission saved id={Id} date={Date}", mission.Id, mission.MissionLocalDate);
            return mission;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            _logger.LogInformation("Mission already exists for date={Date}, returning existing", mission.MissionLocalDate);

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM missions WHERE mission_local_date = $date LIMIT 1";
            selectCmd.Parameters.AddWithValue("$date", mission.MissionLocalDate);

            using var reader = await selectCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (await reader.ReadAsync(ct).ConfigureAwait(false))
                return ReadMission(reader);

            throw new InvalidOperationException("UNIQUE conflict detected but existing row not found");
        }
    }

    public async Task<bool> CompleteMissionAsync(string missionId, Proof proof, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        using var transaction = connection.BeginTransaction();
        try
        {
            var insertProof = connection.CreateCommand();
            insertProof.Transaction = transaction;
            insertProof.CommandText = @"
INSERT INTO proofs (id, mission_id, text_content, photo_path, completed_at_utc)
VALUES ($id, $mission_id, $text, $photo, $completed)";
            insertProof.Parameters.AddWithValue("$id", proof.Id);
            insertProof.Parameters.AddWithValue("$mission_id", proof.MissionId);
            insertProof.Parameters.AddWithValue("$text", proof.TextContent);
            insertProof.Parameters.AddWithValue("$photo", proof.PhotoPath ?? (object)DBNull.Value);
            insertProof.Parameters.AddWithValue("$completed", proof.CompletedAtUtc.ToString("o"));
            await insertProof.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            var updateMission = connection.CreateCommand();
            updateMission.Transaction = transaction;
            updateMission.CommandText = @"
UPDATE missions SET status = 1, completed_at_utc = $completed
WHERE id = $id AND status = 0";
            updateMission.Parameters.AddWithValue("$id", missionId);
            updateMission.Parameters.AddWithValue("$completed", proof.CompletedAtUtc.ToString("o"));
            var updated = await updateMission.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            if (updated == 0)
            {
                transaction.Rollback();
                _logger.LogInformation("CompleteMission no-op: mission {Id} not in Assigned status", missionId);
                return false;
            }

            var getDate = connection.CreateCommand();
            getDate.Transaction = transaction;
            getDate.CommandText = "SELECT mission_local_date FROM missions WHERE id = $id";
            getDate.Parameters.AddWithValue("$id", missionId);
            var localDate = (string?)await getDate.ExecuteScalarAsync(ct).ConfigureAwait(false);

            if (localDate is not null)
            {
                var insertStreak = connection.CreateCommand();
                insertStreak.Transaction = transaction;
                insertStreak.CommandText = @"
INSERT OR IGNORE INTO streaks (mission_local_date, mission_id)
VALUES ($date, $mission_id)";
                insertStreak.Parameters.AddWithValue("$date", localDate);
                insertStreak.Parameters.AddWithValue("$mission_id", missionId);
                await insertStreak.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            transaction.Commit();
            _logger.LogInformation("Mission completed id={Id} proof={ProofId}", missionId, proof.Id);
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to complete mission {Id}", missionId);
            throw;
        }
    }

    public async Task<bool> SkipMissionAsync(string missionId, SkipReason reason, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        using var transaction = connection.BeginTransaction();
        try
        {
            var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
UPDATE missions SET status = 2, skip_reason = $reason, expired_at_utc = $expired
WHERE id = $id AND status = 0";
            cmd.Parameters.AddWithValue("$id", missionId);
            cmd.Parameters.AddWithValue("$reason", reason.ToString());
            cmd.Parameters.AddWithValue("$expired", DateTime.UtcNow.ToString("o"));
            var updated = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            if (updated == 0)
            {
                transaction.Rollback();
                _logger.LogInformation("SkipMission no-op: mission {Id} not in Assigned status", missionId);
                return false;
            }

            transaction.Commit();
            _logger.LogInformation("Mission skipped id={Id} reason={Reason}", missionId, reason);
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to skip mission {Id}", missionId);
            throw;
        }
    }

    public async Task<int> AutoExpireOldMissionsAsync(string todayLocalDate, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var yesterday = DateTime.ParseExact(todayLocalDate, "yyyy-MM-dd", null)
            .AddDays(-1)
            .ToString("yyyy-MM-dd");

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
UPDATE missions SET status = 3, expired_at_utc = $now
WHERE mission_local_date < $yesterday AND status = 0";
        cmd.Parameters.AddWithValue("$yesterday", yesterday);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));

        var count = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (count > 0)
            _logger.LogInformation("AutoExpire: {Count} missions marked as Missed (before {Yesterday})", count, yesterday);

        return count;
    }

    public async Task<List<Mission>> GetCompletedMissionsAsync(int skip, int take, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT * FROM missions
WHERE status = 1
ORDER BY completed_at_utc DESC
LIMIT $take OFFSET $skip";
        cmd.Parameters.AddWithValue("$take", take);
        cmd.Parameters.AddWithValue("$skip", skip);

        var list = new List<Mission>();
        using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            list.Add(ReadMission(reader));

        return list;
    }

    public async Task<List<string>> GetCompletedCuratedIdsAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT curated_id FROM missions
WHERE status = 1 AND curated_id IS NOT NULL AND curated_id != ''";

        var ids = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            ids.Add(reader.GetString(0));

        return ids;
    }

    public async Task<int> GetTotalCompletedAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM missions WHERE status = 1";

        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return Convert.ToInt32(result);
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await InitializeAsync().ConfigureAwait(false);
    }

    private static void BindMissionInsertParams(SqliteCommand cmd, Mission m)
    {
        cmd.Parameters.AddWithValue("$id", m.Id);
        cmd.Parameters.AddWithValue("$curated_id", m.CuratedId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$title", m.Title);
        cmd.Parameters.AddWithValue("$desc", m.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$instr", JsonConvert.SerializeObject(m.Instructions ?? new List<string>()));
        cmd.Parameters.AddWithValue("$cat", (int)m.Category);
        cmd.Parameters.AddWithValue("$diff", (int)m.Difficulty);
        cmd.Parameters.AddWithValue("$track", (int)m.GoalTrack);
        cmd.Parameters.AddWithValue("$mins", m.EstimatedMinutes);
        cmd.Parameters.AddWithValue("$tags", JsonConvert.SerializeObject(m.Tags ?? new List<string>()));
        cmd.Parameters.AddWithValue("$proof_sug", m.ProofSuggestion ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$status", (int)m.Status);
        cmd.Parameters.AddWithValue("$local_date", m.MissionLocalDate);
        cmd.Parameters.AddWithValue("$created", m.CreatedAtUtc.ToString("o"));
    }

    private Mission ReadMission(SqliteDataReader reader)
    {
        var instructionsJson = reader.IsDBNull(reader.GetOrdinal("instructions_json"))
            ? null : reader.GetString(reader.GetOrdinal("instructions_json"));
        var tagsJson = reader.IsDBNull(reader.GetOrdinal("tags_json"))
            ? null : reader.GetString(reader.GetOrdinal("tags_json"));

        return new Mission
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            CuratedId = reader.IsDBNull(reader.GetOrdinal("curated_id"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("curated_id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Description = reader.IsDBNull(reader.GetOrdinal("description"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
            Instructions = string.IsNullOrEmpty(instructionsJson)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(instructionsJson) ?? new List<string>(),
            Category = (MissionCategory)reader.GetInt32(reader.GetOrdinal("category")),
            Difficulty = (MissionDifficulty)reader.GetInt32(reader.GetOrdinal("difficulty")),
            GoalTrack = (GoalTrack)reader.GetInt32(reader.GetOrdinal("goal_track")),
            EstimatedMinutes = reader.GetInt32(reader.GetOrdinal("estimated_minutes")),
            Tags = string.IsNullOrEmpty(tagsJson)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(tagsJson) ?? new List<string>(),
            ProofSuggestion = reader.IsDBNull(reader.GetOrdinal("proof_suggestion"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("proof_suggestion")),
            Status = (MissionStatus)reader.GetInt32(reader.GetOrdinal("status")),
            MissionLocalDate = reader.GetString(reader.GetOrdinal("mission_local_date")),
            CreatedAtUtc = DateTime.TryParse(
                reader.GetString(reader.GetOrdinal("created_at_utc")), out var created)
                ? created : DateTime.UtcNow,
            CompletedAtUtc = reader.IsDBNull(reader.GetOrdinal("completed_at_utc"))
                ? null
                : DateTime.TryParse(reader.GetString(reader.GetOrdinal("completed_at_utc")), out var comp)
                    ? comp : null,
            ExpiredAtUtc = reader.IsDBNull(reader.GetOrdinal("expired_at_utc"))
                ? null
                : DateTime.TryParse(reader.GetString(reader.GetOrdinal("expired_at_utc")), out var exp)
                    ? exp : null
        };
    }
}
