using Vamoriq.ViewModels;

namespace Vamoriq.Views
{
    public partial class MissionPage : ContentPage
    {
        public MissionPage(MissionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is MissionViewModel vm)
                await vm.OnAppearingAsync();
        }
    }
}
