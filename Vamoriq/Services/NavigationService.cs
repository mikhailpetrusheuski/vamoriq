using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Services;

public class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;

    public NavigationService(ILogger<NavigationService> logger)
    {
        _logger = logger;
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        try
        {
            _logger.LogInformation("Navigating to route: {Route}", route);

            if (parameters != null)
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync(route);
            }

            _logger.LogInformation("Successfully navigated to route: {Route}", route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to route: {Route}", route);
            throw;
        }
    }

    public async Task GoBackAsync()
    {
        try
        {
            _logger.LogInformation("Navigating back");

            if (CanGoBack())
            {

                if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
                {
                    await Shell.Current.Navigation.PopAsync();
                }
                else
                {
                    await Shell.Current.GoToAsync("..");
                }
                _logger.LogInformation("Successfully navigated back");
            }
            else
            {
                _logger.LogWarning("Cannot go back - no previous page");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate back");
            throw;
        }
    }

    public async Task NavigateToModalAsync(string route, IDictionary<string, object>? parameters = null)
    {
        try
        {
            _logger.LogInformation("Navigating to modal route: {Route}", route);

            var navigationParameter = new Dictionary<string, object>
            {
                ["Modal"] = true
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    navigationParameter[param.Key] = param.Value;
                }
            }

            await Shell.Current.GoToAsync(route, navigationParameter);

            _logger.LogInformation("Successfully navigated to modal route: {Route}", route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to modal route: {Route}", route);
            throw;
        }
    }

    public async Task NavigateToRootAsync()
    {
        try
        {
            _logger.LogInformation("Navigating to root");

            await Shell.Current.GoToAsync("///Welcome");

            _logger.LogInformation("Successfully navigated to root");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to root");
            throw;
        }
    }

    public bool CanGoBack()
    {
        try
        {
            return Shell.Current.Navigation.NavigationStack.Count > 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if can go back");
            return false;
        }
    }
}
