using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Vamoriq.Services.Utilities;

namespace Vamoriq.Core.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    protected readonly ILogger Logger;
    protected readonly StructuredLogger StructuredLogger;

    protected BaseViewModel(ILogger logger)
    {
        Logger = logger;
        StructuredLogger = new StructuredLogger(logger, GetType().Name);
    }

    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        StructuredLogger.LogError("ViewModel error occurred", new Exception(message), new { ErrorMessage = message });
    }

    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    protected async Task ExecuteAsync(Func<Task> operation, string? errorMessage = null)
    {
        using var operationScope = Logger.BeginScope(
            "ExecuteAsync Scope: ViewModel={ViewModel}, HasErrorMessage={HasErrorMessage}",
            GetType().Name,
            !string.IsNullOrEmpty(errorMessage));

        try
        {
            StructuredLogger.LogMethodEntry("ExecuteAsync", new { HasErrorMessage = !string.IsNullOrEmpty(errorMessage) });

            IsBusy = true;
            ClearError();
            await operation();

            StructuredLogger.LogMethodExit("ExecuteAsync");
        }
        catch (Exception ex)
        {
            var message = errorMessage ?? "An error occurred. Please try again.";
            SetError(message);
            StructuredLogger.LogError("ExecuteAsync failed", ex, new { ErrorMessage = message, OriginalError = errorMessage });
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? errorMessage = null)
    {
        try
        {
            IsBusy = true;
            ClearError();
            return await operation();
        }
        catch (Exception ex)
        {
            var message = errorMessage ?? "An error occurred. Please try again.";
            SetError(message);
            Logger.LogError(ex, message);
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public virtual Task OnAppearingAsync()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    protected virtual async Task RefreshAsync()
    {
        await Task.CompletedTask;
    }

    protected abstract Task GoBackAsync();
}
