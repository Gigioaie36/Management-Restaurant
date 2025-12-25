using System.Windows.Input;

namespace RestaurantManager.Wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand NavigateMenuCommand { get; }
        public ICommand NavigateOrdersCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand NavigateInventoryCommand { get; }

        public MainViewModel()
        {
            // Default view
            _currentViewModel = new MenuViewModel();

            NavigateMenuCommand = new RelayCommand(_ => CurrentViewModel = new MenuViewModel());
            NavigateOrdersCommand = new RelayCommand(_ => CurrentViewModel = new OrderViewModel());
            NavigateReportsCommand = new RelayCommand(_ => CurrentViewModel = new ReportsViewModel());
            NavigateInventoryCommand = new RelayCommand(_ => CurrentViewModel = new InventoryViewModel());
        }
    }
}
