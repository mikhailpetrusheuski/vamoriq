using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Vamoriq.Services.Utilities;

public class StructuredLogger
{
    private readonly ILogger _logger;
    private readonly string _category;

    public StructuredLogger(ILogger logger, string category)
    {
        _logger = logger;
        _category = category;
    }

    public void LogWithFullContext(LogLevel level, string message, object? data = null, Exception? exception = null, Dictionary<string, object>? additionalContext = null)
    {
        var context = new Dictionary<string, object>
        {
            ["Category"] = _category,
            ["Timestamp"] = DateTime.UtcNow,
            ["Message"] = message
        };

        if (data != null)
        {
            context["Data"] = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
        }

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        using var scope = _logger.BeginScope(context);

        if (exception != null)
        {
            _logger.Log(level, exception, message);
        }
        else
        {
            _logger.Log(level, message);
        }
    }

    public void LogMethodEntry(string methodName, object? parameters = null)
    {
        var context = new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Action"] = "Entry"
        };

        if (parameters != null)
        {
            context["Parameters"] = JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = false });
        }

        LogWithFullContext(LogLevel.Debug, $"Entering {methodName}", parameters, additionalContext: context);
    }

    public void LogMethodExit(string methodName, object? result = null, TimeSpan? duration = null)
    {
        var context = new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Action"] = "Exit"
        };

        if (result != null)
        {
            context["Result"] = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
        }

        if (duration.HasValue)
        {
            context["Duration"] = duration.Value.TotalMilliseconds;
        }

        LogWithFullContext(LogLevel.Debug, $"Exiting {methodName}", result, additionalContext: context);
    }

    public void LogError(string message, Exception exception, object? context = null)
    {
        var errorContext = new Dictionary<string, object>
        {
            ["ErrorType"] = exception.GetType().Name,
            ["ErrorMessage"] = exception.Message,
            ["StackTrace"] = exception.StackTrace ?? string.Empty
        };

        if (context != null)
        {
            errorContext["Context"] = JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = false });
        }

        LogWithFullContext(LogLevel.Error, message, context, exception, errorContext);
    }

    public void LogWarning(string message, object? data = null, Dictionary<string, object>? additionalContext = null)
    {
        LogWithFullContext(LogLevel.Warning, message, data, additionalContext: additionalContext);
    }

    public void LogInformation(string message, object? data = null, Dictionary<string, object>? additionalContext = null)
    {
        LogWithFullContext(LogLevel.Information, message, data, additionalContext: additionalContext);
    }

    public void LogDebug(string message, object? data = null, Dictionary<string, object>? additionalContext = null)
    {
        LogWithFullContext(LogLevel.Debug, message, data, additionalContext: additionalContext);
    }
}
