using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Vamoriq.Models;
using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Services.Services;

public class CuratedMissionLibrary : ICuratedMissionLibrary
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<CuratedMissionLibrary> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<CuratedMission>? _missions;
    private string? _loadedLang;

    public CuratedMissionLibrary(ILogger<CuratedMissionLibrary> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CuratedMission?> GetNextMissionAsync(
        IReadOnlyList<string> completedCuratedIds,
        int dayNumber,
        CancellationToken ct = default)
    {
        var allMissions = await LoadMissionsAsync(ct);

        if (allMissions.Count == 0)
            return null;

        var remaining = allMissions
            .Where(m => m.GoalTrack == "NewCity")
            .Where(m => !completedCuratedIds.Contains(m.Id))
            .ToList();

        var candidates = remaining
            .Where(m => m.Tier is "strong" or "medium")
            .ToList();

        if (candidates.Count == 0 && remaining.Count > 0)
        {
            _logger.LogInformation("Strong+medium missions exhausted — moving to backup tier");
            candidates = remaining;
        }

        if (candidates.Count == 0)
        {
            _logger.LogInformation("All curated missions completed — allowing repeats");
            candidates = allMissions
                .Where(m => m.GoalTrack == "NewCity")
                .Where(m => m.Tier is "strong" or "medium")
                .ToList();
        }

        if (candidates.Count == 0)
            return null;

        return PickWeightedRandom(candidates, dayNumber);
    }

    private static CuratedMission PickWeightedRandom(List<CuratedMission> candidates, int dayNumber)
    {
        var weights = candidates
            .Select(m => dayNumber <= 7 && m.Tier == "strong" ? 3 : 1)
            .ToList();

        var totalWeight = weights.Sum();
        var roll = Random.Shared.Next(totalWeight);

        var cumulative = 0;
        for (var i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return candidates[i];
        }

        return candidates[^1];
    }

    private async Task<List<CuratedMission>> LoadMissionsAsync(CancellationToken ct)
    {
        var lang = CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName ?? "en";

        if (_missions is not null && _loadedLang == lang)
            return _missions;

        await _semaphore.WaitAsync(ct);
        try
        {
            if (_missions is not null && _loadedLang == lang)
                return _missions;

            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "Vamoriq.Services.Data.missions_newcity.json";

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                _logger.LogError("Embedded resource {Resource} not found", resourceName);
                _missions = new List<CuratedMission>();
                _loadedLang = lang;
                return _missions;
            }

            var loaded = await JsonSerializer.DeserializeAsync<List<CuratedMission>>(stream, JsonOptions, ct);
            _missions = loaded ?? new List<CuratedMission>();

            if (lang != "en")
                await ApplyTranslationsAsync(assembly, lang, ct);

            _loadedLang = lang;
            _logger.LogInformation("Loaded {Count} curated missions (lang={Lang})", _missions.Count, lang);
            return _missions;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ApplyTranslationsAsync(Assembly assembly, string lang, CancellationToken ct)
    {
        var resName = $"Vamoriq.Services.Data.missions_newcity.{lang}.json";
        await using var stream = assembly.GetManifestResourceStream(resName);
        if (stream is null)
        {
            _logger.LogDebug("No translation file for {Lang}", lang);
            return;
        }

        var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, CuratedMissionTranslation>>(stream, JsonOptions, ct);
        if (dict is null) return;

        foreach (var m in _missions!)
        {
            if (dict.TryGetValue(m.Id, out var tr))
                m.Translation = tr;
        }

        _logger.LogInformation("Applied {Count} translations for {Lang}", dict.Count, lang);
    }

    public async Task LocalizeMissionAsync(Mission mission, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(mission.CuratedId)) return;

        var lang = CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName ?? "en";
        if (lang == "en") return;

        var allMissions = await LoadMissionsAsync(ct);
        var curated = allMissions.FirstOrDefault(m => m.Id == mission.CuratedId);
        if (curated is null) return;

        mission.Title = curated.GetTitle(lang);
        mission.Description = curated.GetDescription(lang);
        mission.Instructions = curated.GetInstructions(lang);
        mission.ProofSuggestion = curated.GetProofSuggestion(lang);
    }
}
