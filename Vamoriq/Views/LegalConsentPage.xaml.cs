using Vamoriq.ViewModels;

namespace Vamoriq.Views;

public partial class LegalConsentPage : ContentPage
{
    public LegalConsentPage()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        EnsureBindingContext();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        EnsureBindingContext();
    }

    private void EnsureBindingContext()
    {
        if (BindingContext != null)
        {
            return;
        }

        var services = Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
        if (services == null)
        {
            return;
        }

        var vm = services.GetService<LegalConsentViewModel>();
        if (vm != null)
        {
            BindingContext = vm;
        }
    }
}
