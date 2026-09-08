using Vamoriq.Models;

namespace Vamoriq.Services.Interfaces;

public interface IMissionService
{
    Task InitializeAsync();
    Task<Mission?> GetMissionByLocalDateAsync(string localDate, CancellationToken ct = default);
    Task<Mission?> GetOverdueMissionAsync(string todayLocalDate, CancellationToken ct = default);
    Task<Mission> SaveMissionAsync(Mission mission, CancellationToken ct = default);
    Task<bool> CompleteMissionAsync(string missionId, Proof proof, CancellationToken ct = default);
    Task<bool> SkipMissionAsync(string missionId, SkipReason reason, CancellationToken ct = default);
    Task<int> AutoExpireOldMissionsAsync(string todayLocalDate, CancellationToken ct = default);
    Task<List<Mission>> GetCompletedMissionsAsync(int skip, int take, CancellationToken ct = default);
    Task<List<string>> GetCompletedCuratedIdsAsync(CancellationToken ct = default);
    Task<int> GetTotalCompletedAsync(CancellationToken ct = default);
}
