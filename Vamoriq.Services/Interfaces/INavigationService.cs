namespace Vamoriq.Services.Interfaces;

public interface INavigationService
{

    Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);

    Task GoBackAsync();

    Task NavigateToModalAsync(string route, IDictionary<string, object>? parameters = null);

    Task NavigateToRootAsync();

    bool CanGoBack();
}
