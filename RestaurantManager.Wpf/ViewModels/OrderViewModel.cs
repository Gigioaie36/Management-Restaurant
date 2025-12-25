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
        public ICommand AddTableCommand { get; }
        public ICommand DeleteTableCommand { get; }

        private int _newTableCapacity = 4;
        public int NewTableCapacity
        {
            get => _newTableCapacity;
            set => SetProperty(ref _newTableCapacity, value);
        }

        public OrderViewModel()
        {
            _context = new RestaurantDbContext();
            LoadTablesCommand = new RelayCommand(_ => LoadTables());
            SelectTableCommand = new RelayCommand(param => SelectTable(param as RestaurantTable));
            AddTableCommand = new RelayCommand(_ => AddTable());
            DeleteTableCommand = new RelayCommand(param => DeleteTable(param as RestaurantTable));
            
            LoadTables();
        }

        private void AddTable()
        {
            int nextNumber = 1;
            if (Tables.Any())
            {
                // Try to parse numbers to find max, handling non-numeric gracefully
                var numbers = Tables.Select(t => int.TryParse(t.TableNumber, out int n) ? n : 0).ToList();
                nextNumber = numbers.Max() + 1;
            }

            var newTable = new RestaurantTable
            {
                TableNumber = nextNumber.ToString(),
                Capacity = NewTableCapacity,
                Status = TableStatus.Free
            };

            _context.Tables.Add(newTable);
            _context.SaveChanges();
        }

        private void DeleteTable(RestaurantTable? table)
        {
            if (table == null) return;
            
            if (table.Status != TableStatus.Free)
            {
                System.Windows.MessageBox.Show("Cannot delete an occupied table!", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var confirm = System.Windows.MessageBox.Show($"Are you sure you want to delete Table {table.TableNumber}?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            _context.Tables.Remove(table);
            _context.SaveChanges();
        }

        private bool _isDeleteMode;
        public bool IsDeleteMode
        {
            get => _isDeleteMode;
            set => SetProperty(ref _isDeleteMode, value);
        }

        private async void SelectTable(RestaurantTable? table)
        {
            try
            {
                if (table == null) return;

                if (IsDeleteMode)
                {
                    if (table.Status != TableStatus.Free)
                    {
                        System.Windows.MessageBox.Show("Cannot delete an occupied table!", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    var confirm = System.Windows.MessageBox.Show($"Delete Table {table.TableNumber}?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo);
                    if (confirm == System.Windows.MessageBoxResult.Yes)
                    {
                        _context.Tables.Remove(table);
                        _context.SaveChanges();
                        Tables.Remove(table);
                    }
                    return;
                }

                switch (table.Status)
                {
                    case TableStatus.Free:
                        // Open Order Selection Window
                        var orderVm = new OrderSelectionViewModel(_context, table.Id);
                        var orderWindow = new Views.OrderSelectionWindow
                        {
                            DataContext = orderVm
                        };
                        
                        orderVm.CloseAction = new Action(orderWindow.Close);
                        
                        orderWindow.ShowDialog();
                        
                        // Refresh table status (it might have changed to Occupied in the VM)
                        _context.Entry(table).Reload();
                        break;

                    case TableStatus.Occupied:
                        // Find the active order for this table
                        var activeOrder = _context.Orders
                            .Include(o => o.OrderItems)
                            .FirstOrDefault(o => o.TableId == table.Id && o.Status != OrderStatus.Paid);

                        decimal totalAmount = activeOrder?.TotalAmount ?? 0;
                        if (activeOrder != null)
                        {
                            totalAmount = activeOrder.OrderItems.Sum(oi => oi.PriceAtOrder * oi.Quantity);
                        }

                        // Simulate Payment Selection
                        var result = System.Windows.MessageBox.Show(
                            $"Total: {totalAmount} RON\nSelect Payment Method for Table {table.TableNumber}:\nYes = Card\nNo = Cash", 
                            "Payment", 
                            System.Windows.MessageBoxButton.YesNoCancel);
                        
                        if (result == System.Windows.MessageBoxResult.Cancel) return;

                        // Update Order Status
                        if (activeOrder != null)
                        {
                            activeOrder.Status = OrderStatus.Paid;
                            activeOrder.PaymentMethod = (result == System.Windows.MessageBoxResult.Yes) ? PaymentMethodType.Card : PaymentMethodType.Cash;
                            activeOrder.ServedDate = DateTime.Now;
                        }
                        
                        _context.SaveChanges(); 

                        await Task.Delay(3000); // Wait 3 seconds
                        
                        table.Status = TableStatus.Free;
                        break;
                }
                
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}\nStack: {ex.StackTrace}");
            }
        }

        private void LoadTables()
        {
            _context.Tables.Load();
            Tables = _context.Tables.Local.ToObservableCollection();
            
            // Sanitize data: Tables might be in an invalid state (e.g. WaitingPayment = 2) if DB wasn't cleared
            foreach (var t in Tables)
            {
                if ((int)t.Status > 1) // If invalid enum value
                {
                    t.Status = TableStatus.Free;
                }
            }
            _context.SaveChanges();

            OnPropertyChanged(nameof(Tables));

            if (Tables.Count < 10)
            {
                // Ensure initial 3 tables exist
                if (!Tables.Any(t => t.TableNumber == "1")) _context.Tables.Add(new RestaurantTable { TableNumber = "1", Capacity = 4, Status = TableStatus.Free });
                if (!Tables.Any(t => t.TableNumber == "2")) _context.Tables.Add(new RestaurantTable { TableNumber = "2", Capacity = 2, Status = TableStatus.Free });
                if (!Tables.Any(t => t.TableNumber == "3")) _context.Tables.Add(new RestaurantTable { TableNumber = "3", Capacity = 6, Status = TableStatus.Free });
                
                // Add 7 more tables
                if (!Tables.Any(t => t.TableNumber == "4")) _context.Tables.Add(new RestaurantTable { TableNumber = "4", Capacity = 2, Status = TableStatus.Free });
                if (!Tables.Any(t => t.TableNumber == "5")) _context.Tables.Add(new RestaurantTable { TableNumber = "5", Capacity = 4, Status = TableStatus.Free });
                if (!Tables.Any(t => t.TableNumber == "6")) _context.Tables.Add(new RestaurantTable { TableNumber = "6", Capacity = 8, Status = TableStatus.Free });
                if (!Tables.Any(t => t.TableNumber == "7")) _context.Tables.Add(new RestaurantTable { TableNumber = "7", Capacity = 4, Status = TableStatus.Free });
                if (!Tables.Any(t => t.TableNumber == "8")) _context.Tables.Add(new RestaurantTable { TableNumber = "8", Capacity = 2, Status = TableStatus.Free });
                if (!Tables.Any(t => t.TableNumber == "9")) _context.Tables.Add(new RestaurantTable { TableNumber = "9", Capacity = 6, Status = TableStatus.Free });
                if (!Tables.Any(t => t.TableNumber == "10")) _context.Tables.Add(new RestaurantTable { TableNumber = "10", Capacity = 8, Status = TableStatus.Free });

                _context.SaveChanges();
                
                // Refresh local collection
                _context.Tables.Load();
                Tables = _context.Tables.Local.ToObservableCollection();
                OnPropertyChanged(nameof(Tables));
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
