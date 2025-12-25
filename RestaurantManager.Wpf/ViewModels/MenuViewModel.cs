using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantManager.Wpf.ViewModels
{
    public class MenuViewModel : ViewModelBase, IDisposable
    {
        private readonly RestaurantDbContext _context;

        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();
        public ObservableCollection<MenuItem> MenuItems { get; set; } = new ObservableCollection<MenuItem>();
        
        // Recipe Management
        public ObservableCollection<Ingredient> AvailableIngredients { get; set; } = new ObservableCollection<Ingredient>();
        public ObservableCollection<RecipeItem> PendingRecipeItems { get; set; } = new ObservableCollection<RecipeItem>();

        private MenuItem _selectedMenuItem;
        public MenuItem SelectedMenuItem
        {
            get => _selectedMenuItem;
            set => SetProperty(ref _selectedMenuItem, value);
        }

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public ICommand AddMenuItemCommand { get; }
        public ICommand DeleteMenuItemCommand { get; }
        public ICommand AddIngredientToRecipeCommand { get; }
        public ICommand RemoveIngredientFromRecipeCommand { get; }

        private string _newItemName = string.Empty;
        public string NewItemName
        {
            get => _newItemName;
            set => SetProperty(ref _newItemName, value);
        }

        private string _newItemDescription = string.Empty;
        public string NewItemDescription
        {
            get => _newItemDescription;
            set => SetProperty(ref _newItemDescription, value);
        }

        private decimal _newItemPrice;
        public decimal NewItemPrice
        {
            get => _newItemPrice;
            set => SetProperty(ref _newItemPrice, value);
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        // Selection for Recipe
        private Ingredient? _selectedIngredient;
        public Ingredient? SelectedIngredient
        {
            get => _selectedIngredient;
            set => SetProperty(ref _selectedIngredient, value);
        }

        private double _ingredientQuantity;
        public double IngredientQuantity
        {
            get => _ingredientQuantity;
            set => SetProperty(ref _ingredientQuantity, value);
        }

        public MenuViewModel() : this(new RestaurantDbContext())
        {
        }

        public MenuViewModel(RestaurantDbContext context)
        {
            _context = context;
            
            LoadDataCommand = new RelayCommand(_ => LoadData());
            SaveChangesCommand = new RelayCommand(_ => SaveChanges());
            AddMenuItemCommand = new RelayCommand(_ => AddMenuItem());
            DeleteMenuItemCommand = new RelayCommand(param => DeleteMenuItem(param as MenuItem));
            AddIngredientToRecipeCommand = new RelayCommand(_ => AddIngredientToRecipe());
            RemoveIngredientFromRecipeCommand = new RelayCommand(param => RemoveIngredientFromRecipe(param as RecipeItem));

            LoadData();
        }

        private void LoadData()
        {
            _context.Categories.Load();
            Categories = _context.Categories.Local.ToObservableCollection();

            _context.MenuItems.Include(m => m.Category).Load();
            MenuItems = _context.MenuItems.Local.ToObservableCollection();
            
            _context.Ingredients.Load();
            AvailableIngredients = _context.Ingredients.Local.ToObservableCollection();

            if (!Categories.Any())
            {
                _context.Categories.Add(new Category { Name = "Food" });
                _context.Categories.Add(new Category { Name = "Drinks" });
                _context.SaveChanges();
            }
        }

        private void AddIngredientToRecipe()
        {
            if (SelectedIngredient == null)
            {
                System.Windows.MessageBox.Show("Select an ingredient first.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (IngredientQuantity <= 0)
            {
                System.Windows.MessageBox.Show("Quantity must be positive.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // CRITICAL: Validation requested by USER - Cannot exceed existing stock
            if (IngredientQuantity > SelectedIngredient.StockQuantity)
            {
                System.Windows.MessageBox.Show($"Not enough stock! You requested {IngredientQuantity} {SelectedIngredient.Unit}, but only {SelectedIngredient.StockQuantity} {SelectedIngredient.Unit} is available.", "Stock Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var item = new RecipeItem
            {
                IngredientId = SelectedIngredient.Id,
                Ingredient = SelectedIngredient,
                QuantityRequired = IngredientQuantity
            };

            PendingRecipeItems.Add(item);
            
            // Reset quantity but keep ingredient selected maybe? no, clear both.
            IngredientQuantity = 0;
        }

        private void RemoveIngredientFromRecipe(RecipeItem? item)
        {
            if (item != null)
            {
                PendingRecipeItems.Remove(item);
            }
        }

        private void AddMenuItem()
        {
            if (string.IsNullOrWhiteSpace(NewItemName) || SelectedCategory == null)
            {
                System.Windows.MessageBox.Show("Name and Category are required.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var newItem = new MenuItem
            {
                Name = NewItemName,
                Description = NewItemDescription,
                Price = NewItemPrice,
                CategoryId = SelectedCategory.Id,
                Category = SelectedCategory
            };

            _context.MenuItems.Add(newItem);
            _context.SaveChanges(); 

            // Save Recipe Items
            foreach (var rItem in PendingRecipeItems)
            {
                rItem.MenuItemId = newItem.Id;
                _context.RecipeItems.Add(rItem);
            }
            _context.SaveChanges();

            _context.SaveChanges();
            
            // Clear Inputs
            NewItemName = string.Empty;
            NewItemDescription = string.Empty;
            NewItemPrice = 0;
            PendingRecipeItems.Clear();
        }

        private void DeleteMenuItem(MenuItem? menuItem)
        {
            if (menuItem != null)
            {
                _context.MenuItems.Remove(menuItem);
                _context.SaveChanges();
            }
        }

        private void SaveChanges()
        {
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
