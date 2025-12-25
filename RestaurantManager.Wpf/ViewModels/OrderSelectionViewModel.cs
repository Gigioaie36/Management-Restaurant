using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.ViewModels
{
    public class OrderSelectionViewModel : ViewModelBase
    {
        private readonly RestaurantDbContext _context;
        private readonly int _tableId;
        private Order _currentOrder;

        public ObservableCollection<MenuItem> MenuItems { get; set; }
        public ObservableCollection<OrderItem> OrderItems { get; set; } = new ObservableCollection<OrderItem>();

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public ICommand AddToOrderCommand { get; }
        public ICommand RemoveFromOrderCommand { get; }
        public ICommand SaveOrderCommand { get; }
        
        // Action to close the window
        public Action CloseAction { get; set; }

        public OrderSelectionViewModel(RestaurantDbContext context, int tableId)
        {
            _context = context;
            _tableId = tableId;

            // Load Menu Items
            _context.MenuItems.Include(m => m.Category).Load();
            MenuItems = _context.MenuItems.Local.ToObservableCollection();

            // Initialize Commands
            AddToOrderCommand = new RelayCommand(param => AddToOrder(param as MenuItem));
            RemoveFromOrderCommand = new RelayCommand(param => RemoveFromOrder(param as OrderItem));
            SaveOrderCommand = new RelayCommand(_ => SaveOrder());
        }

        private void AddToOrder(MenuItem? menuItem)
        {
            if (menuItem == null) return;

            var existingItem = OrderItems.FirstOrDefault(oi => oi.MenuItemId == menuItem.Id);
            if (existingItem != null)
            {
                existingItem.Quantity++;
                // Trigger update manually or rely on property change if OrderItem implemented it. 
                // Since OrderItem is POCO, we remove and re-add or need a wrapper. 
                // Simplest solution for now: Re-calculate total.
                // Note: UI for Quantity might not update without INotifyPropertyChanged on OrderItem.
                // We will force a refresh of the collection or use a wrapper in a real app. 
                // For this quick implementation, let's just create a new entry for every click or simple Quantity update logic.
                
                // Hack to refresh the specific item in List if bound
                var index = OrderItems.IndexOf(existingItem);
                OrderItems.RemoveAt(index);
                OrderItems.Insert(index, existingItem);
            }
            else
            {
                OrderItems.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    MenuItem = menuItem,
                    PriceAtOrder = menuItem.Price,
                    Quantity = 1
                });
            }
            RecalculateTotal();
        }

        private void RemoveFromOrder(OrderItem? orderItem)
        {
            if (orderItem == null) return;
            OrderItems.Remove(orderItem);
            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            TotalAmount = OrderItems.Sum(oi => oi.PriceAtOrder * oi.Quantity);
        }

        private void SaveOrder()
        {
            if (OrderItems.Count == 0)
            {
                MessageBox.Show("Please add items to the order first.");
                return;
            }

            // Create the real Order in DB
            var infoTable = _context.Tables.FirstOrDefault(t => t.Id == _tableId);
            if (infoTable != null)
            {
                infoTable.Status = TableStatus.Occupied;
            }

            var newOrder = new Order
            {
                TableId = _tableId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.New,
                TotalAmount = TotalAmount,
                OrderItems = OrderItems.ToList() 
                // Note: We need to be careful with detached entities. 
                // The MenuItems in OrderItems are attached to Context, so it should be fine.
            };
            
            // Clean up OrderItems relationships to avoid EF trying to re-insert MenuItems if tracking is weird
            foreach(var item in newOrder.OrderItems)
            {
                item.MenuItem = null; // Use ID only
            }

            _context.Orders.Add(newOrder);
            _context.SaveChanges();

            CloseAction?.Invoke();
        }
    }
}
