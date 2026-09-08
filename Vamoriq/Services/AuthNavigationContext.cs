namespace Vamoriq.Services;

public static class AuthNavigationContext
{
    private static readonly object _gate = new();
    private static string? _nextIdpHint;

    public static void SetNextIdpHint(string? idpHint)
    {
        lock (_gate)
        {
            _nextIdpHint = string.IsNullOrWhiteSpace(idpHint) ? null : idpHint.Trim().ToLowerInvariant();
        }
    }

    public static string? ConsumeNextIdpHint()
    {
        lock (_gate)
        {
            var value = _nextIdpHint;
            _nextIdpHint = null;
            return value;
        }
    }
}
