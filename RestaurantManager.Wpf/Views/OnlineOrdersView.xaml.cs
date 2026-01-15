using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace RestaurantManager.Wpf.Views
{
    public partial class OnlineOrdersView : UserControl
    {
        public OnlineOrdersView()
        {
            InitializeComponent();
        }

        private void Sector_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Path path && path.Tag is string sectorId)
            {
                if (DataContext is ViewModels.OnlineOrdersViewModel vm)
                {
                    if (vm.SelectSectorCommand.CanExecute(sectorId))
                    {
                        vm.SelectSectorCommand.Execute(sectorId);
                    }
                }
            }
        }
    }
}
