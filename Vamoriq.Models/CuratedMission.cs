namespace Vamoriq.Models;

public class CuratedMissionTranslation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Instructions { get; set; } = new();
    public string ProofSuggestion { get; set; } = string.Empty;
}

public class CuratedMission
{
    public string Id { get; set; } = string.Empty;
    public string BaseTitle { get; set; } = string.Empty;
    public string BaseDescription { get; set; } = string.Empty;
    public List<string> BaseInstructions { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string GoalTrack { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public List<string> Tags { get; set; } = new();
    public string ProofSuggestion { get; set; } = string.Empty;
    public string Tier { get; set; } = "medium";

    public CuratedMissionTranslation? Translation { get; set; }

    public string GetTitle(string lang) =>
        lang != "en" && Translation is { Title.Length: > 0 } ? Translation.Title : BaseTitle;

    public string GetDescription(string lang) =>
        lang != "en" && Translation is { Description.Length: > 0 } ? Translation.Description : BaseDescription;

    public List<string> GetInstructions(string lang) =>
        lang != "en" && Translation is not null && Translation.Instructions.Count > 0 ? Translation.Instructions : BaseInstructions;

    public string GetProofSuggestion(string lang) =>
        lang != "en" && Translation is { ProofSuggestion.Length: > 0 } ? Translation.ProofSuggestion : ProofSuggestion;
}
