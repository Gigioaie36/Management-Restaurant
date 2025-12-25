using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;
using RestaurantManager.Wpf.ViewModels;
using Xunit;

namespace RestaurantManager.Tests
{
    public class MenuViewModelTests
    {
        [Fact]
        public void AddMenuItem_ShouldAddToCollection()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<RestaurantDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDB_Add")
                .Options;

            using (var context = new RestaurantDbContext(options))
            {
                // Seed a category
                context.Categories.Add(new Category { Id = 1, Name = "Test Category" });
                context.SaveChanges();

                var viewModel = new MenuViewModel(context);

                // Act
                viewModel.AddMenuItemCommand.Execute(null);

                // Assert
                Assert.NotEmpty(viewModel.MenuItems);
                Assert.Contains(viewModel.MenuItems, item => item.Name == "New Item");
            }
        }

        [Fact]
        public void DeleteMenuItem_ShouldRemoveFromCollection()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<RestaurantDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDB_Delete")
                .Options;

            using (var context = new RestaurantDbContext(options))
            {
                var item = new MenuItem { Id = 1, Name = "To Delete", Price = 10, CategoryId = 1 };
                context.MenuItems.Add(item);
                context.SaveChanges();

                var viewModel = new MenuViewModel(context);
                viewModel.SelectedMenuItem = viewModel.MenuItems.First();

                // Act
                viewModel.DeleteMenuItemCommand.Execute(null);

                // Assert
                Assert.Empty(viewModel.MenuItems);
            }
        }
    }
}
