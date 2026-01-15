using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Wpf.Models
{
    public class DeliveryDriver
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // Sector ID (1-6) that the driver is restricted to
        [Range(1, 6)]
        public int AssignedSector { get; set; }

        public bool IsBusy { get; set; } = false;
    }
}
