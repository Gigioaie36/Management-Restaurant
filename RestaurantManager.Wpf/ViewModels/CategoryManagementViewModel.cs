using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.ViewModels
{
    public class CategoryManagementViewModel : ViewModelBase
    {
        private readonly RestaurantDbContext _context;

        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                // When selecting, populate the "Edit" name potentially? 
                // Alternatively, bind directly to SelectedCategory.Name in UI.
            }
        }

        private string _newCategoryName = string.Empty;
        public string NewCategoryName
        {
            get => _newCategoryName;
            set => SetProperty(ref _newCategoryName, value);
        }

        public ICommand AddCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public Action? CloseAction { get; set; }

        public CategoryManagementViewModel(RestaurantDbContext context)
        {
            _context = context;

            // Load Categories locally to track changes
            _context.Categories.Load();
            Categories = _context.Categories.Local.ToObservableCollection();

            AddCategoryCommand = new RelayCommand(_ => AddCategory());
            DeleteCategoryCommand = new RelayCommand(param => DeleteCategory(param as Category));
            SaveChangesCommand = new RelayCommand(_ => SaveChanges());
        }

        private void AddCategory()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                MessageBox.Show("Category name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Categories.Any(c => c.Name.Equals(NewCategoryName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Category already exists.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newCategory = new Category { Name = NewCategoryName };
            _context.Categories.Add(newCategory);
            _context.SaveChanges();

            NewCategoryName = string.Empty;
        }

        private void DeleteCategory(Category? category)
        {
            if (category == null) return;

            // Check if used
            var isUsed = _context.MenuItems.Any(m => m.CategoryId == category.Id);
            if (isUsed)
            {
                MessageBox.Show("Cannot delete this category because it contains menu items. Delete or move the items first.", "Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to delete category '{category.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
        }

        private void SaveChanges()
        {
            _context.SaveChanges();
            MessageBox.Show("Changes saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
