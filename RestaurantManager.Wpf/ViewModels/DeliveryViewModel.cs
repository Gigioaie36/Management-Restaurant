using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.ViewModels
{
    public class DeliveryViewModel : ViewModelBase
    {
        private readonly RestaurantDbContext _context;

        public ObservableCollection<DeliveryDriver> Drivers { get; set; } = new ObservableCollection<DeliveryDriver>();

        private string _newDriverName = string.Empty;
        public string NewDriverName
        {
            get => _newDriverName;
            set => SetProperty(ref _newDriverName, value);
        }

        private int _newDriverSector = 1;
        public int NewDriverSector
        {
            get => _newDriverSector;
            set => SetProperty(ref _newDriverSector, value);
        }

        // Collection for Sector selection (1-6)
        public ObservableCollection<int> Sectors { get; } = new ObservableCollection<int> { 1, 2, 3, 4, 5, 6 };

        public ICommand AddDriverCommand { get; }
        public ICommand DeleteDriverCommand { get; }
        public ICommand RefreshCommand { get; } // New Command

        public DeliveryViewModel()
        {
            _context = new RestaurantDbContext();
            
            // Ensure Table Exists - Simple approach for local dev
            // In strict env, we'd use migrations. Here we try to access or create.
            // Ideally MainViewModel handled this, but let's be safe.
            try { _context.Database.EnsureCreated(); } catch { }

            LoadDrivers();

            AddDriverCommand = new RelayCommand(_ => AddDriver());
            DeleteDriverCommand = new RelayCommand(param => DeleteDriver(param as DeliveryDriver));
            RefreshCommand = new RelayCommand(_ => LoadDrivers()); // Bind to LoadDrivers
        }

        private void LoadDrivers()
        {
            try
            {
                // Force Refresh: Clear local cache to get updates from other ViewModels (Service)
                _context.ChangeTracker.Clear();
                
                // Re-fetch everything
                var freshDrivers = _context.DeliveryDrivers.ToList();
                
                Drivers.Clear();
                foreach (var d in freshDrivers)
                {
                    Drivers.Add(d);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading drivers: {ex.Message}. Try restarting app to apply DB changes.");
            }
        }

        private void AddDriver()
        {
            if (string.IsNullOrWhiteSpace(NewDriverName))
            {
                MessageBox.Show("Please enter a driver name.");
                return;
            }

            var newDriver = new DeliveryDriver
            {
                Name = NewDriverName,
                AssignedSector = NewDriverSector
            };

            _context.DeliveryDrivers.Add(newDriver);
            try
            {
                _context.SaveChanges();
            }
            catch (System.Exception ex)
            {
                 // Fallback: Table might not exist. Try to create it via raw SQL (quick fix for localdb)
                 try 
                 {
                     string createTableSql = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DeliveryDrivers' AND xtype='U')
                        CREATE TABLE [DeliveryDrivers] (
                            [Id] int NOT NULL IDENTITY,
                            [Name] nvarchar(max) NOT NULL,
                            [AssignedSector] int NOT NULL,
                            CONSTRAINT [PK_DeliveryDrivers] PRIMARY KEY ([Id])
                        );";
                     _context.Database.ExecuteSqlRaw(createTableSql);
                     _context.SaveChanges();
                 }
                 catch (System.Exception innerEx)
                 {
                      MessageBox.Show($"Error adding driver: {ex.Message}\nRetry failed: {innerEx.Message}");
                      return;
                 }
            }

            Drivers.Add(newDriver); // Local update
            NewDriverName = string.Empty; // Reset form
        }

        private void DeleteDriver(DeliveryDriver? driver)
        {
            if (driver == null) return;

            var confirm = MessageBox.Show($"Delete driver {driver.Name}?", "Confirm", MessageBoxButton.YesNo);
            if (confirm == MessageBoxResult.Yes)
            {
                _context.DeliveryDrivers.Remove(driver);
                _context.SaveChanges();
                Drivers.Remove(driver);
            }
        }
    }
}
