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
        public ICommand DecreaseQuantityCommand { get; }
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
            DecreaseQuantityCommand = new RelayCommand(param => DecreaseQuantity(param as OrderItem));
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
                // Refresh list item hack
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

        private void DecreaseQuantity(OrderItem? orderItem)
        {
            if (orderItem == null) return;

            orderItem.Quantity--;
            if (orderItem.Quantity <= 0)
            {
                OrderItems.Remove(orderItem);
            }
            else
            {
                // Refresh list item hack to update Total and Quantity binding
                var index = OrderItems.IndexOf(orderItem);
                OrderItems.RemoveAt(index);
                OrderItems.Insert(index, orderItem);
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

            // 1. Validation: Check if we have enough stock for ALL items
            var requiredQuantities = new Dictionary<int, double>();
            var insufficientIngredients = new List<string>();

            // Calculate total requirements for the entire order
            foreach (var orderLine in OrderItems)
            {
                // Use a separate context query or detached list to avoid messing with tracking if needed, 
                // but here we just need read access.
                var recipes = _context.RecipeItems
                    .Include(r => r.Ingredient)
                    .Where(r => r.MenuItemId == orderLine.MenuItemId)
                    .ToList();

                foreach (var recipe in recipes)
                {
                    if (recipe.Ingredient != null)
                    {
                        if (!requiredQuantities.ContainsKey(recipe.IngredientId))
                        {
                            requiredQuantities[recipe.IngredientId] = 0;
                        }
                        requiredQuantities[recipe.IngredientId] += recipe.QuantityRequired * orderLine.Quantity;
                    }
                }
            }

            // Check against current stock
            foreach (var req in requiredQuantities)
            {
                var ingredient = _context.Ingredients.Find(req.Key);
                if (ingredient != null)
                {
                    if (ingredient.StockQuantity < req.Value)
                    {
                        insufficientIngredients.Add($"- {ingredient.Name} (Required: {req.Value} {ingredient.Unit}, Available: {ingredient.StockQuantity} {ingredient.Unit})");
                    }
                }
            }

            if (insufficientIngredients.Any())
            {
                MessageBox.Show($"Cannot place order. Insufficient stock for:\n{string.Join("\n", insufficientIngredients)}", 
                    "Inventory Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Stop execution, do not deduct, do not save order
            }

            // Deduct from Stock
            foreach (var orderLine in OrderItems)
            {
                var recipes = _context.RecipeItems
                    .Include(r => r.Ingredient)
                    .Where(r => r.MenuItemId == orderLine.MenuItemId)
                    .ToList();

                foreach (var recipe in recipes)
                {
                    if (recipe.Ingredient != null)
                    {
                        var quantityToDeduct = recipe.QuantityRequired * orderLine.Quantity;
                        recipe.Ingredient.StockQuantity -= quantityToDeduct;
                    }
                }
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
