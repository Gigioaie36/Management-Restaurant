using Microsoft.EntityFrameworkCore;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.Data
{
    public class RestaurantDbContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeItem> RecipeItems { get; set; }
        public DbSet<RestaurantTable> Tables { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RestaurantDB;Trusted_Connection=True;MultipleActiveResultSets=true");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed some initial data if needed, or configure specific relationships
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Aperitive" },
                new Category { Id = 2, Name = "Fel Principal" },
                new Category { Id = 3, Name = "Desert" },
                new Category { Id = 4, Name = "Bauturi" }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
