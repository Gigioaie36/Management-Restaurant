using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Wpf.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
