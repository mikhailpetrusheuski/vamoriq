using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Services.Providers;

public class PromptProvider : IPromptProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PromptProvider>? _logger;

    public PromptProvider(IConfiguration configuration, ILogger<PromptProvider>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
    }

    public string? GetPromptId(string category, string locale)
    {
        _logger?.LogDebug("Getting prompt ID for category: {Category}, locale: {Locale}", category, locale);

        var configKey = $"PromptMapping:{category}:{locale}";
        var promptId = _configuration[configKey];

        if (string.IsNullOrEmpty(promptId))
        {
            _logger?.LogWarning("Prompt ID not found for category: {Category}, locale: {Locale}", category, locale);

            var fallbackKey = $"PromptMapping:{category}:en";
            promptId = _configuration[fallbackKey];

            if (!string.IsNullOrEmpty(promptId))
            {
                _logger?.LogInformation("Using fallback English prompt for category: {Category}", category);
            }
        }

        return promptId;
    }
}
