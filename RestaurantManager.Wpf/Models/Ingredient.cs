using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Wpf.Models
{
    public class Ingredient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public double StockQuantity { get; set; }

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } = "kg"; // e.g., kg, liters, pcs
    }
}
