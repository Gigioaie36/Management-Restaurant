using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.ViewModels
{
    public class ReportItem
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class ReportsViewModel : ViewModelBase, IDisposable
    {
        private readonly RestaurantDbContext _context;

        public ObservableCollection<ReportItem> TopSellingItems { get; set; } = new ObservableCollection<ReportItem>();
        public ObservableCollection<ReportItem> CategoryRevenues { get; set; } = new ObservableCollection<ReportItem>();
        
        private string _averageServiceTime = "N/A";
        public string AverageServiceTime
        {
            get => _averageServiceTime;
            set => SetProperty(ref _averageServiceTime, value);
        }

        public ICommand RefreshReportsCommand { get; }

        public ReportsViewModel()
        {
            _context = new RestaurantDbContext();
            RefreshReportsCommand = new RelayCommand(_ => LoadReports());
            LoadReports();
        }

        private void LoadReports()
        {
            // Report 1: Top 5 Best Selling Items
            var topItems = _context.OrderItems
                .Include(oi => oi.MenuItem)
                .GroupBy(oi => oi.MenuItemId)
                .Select(g => new
                {
                    Name = g.First().MenuItem!.Name,
                    TotalQuantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToList();

            TopSellingItems.Clear();
            foreach (var item in topItems)
            {
                TopSellingItems.Add(new ReportItem { Name = item.Name, Value = item.TotalQuantity, Label = $"{item.TotalQuantity} pcs" });
            }

            // Report 2: Revenue per Category
            var catRevenues = _context.OrderItems
                .Include(oi => oi.MenuItem)
                .ThenInclude(mi => mi!.Category)
                .GroupBy(oi => oi.MenuItem!.Category!.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalRevenue = g.Sum(oi => oi.PriceAtOrder * oi.Quantity)
                })
                .ToList();

            CategoryRevenues.Clear();
            foreach (var cat in catRevenues)
            {
                CategoryRevenues.Add(new ReportItem { Name = cat.CategoryName, Value = (double)cat.TotalRevenue, Label = $"{cat.TotalRevenue:C}" });
            }

            // Report 3: Average Service Time
            // Time between OrderDate and ServedDate for served orders
            var serviceTimes = _context.Orders
                .Where(o => o.Status == OrderStatus.Served || o.Status == OrderStatus.Paid)
                .Where(o => o.ServedDate.HasValue)
                .ToList() // Client evaluation for TimeSpan calculation if EF doesn't translate DateDiff well for all providers
                .Select(o => (o.ServedDate!.Value - o.OrderDate).TotalMinutes)
                .ToList();

            if (serviceTimes.Any())
            {
                double avgMinutes = serviceTimes.Average();
                AverageServiceTime = $"{avgMinutes:F0} minutes";
            }
            else
            {
                AverageServiceTime = "No data";
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
