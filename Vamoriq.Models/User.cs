using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Vamoriq.Models;

public class User
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("email")]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("username")]
    [Required]
    public string Username { get; set; } = string.Empty;

    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonProperty("isEmailVerified")]
    public bool IsEmailVerified { get; set; } = false;

    [JsonProperty("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [JsonProperty("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonProperty("settings")]
    public UserSettings UserSettings { get; set; } = new();
}

public class UserSettings
{
    [JsonProperty("language")]
    public string Language { get; set; } = "en";

    [JsonProperty("notifications")]
    public NotificationSettings Notifications { get; set; } = new();

    [JsonProperty("theme")]
    public UserTheme Theme { get; set; } = UserTheme.System;
}

public class NotificationSettings
{
    [JsonProperty("dailyReminder")]
    public bool DailyReminder { get; set; } = true;

    [JsonProperty("subscriptionUpdates")]
    public bool SubscriptionUpdates { get; set; } = true;
}

public enum UserTheme
{
    Light,
    Dark,
    System
}
