using Vamoriq.ViewModels;

namespace Vamoriq.Views
{
    public partial class ProgressPage : ContentPage
    {
        public ProgressPage(ProgressViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ProgressViewModel vm)
                await vm.OnAppearingAsync();
        }
    }
}
