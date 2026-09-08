namespace Vamoriq.Models;

public class Mission
{
    public string Id { get; set; } = string.Empty;
    public string CuratedId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Instructions { get; set; } = new();
    public MissionCategory Category { get; set; }
    public MissionDifficulty Difficulty { get; set; }
    public GoalTrack GoalTrack { get; set; }
    public int EstimatedMinutes { get; set; }
    public List<string> Tags { get; set; } = new();
    public string ProofSuggestion { get; set; } = string.Empty;
    public MissionStatus Status { get; set; }
    public string MissionLocalDate { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ExpiredAtUtc { get; set; }
}

public enum MissionStatus
{
    Assigned = 0,
    Completed = 1,
    Skipped = 2,
    Missed = 3
}

public enum GoalTrack
{
    NewCity = 0
}

public enum MissionCategory
{
    Explore = 0,
    Social = 1,
    Communicate = 2,
    Fun = 3
}

public enum MissionDifficulty
{
    Easy = 0,
    Medium = 1,
    Bold = 2
}

public enum SkipReason
{
    TooBusy = 0,
    NotInterested = 1,
    NotSafeFeeling = 2,
    TechnicalIssue = 3,
    Other = 4
}
