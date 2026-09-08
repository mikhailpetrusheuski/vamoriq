using Vamoriq.Models;

namespace Vamoriq.Services.Interfaces;

public interface ICuratedMissionLibrary
{
    Task<CuratedMission?> GetNextMissionAsync(IReadOnlyList<string> completedCuratedIds, int dayNumber, CancellationToken ct = default);
    Task LocalizeMissionAsync(Mission mission, CancellationToken ct = default);
}
