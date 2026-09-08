using System.Text.Json;

namespace Vamoriq.Services.Models
{
    public class GenerateAIResponse
    {
        public GenerateAIPayload? GenerateAI { get; set; }
    }

    public class GenerateAIPayload
    {
        public AIResponse? GenerateAI { get; set; }
        public List<Error>? Errors { get; set; }
    }

    public class AIResponse
    {
        public bool Success { get; set; }
        public JsonElement? Data { get; set; }
        public string? Error { get; set; }
        public string? PromptId { get; set; }
        public string? Model { get; set; }
        public int? TokensUsed { get; set; }
        public long? ProcessingTimeMs { get; set; }
        public bool? Cached { get; set; }
        public JsonElement? Usage { get; set; }
    }

    public class Error
    {
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class GenerateImageResponse
    {
        public GenerateImagePayload? GenerateImage { get; set; }
    }

    public class GenerateImagePayload
    {
        public string? GenerateImage { get; set; }
    }
}
