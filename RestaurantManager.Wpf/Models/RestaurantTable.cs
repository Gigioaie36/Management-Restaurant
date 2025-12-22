using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Wpf.Models
{
    public enum TableStatus
    {
        Free,
        Occupied,
        WaitingPayment
    }

    public class RestaurantTable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TableNumber { get; set; } = string.Empty;

        public TableStatus Status { get; set; } = TableStatus.Free;

        public int Capacity { get; set; }
    }
}
