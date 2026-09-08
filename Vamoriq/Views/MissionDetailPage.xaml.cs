using Vamoriq.ViewModels;

namespace Vamoriq.Views
{
    public partial class MissionDetailPage : ContentPage
    {
        public MissionDetailPage(MissionDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
