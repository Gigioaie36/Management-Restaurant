using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.ViewModels
{
    public class OrderViewModel : ViewModelBase, IDisposable
    {
        private readonly RestaurantDbContext _context;
        
        public ObservableCollection<RestaurantTable> Tables { get; set; } = new ObservableCollection<RestaurantTable>();

        public ICommand LoadTablesCommand { get; }
        public ICommand SelectTableCommand { get; }

        public OrderViewModel()
        {
            _context = new RestaurantDbContext();
            LoadTablesCommand = new RelayCommand(_ => LoadTables());
            SelectTableCommand = new RelayCommand(param => SelectTable(param as RestaurantTable));
            
            LoadTables();
        }

        private void LoadTables()
        {
            _context.Tables.Load();
            Tables = _context.Tables.Local.ToObservableCollection();
            OnPropertyChanged(nameof(Tables));

            // Ensure we have some tables for testing if empty
            if (Tables.Count == 0)
            {
                _context.Tables.Add(new RestaurantTable { TableNumber = "1", Capacity = 4, Status = TableStatus.Free });
                _context.Tables.Add(new RestaurantTable { TableNumber = "2", Capacity = 2, Status = TableStatus.Free });
                _context.Tables.Add(new RestaurantTable { TableNumber = "3", Capacity = 6, Status = TableStatus.Free });
                _context.SaveChanges();
            }
        }

        private void SelectTable(RestaurantTable? table)
        {
            if (table == null) return;
             
            // Navigation logic to Order Details would go here. 
            // For now, we need to communicate with MainViewModel to switch view, 
            // OR use a dialog, OR show details in the same view.
            // I will use a simple approach: Navigate to a "TableOrderViewModel" via MainViewModel? 
            // Getting reference to MainViewModel is tricky without a Messenger/Service.
            // Simplified: I will expose a SelectedTableOrderVM property here and bind a ContentControl to it locally?
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
