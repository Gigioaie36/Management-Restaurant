using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Wpf.Models
{
    public class RecipeItem
    {
        [Key]
        public int Id { get; set; }

        public int MenuItemId { get; set; }

        [ForeignKey(nameof(MenuItemId))]
        public MenuItem? MenuItem { get; set; }

        public int IngredientId { get; set; }

        [ForeignKey(nameof(IngredientId))]
        public Ingredient? Ingredient { get; set; }

        public double QuantityRequired { get; set; }
    }
}
