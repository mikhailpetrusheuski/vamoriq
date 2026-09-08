using Vamoriq.Models;
using Vamoriq.Services.Interfaces;
using Vamoriq.Services.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace Vamoriq.Services.Services;

public class MissionAIService : IMissionAIService
{
    private readonly IGraphQLService _graphQLService;
    private readonly IAuthService _authService;
    private readonly IPromptProvider _promptProvider;
    private readonly ILogger<MissionAIService> _logger;

    public MissionAIService(
        IGraphQLService graphQLService,
        IAuthService authService,
        IPromptProvider promptProvider,
        ILogger<MissionAIService> logger)
    {
        _graphQLService = graphQLService ?? throw new ArgumentNullException(nameof(graphQLService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PersonalizationResult> PersonalizeMissionAsync(
        CuratedMission baseMission, string city, int dayNumber, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(8));
            var token = cts.Token;

            if (!await _authService.IsAuthenticatedAsync(token))
            {
                _logger.LogWarning("Not authenticated, returning fallback mission");
                return new PersonalizationResult(CreateFallbackMission(baseMission), false);
            }

            var locale = CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName ?? "en";
            var promptId = _promptProvider.GetPromptId("mission_personalize", locale);
            if (string.IsNullOrEmpty(promptId))
            {
                _logger.LogWarning("No prompt ID found for mission_personalize/{Locale}", locale);
                return new PersonalizationResult(CreateFallbackMission(baseMission), false);
            }

            var requestData = new Dictionary<string, object>
            {
                ["baseTitle"] = baseMission.GetTitle(locale),
                ["baseDescription"] = baseMission.GetDescription(locale),
                ["baseInstructions"] = baseMission.GetInstructions(locale),
                ["category"] = baseMission.Category,
                ["difficulty"] = baseMission.Difficulty,
                ["estimatedMinutes"] = baseMission.EstimatedMinutes,
                ["tags"] = baseMission.Tags,
                ["proofSuggestion"] = baseMission.GetProofSuggestion(locale),
                ["city"] = city,
                ["dayNumber"] = dayNumber
            };

            var input = new { promptId, requestData, locale };
            var variables = new { input };

            var query = @"
            mutation GenerateAI($input: GenerateAIInput!) {
                generateAI(input: $input) {
                    generateAI {
                        success
                        data
                        error
                    }
                }
            }";

            var gqlResponse = await _graphQLService
                .ExecuteQueryAsync<GraphQLResponse<GenerateAIResponse>>(query, variables, token);

            var aiResult = gqlResponse?.Data?.GenerateAI?.GenerateAI;
            if (aiResult is null || !aiResult.Success || !aiResult.Data.HasValue)
            {
                _logger.LogWarning("AI returned no usable data: {Error}", aiResult?.Error);
                return new PersonalizationResult(CreateFallbackMission(baseMission), false);
            }

            var dataElement = aiResult.Data.Value;
            if (dataElement.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning("AI data is not an object: {Kind}", dataElement.ValueKind);
                return new PersonalizationResult(CreateFallbackMission(baseMission), false);
            }

            var dto = JsonSerializer.Deserialize<ApiMissionDto>(
                dataElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null)
            {
                _logger.LogWarning("Failed to deserialize AI mission response");
                return new PersonalizationResult(CreateFallbackMission(baseMission), false);
            }

            var mission = new Mission
            {
                Id = Guid.NewGuid().ToString(),
                CuratedId = baseMission.Id,
                Title = dto.Title ?? baseMission.GetTitle(locale),
                Description = dto.Description ?? baseMission.GetDescription(locale),
                Instructions = dto.Instructions ?? baseMission.GetInstructions(locale),
                Category = ParseCategory(dto.Category ?? baseMission.Category),
                Difficulty = ParseDifficulty(dto.Difficulty ?? baseMission.Difficulty),
                GoalTrack = GoalTrack.NewCity,
                EstimatedMinutes = dto.EstimatedMinutes ?? baseMission.EstimatedMinutes,
                Tags = dto.Tags ?? baseMission.Tags,
                ProofSuggestion = dto.ProofSuggestion ?? baseMission.GetProofSuggestion(locale),
                Status = MissionStatus.Assigned,
                MissionLocalDate = DateTime.Now.Date.ToString("yyyy-MM-dd"),
                CreatedAtUtc = DateTime.UtcNow
            };
            return new PersonalizationResult(mission, true);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Mission personalization timed out or was cancelled");
            return new PersonalizationResult(CreateFallbackMission(baseMission), false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mission personalization failed");
            return new PersonalizationResult(CreateFallbackMission(baseMission), false);
        }
    }

    private static Mission CreateFallbackMission(CuratedMission baseMission)
    {
        var lang = CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName ?? "en";
        return new Mission
        {
            Id = Guid.NewGuid().ToString(),
            CuratedId = baseMission.Id,
            Title = baseMission.GetTitle(lang),
            Description = baseMission.GetDescription(lang),
            Instructions = baseMission.GetInstructions(lang),
            Category = ParseCategory(baseMission.Category),
            Difficulty = ParseDifficulty(baseMission.Difficulty),
            GoalTrack = GoalTrack.NewCity,
            EstimatedMinutes = baseMission.EstimatedMinutes,
            Tags = baseMission.Tags,
            ProofSuggestion = baseMission.GetProofSuggestion(lang),
            Status = MissionStatus.Assigned,
            MissionLocalDate = DateTime.Now.Date.ToString("yyyy-MM-dd"),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static MissionCategory ParseCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return MissionCategory.Explore;
        return Enum.TryParse<MissionCategory>(value, ignoreCase: true, out var cat)
            ? cat
            : MissionCategory.Explore;
    }

    private static MissionDifficulty ParseDifficulty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return MissionDifficulty.Easy;
        return Enum.TryParse<MissionDifficulty>(value, ignoreCase: true, out var diff)
            ? diff
            : MissionDifficulty.Easy;
    }

    private class ApiMissionDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<string>? Instructions { get; set; }
        public string? Category { get; set; }
        public string? Difficulty { get; set; }
        public int? EstimatedMinutes { get; set; }
        public List<string>? Tags { get; set; }
        public string? ProofSuggestion { get; set; }
    }
}
