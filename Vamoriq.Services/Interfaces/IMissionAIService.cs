using Vamoriq.Models;

namespace Vamoriq.Services.Interfaces;

public record PersonalizationResult(Mission Mission, bool WasPersonalized);

public interface IMissionAIService
{
    Task<PersonalizationResult> PersonalizeMissionAsync(CuratedMission baseMission, string city, int dayNumber, CancellationToken ct = default);
}
