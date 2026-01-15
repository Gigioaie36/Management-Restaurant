using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.ViewModels
{
    public class KitchenViewModel : ViewModelBase, IDisposable
    {
        private readonly RestaurantDbContext _context;
        private readonly DispatcherTimer _timer;

        // Active orders (New, Preparing, Served)
        public ObservableCollection<Order> ActiveOrders { get; set; } = new ObservableCollection<Order>();

        // History of completed orders (Paid)
        public ObservableCollection<Order> HistoryOrders { get; set; } = new ObservableCollection<Order>();

        public ICommand RefreshCommand { get; }

        public KitchenViewModel(RestaurantDbContext context)
        {
            _context = context;

            RefreshCommand = new RelayCommand(_ => LoadData());

            // Initialize Timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            LoadData();
        }

        private void LoadData()
        {
            // Reload everything from DB
            // Note: In a real app we might care about tracking changes more carefully to not break UI state,
            // but here we just want to see the latest.
            
            // We need to clear and reload locally. 
            // _context.Orders.Local could contain mixed stale data if we don't refresh.
            
            // 1. Load Active Orders (Not Paid)
            _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.Status != OrderStatus.Paid)
                .Load();

            var active = _context.Orders.Local
                .Where(o => o.Status != OrderStatus.Paid)
                .OrderBy(o => o.OrderDate)
                .ToList();

            ActiveOrders.Clear();
            foreach (var o in active) ActiveOrders.Add(o);

            // 2. Load History (Paid), last 20
            // We can't easily rely just on .Local for history if we want to fetch new ones from DB that might not be loaded.
            // But usually, we just loaded them. 
            // Actually, for history, we might want to query explicitly to get the latest completed ones.
            
            var history = _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.Status == OrderStatus.Paid)
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .ToList();

            HistoryOrders.Clear();
            foreach (var o in history) HistoryOrders.Add(o);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            bool changesMade = false;

            // Iterate over a copy to avoid collection modification issues
            foreach (var order in ActiveOrders.ToList()) 
            {
                if (order.Status == OrderStatus.Paid) continue;

                var timeElapsed = now - order.OrderDate;

                // --- 1. Order Status Transitions ---
                if (order.Status == OrderStatus.New)
                {
                    if (timeElapsed.TotalSeconds >= 5)
                    {
                        order.Status = OrderStatus.Preparing;
                        changesMade = true;
                    }
                }
                else if (order.Status == OrderStatus.Preparing)
                {
                    if (timeElapsed.TotalSeconds >= 10) // 5s New + 5s Preparing
                    {
                        order.Status = OrderStatus.Served;
                        order.ServedDate = now;
                        changesMade = true;
                    }
                }

                // --- 2. Table Status Enforcement ---
                // Rule: Occupied = New + Preparing + 5s (of Served)
                if (order.Table != null)
                {
                    if (order.Status == OrderStatus.New || order.Status == OrderStatus.Preparing)
                    {
                        // Strictly Occupied during New and Preparing
                        if (order.Table.Status != TableStatus.Occupied)
                        {
                            order.Table.Status = TableStatus.Occupied;
                            changesMade = true;
                        }
                    }
                    else if (order.Status == OrderStatus.Served)
                    {
                        var servedTime = order.ServedDate ?? now;
                        var timeSinceServed = now - servedTime;

                        if (timeSinceServed.TotalSeconds < 5)
                        {
                            // First 5 seconds of Served: Still Occupied
                            if (order.Table.Status != TableStatus.Occupied)
                            {
                                order.Table.Status = TableStatus.Occupied;
                                changesMade = true;
                            }
                        }
                        else
                        {
                            // After 5 seconds: AsteaptaNota (Waiting for bill)
                            if (order.Table.Status != TableStatus.AsteaptaNota)
                            {
                                order.Table.Status = TableStatus.AsteaptaNota;
                                changesMade = true;
                            }
                        }
                    }
                }
            }

            if (changesMade)
            {
                _context.SaveChanges();
            }
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }
}
