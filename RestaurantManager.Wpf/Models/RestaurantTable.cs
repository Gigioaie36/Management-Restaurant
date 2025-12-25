using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Wpf.Models
{
    public enum TableStatus
    {
        Free,
        Occupied
    }

    public class RestaurantTable : System.ComponentModel.INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TableNumber { get; set; } = string.Empty;

        private TableStatus _status = TableStatus.Free;
        public TableStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public int Capacity { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
