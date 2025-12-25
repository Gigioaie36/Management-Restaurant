using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.ViewModels
{
    public class InventoryViewModel : ViewModelBase, IDisposable
    {
        private readonly RestaurantDbContext _context;

        public ObservableCollection<Ingredient> Ingredients { get; set; } = new ObservableCollection<Ingredient>();

        public ICommand LoadDataCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public ICommand AddIngredientCommand { get; }
        public ICommand DeleteIngredientCommand { get; }

        private string _newIngredientName = string.Empty;
        public string NewIngredientName
        {
            get => _newIngredientName;
            set => SetProperty(ref _newIngredientName, value);
        }

        private double _newIngredientQuantity;
        public double NewIngredientQuantity
        {
            get => _newIngredientQuantity;
            set => SetProperty(ref _newIngredientQuantity, value);
        }

        private string _newIngredientUnit = "kg";
        public string NewIngredientUnit
        {
            get => _newIngredientUnit;
            set => SetProperty(ref _newIngredientUnit, value);
        }

        public InventoryViewModel(RestaurantDbContext context)
        {
            _context = context;
        }

        public InventoryViewModel() : this(new RestaurantDbContext())
        {
            LoadDataCommand = new RelayCommand(_ => LoadData());
            SaveChangesCommand = new RelayCommand(_ => SaveChanges());
            AddIngredientCommand = new RelayCommand(_ => AddIngredient());
            DeleteIngredientCommand = new RelayCommand(param => DeleteIngredient(param as Ingredient));
            
            LoadData();
        }

        private void LoadData()
        {
            _context.Ingredients.Load();
            Ingredients = _context.Ingredients.Local.ToObservableCollection();
            
            if (Ingredients.Count == 0)
            {
                _context.Ingredients.Add(new Ingredient { Name = "Tomatoes", StockQuantity = 10, Unit = "kg" });
                _context.Ingredients.Add(new Ingredient { Name = "Cheese", StockQuantity = 5, Unit = "kg" });
                _context.Ingredients.Add(new Ingredient { Name = "Flour", StockQuantity = 20, Unit = "kg" });
                _context.Ingredients.Add(new Ingredient { Name = "Chicken Breast", StockQuantity = 15, Unit = "kg" });
                _context.Ingredients.Add(new Ingredient { Name = "Olive Oil", StockQuantity = 5, Unit = "liters" });
                _context.SaveChanges();
            }

            OnPropertyChanged(nameof(Ingredients));
        }

        private void SaveChanges()
        {
            _context.SaveChanges();
            System.Windows.MessageBox.Show("Inventory updated!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void AddIngredient()
        {
            if (string.IsNullOrWhiteSpace(NewIngredientName))
            {
                System.Windows.MessageBox.Show("Please enter a name.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var newIng = new Ingredient
            {
                Name = NewIngredientName,
                StockQuantity = NewIngredientQuantity,
                Unit = NewIngredientUnit
            };

            _context.Ingredients.Add(newIng);
            _context.SaveChanges(); // Auto-adds to local collection because of ToObservableCollection() logic usually, but sometimes safe to add manually if not using synced Local
            
            // Clear inputs
            NewIngredientName = string.Empty;
            NewIngredientQuantity = 0;
            NewIngredientUnit = "kg";
        }

        private void DeleteIngredient(Ingredient? ingredient)
        {
            if (ingredient == null) return;

            var confirm = System.Windows.MessageBox.Show($"Delete {ingredient.Name}?", "Confirm", System.Windows.MessageBoxButton.YesNo);
            if (confirm == System.Windows.MessageBoxResult.Yes)
            {
                _context.Ingredients.Remove(ingredient);
                _context.SaveChanges();
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
