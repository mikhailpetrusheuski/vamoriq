using Vamoriq.ViewModels;
using Vamoriq.Services;

namespace Vamoriq.Views;

public partial class AuthPage : ContentPage, IQueryAttributable
{
    private string? _idpHint;
    private string? _lastStartedIdpHint;
    private bool _hasStartedOnce;

    private static string? TryGetQueryParam(Uri? uri, string key)
    {
        if (uri == null)
        {
            return null;
        }

        var query = uri.Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        if (query.StartsWith("?", StringComparison.Ordinal))
        {
            query = query.Substring(1);
        }

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 0)
            {
                continue;
            }

            if (!string.Equals(kv[0], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (kv.Length < 2)
            {
                return string.Empty;
            }

            return Uri.UnescapeDataString(kv[1]);
        }

        return null;
    }

    public AuthPage(AuthViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("idp", out var idpValue) && idpValue != null)
        {
            _idpHint = Uri.UnescapeDataString(idpValue.ToString() ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
        }
        else
        {
            _idpHint = null;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var idpFromContext = AuthNavigationContext.ConsumeNextIdpHint();
        if (!string.IsNullOrWhiteSpace(idpFromContext))
        {
            _idpHint = idpFromContext;
        }
        else
        {

            _idpHint = null;

            try
            {
                var currentLocation = Shell.Current?.CurrentState?.Location;
                var idpFromLocation = TryGetQueryParam(currentLocation, "idp");
                _idpHint = string.IsNullOrWhiteSpace(idpFromLocation)
                    ? null
                    : idpFromLocation.Trim().ToLowerInvariant();
            }
            catch
            {

            }
        }

        if (_hasStartedOnce && string.Equals(_lastStartedIdpHint, _idpHint, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastStartedIdpHint = _idpHint;
        _hasStartedOnce = true;

        if (BindingContext is AuthViewModel vm)
        {

            _ = vm.StartAuthenticationAsync(_idpHint);
        }
    }
}
