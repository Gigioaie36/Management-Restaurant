using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;
using RestaurantManager.Wpf.Views;

namespace RestaurantManager.Wpf.ViewModels
{
    public class OnlineOrdersViewModel : ViewModelBase
    {
        private readonly RestaurantDbContext _context;
        private DispatcherTimer _refreshTimer;

        public ICommand SelectSectorCommand { get; }

        // Availability Properties
        private bool _isS1Available = true;
        public bool IsS1Available { get => _isS1Available; set => SetProperty(ref _isS1Available, value); }

        private bool _isS2Available = true;
        public bool IsS2Available { get => _isS2Available; set => SetProperty(ref _isS2Available, value); }

        private bool _isS3Available = true;
        public bool IsS3Available { get => _isS3Available; set => SetProperty(ref _isS3Available, value); }

        private bool _isS4Available = true;
        public bool IsS4Available { get => _isS4Available; set => SetProperty(ref _isS4Available, value); }

        private bool _isS5Available = true;
        public bool IsS5Available { get => _isS5Available; set => SetProperty(ref _isS5Available, value); }

        private bool _isS6Available = true;
        public bool IsS6Available { get => _isS6Available; set => SetProperty(ref _isS6Available, value); }

        public OnlineOrdersViewModel()
        {
            _context = new RestaurantDbContext();
            
            // Initial check
            RefreshAvailability(null, null);

            // Timer for refreshing availability
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(2);
            _refreshTimer.Tick += RefreshAvailability;
            _refreshTimer.Start();

            SelectSectorCommand = new RelayCommand(param => SelectSector(param));
        }

        private void RefreshAvailability(object? sender, EventArgs? e)
        {
            try
            {
                // We need a fresh context or reload to get latest status if other VMs update DB
                using (var ctx = new RestaurantDbContext())
                {
                    bool CheckSector(int s) => ctx.DeliveryDrivers.Any(d => d.AssignedSector == s && !d.IsBusy);

                    IsS1Available = CheckSector(1);
                    IsS2Available = CheckSector(2);
                    IsS3Available = CheckSector(3);
                    IsS4Available = CheckSector(4);
                    IsS5Available = CheckSector(5);
                    IsS6Available = CheckSector(6);
                }
            }
            catch { }
        }

        private void SelectSector(object? param)
        {
            if (param is string sectorIdStr && int.TryParse(sectorIdStr, out int sectorId))
            {
                // Strict Validation Check
                using (var ctx = new RestaurantDbContext())
                {
                    bool anyFree = ctx.DeliveryDrivers.Any(d => d.AssignedSector == sectorId && !d.IsBusy);
                    if (!anyFree)
                    {
                        MessageBox.Show($"Niciun livrator DISPONIBIL pentru Sectorul {sectorId}. Asteapta finalizarea livrarilor curente.", 
                            "Livratori Ocupati", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                OpenOrderForSector(sectorId);
            }
        }

        private void OpenOrderForSector(int sectorId)
        {
            try
            {
                string tableName = $"Online-S{sectorId}";
                var table = _context.Tables.FirstOrDefault(t => t.TableNumber == tableName);
                if (table == null)
                {
                    table = new RestaurantTable
                    {
                        TableNumber = tableName,
                        Capacity = 100,
                        Status = TableStatus.Occupied
                    };
                    _context.Tables.Add(table);
                    _context.SaveChanges();
                }
                
                var orderVm = new OrderSelectionViewModel(_context, table.Id);
                var orderWindow = new OrderSelectionWindow
                {
                    DataContext = orderVm,
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                orderVm.CloseAction = new Action(orderWindow.Close);
                orderWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening sector {sectorId}: {ex.Message}");
            }
        }
    }
}
