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

        private MenuItem _selectedMenuItem;
        public MenuItem SelectedMenuItem
        {
            get => _selectedMenuItem;
            set => SetProperty(ref _selectedMenuItem, value);
        }

        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public ICommand AddMenuItemCommand { get; }
        public ICommand DeleteMenuItemCommand { get; }

        public MenuViewModel()
        {
            _context = new RestaurantDbContext();
            
            LoadDataCommand = new RelayCommand(_ => LoadData());
            SaveChangesCommand = new RelayCommand(_ => SaveChanges());
            AddMenuItemCommand = new RelayCommand(_ => AddMenuItem());
            DeleteMenuItemCommand = new RelayCommand(_ => DeleteMenuItem(), _ => SelectedMenuItem != null);

            LoadData();
        }

        private void LoadData()
        {
            _context.Categories.Load();
            _context.MenuItems.Include(m => m.Category).Load();

            Categories = _context.Categories.Local.ToObservableCollection();
            MenuItems = _context.MenuItems.Local.ToObservableCollection();
            
            OnPropertyChanged(nameof(Categories));
            OnPropertyChanged(nameof(MenuItems));
        }

        private void AddMenuItem()
        {
            var newItem = new MenuItem { Name = "New Item", Price = 0, CategoryId = Categories.FirstOrDefault()?.Id ?? 0 };
            _context.MenuItems.Add(newItem);
            // Local collection updates automatically because it is bound to EF Local view
            SelectedMenuItem = newItem;
        }

        private void DeleteMenuItem()
        {
            if (SelectedMenuItem != null)
            {
                _context.MenuItems.Remove(SelectedMenuItem);
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

