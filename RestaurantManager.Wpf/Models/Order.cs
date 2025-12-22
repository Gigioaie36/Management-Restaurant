using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Wpf.Models
{
    public enum OrderStatus
    {
        New,
        Preparing,
        Served,
        Paid
    }

    public enum PaymentMethodType
    {
        Cash,
        Card,
        None // For unpaid orders
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int TableId { get; set; }

        [ForeignKey(nameof(TableId))]
        public RestaurantTable? Table { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime? ServedDate { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.New;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.None;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
