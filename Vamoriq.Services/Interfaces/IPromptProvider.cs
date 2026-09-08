namespace Vamoriq.Services.Interfaces;

public interface IPromptProvider
{

    string? GetPromptId(string category, string locale);
}
