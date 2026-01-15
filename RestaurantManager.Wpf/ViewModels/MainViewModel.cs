using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Models;

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

        // Keep KitchenViewModel alive to run the timer
        private readonly KitchenViewModel _kitchenViewModel;

        public ICommand NavigateMenuCommand { get; }
        public ICommand NavigateOrdersCommand { get; }
        public ICommand NavigateKitchenCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand NavigateInventoryCommand { get; }
        public ICommand NavigateOnlineOrdersCommand { get; }
        public ICommand NavigateDeliveryCommand { get; }

        private DeliveryManager _deliveryManager; // Keeps logic alive

        public MainViewModel()
        {
            // Initialize Context once or per VM? 
            // Better to let them have their own context or share one? 
            // For Kitchen Timer to update others, they need to share DB or refresh often.
            // Kitchen updates DB. Orders View needs to Reload to see changes.
            
            _kitchenViewModel = new KitchenViewModel(new Data.RestaurantDbContext());

            // Default view
            _currentViewModel = new MenuViewModel();

            NavigateMenuCommand = new RelayCommand(_ => CurrentViewModel = new MenuViewModel());
            NavigateOrdersCommand = new RelayCommand(_ => CurrentViewModel = new OrderViewModel());
            NavigateKitchenCommand = new RelayCommand(_ => CurrentViewModel = _kitchenViewModel); // Use singleton
            NavigateReportsCommand = new RelayCommand(_ => CurrentViewModel = new ReportsViewModel());
            NavigateInventoryCommand = new RelayCommand(_ => CurrentViewModel = new InventoryViewModel());
            NavigateOnlineOrdersCommand = new RelayCommand(_ => CurrentViewModel = new OnlineOrdersViewModel());
            NavigateDeliveryCommand = new RelayCommand(_ => CurrentViewModel = new DeliveryViewModel());

            PerformStartupCleanup();
            _deliveryManager = new DeliveryManager(); 
        }

        private void PerformStartupCleanup()
        {
            try 
            {
                using (var context = new Data.RestaurantDbContext())
                {
                    // Ensure Database Table Exists (Manual Migration)
                    try 
                    {
                         string createTableSql = @"
                            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DeliveryDrivers' AND xtype='U')
                            CREATE TABLE [DeliveryDrivers] (
                                [Id] int NOT NULL IDENTITY,
                                [Name] nvarchar(max) NOT NULL,
                                [AssignedSector] int NOT NULL,
                                [IsBusy] bit NOT NULL DEFAULT 0,
                                CONSTRAINT [PK_DeliveryDrivers] PRIMARY KEY ([Id])
                            );";
                         context.Database.ExecuteSqlRaw(createTableSql);

                         // Update Schema: Add IsBusy if missing (Migration for existing DBs)
                         string alterTableSql = @"
                            IF EXISTS (SELECT * FROM sysobjects WHERE name='DeliveryDrivers' AND xtype='U')
                            AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeliveryDrivers]') AND name = 'IsBusy')
                            BEGIN
                                ALTER TABLE [DeliveryDrivers] ADD [IsBusy] bit NOT NULL DEFAULT 0;
                            END";
                         context.Database.ExecuteSqlRaw(alterTableSql);
                    }
                    catch (System.Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Table creation failed: {dbEx.Message}");
                    }

                    // Startup Cleanup: Reset all tables to Free and close any lingering active orders
                    // This ensures a clean state every time the application starts (good for demo/dev).
                    
                    var pendingOrders = context.Orders.Where(o => o.Status != OrderStatus.Paid).ToList();
                    foreach (var order in pendingOrders)
                    {
                        order.Status = OrderStatus.Paid; // Auto-close lingering orders
                        order.PaymentMethod = PaymentMethodType.Cash; // Default to Cash
                        if (order.ServedDate == null) order.ServedDate = System.DateTime.Now;
                    }

                    var tables = context.Tables.ToList();
                    foreach (var t in tables)
                    {
                        t.Status = TableStatus.Free;
                    }

                    // Reset Drivers
                    var drivers = context.DeliveryDrivers.ToList();
                    foreach (var d in drivers)
                    {
                        d.IsBusy = false;
                    }

                    context.SaveChanges();
                }
            }
            catch (System.Exception ex)
            {
                // Log or ignore if db issue on startup, ideally shouldn't happen
                System.Diagnostics.Debug.WriteLine($"Startup cleanup failed: {ex.Message}");
            }
        }
    }
}
