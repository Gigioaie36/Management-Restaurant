using System.Windows.Controls;

namespace RestaurantManager.Wpf.Views
{
    public partial class MenuView : UserControl
    {
        public MenuView()
        {
            InitializeComponent();
        }

        private void ClearFilter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MenuViewModel vm)
            {
                vm.SelectedFilterCategory = null;
            }
        }
    }
}
