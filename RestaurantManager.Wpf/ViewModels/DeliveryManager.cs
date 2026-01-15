using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using RestaurantManager.Wpf.Data;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.ViewModels
{
    public class DeliveryManager
    {
        private DispatcherTimer _deliveryTimer;
        private readonly List<DeliveryJob> _activeDeliveries = new List<DeliveryJob>();
        
        private class DeliveryJob
        {
            public int OrderId { get; set; }
            public int DriverId { get; set; }
            // Null means "Assigned but not yet Delivering" (e.g. Preparing)
            public DateTime? DeliveryStartTime { get; set; } 
        }

        public DeliveryManager()
        {
            // Run every 1 second for responsiveness
            _deliveryTimer = new DispatcherTimer();
            _deliveryTimer.Interval = TimeSpan.FromSeconds(1);
            _deliveryTimer.Tick += DeliveryLoop;
            _deliveryTimer.Start();
        }

        private void DeliveryLoop(object? sender, EventArgs e)
        {
            try
            {
                using (var context = new RestaurantDbContext())
                {
                    // 1. Assign Drivers to NEW Online Orders (Reserve them early)
                    AssignDrivers(context);

                    // 2. Monitor Active Deliveries (State transitions and Timer)
                    MonitorDeliveries(context);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delivery Loop Error: {ex.Message}");
            }
        }

        private void AssignDrivers(RestaurantDbContext context)
        {
            // Find orders that are NEW (just placed), are Online, and NOT already assigned
            // We want to lock the driver immediately.
            var candidateOrders = context.Orders
                .Where(o => (o.Status == OrderStatus.New || o.Status == OrderStatus.Preparing) 
                            && o.Table.TableNumber.StartsWith("Online-S"))
                .ToList();

            foreach (var order in candidateOrders)
            {
                // Skip if already in active list (driver already locked)
                if (_activeDeliveries.Any(d => d.OrderId == order.Id)) continue;
                // Also skip if order is Paid (just in case)
                if (order.Status == OrderStatus.Paid) continue;

                // Parse Sector
                string tableName = order.Table.TableNumber;
                if (tableName.Length > 8 && int.TryParse(tableName.Substring(8), out int sectorId))
                {
                    // Find a free driver
                    var driver = context.DeliveryDrivers
                        .FirstOrDefault(d => d.AssignedSector == sectorId && !d.IsBusy);

                    if (driver != null)
                    {
                        // Lock Driver
                        driver.IsBusy = true;
                        context.SaveChanges();

                        _activeDeliveries.Add(new DeliveryJob
                        {
                            OrderId = order.Id,
                            DriverId = driver.Id,
                            DeliveryStartTime = null // Not started delivering yet
                        });
                        
                        System.Diagnostics.Debug.WriteLine($"[Delivery] Driver {driver.Name} LOCKED for Order {order.Id} (Status: {order.Status})");
                    }
                }
            }
        }

        private void MonitorDeliveries(RestaurantDbContext context)
        {
            var jobsCompleted = new List<DeliveryJob>();

            foreach (var job in _activeDeliveries)
            {
                var order = context.Orders.Find(job.OrderId);
                var driver = context.DeliveryDrivers.Find(job.DriverId);

                if (order == null || driver == null) 
                {
                    // Anomaly
                     jobsCompleted.Add(job);
                     continue;
                }

                // If Order became PAID externally (e.g. cancelled?), release driver
                if (order.Status == OrderStatus.Paid && job.DeliveryStartTime == null)
                {
                     driver.IsBusy = false;
                     context.SaveChanges();
                     jobsCompleted.Add(job);
                     continue;
                }

                // Transition Logic:
                
                // Case 1: Preparing -> Served (Start Delivery Timer)
                if (order.Status == OrderStatus.Served && job.DeliveryStartTime == null)
                {
                    job.DeliveryStartTime = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"[Delivery] Order {order.Id} picked up. Delivery Timer Started.");
                }

                // Case 2: In Delivery (Timer Running)
                if (job.DeliveryStartTime.HasValue)
                {
                    if ((DateTime.Now - job.DeliveryStartTime.Value).TotalSeconds >= 20)
                    {
                        // Delivery Completed
                        order.Status = OrderStatus.Paid;
                        order.PaymentMethod = PaymentMethodType.Card;
                        driver.IsBusy = false;
                        context.SaveChanges();
                        
                        jobsCompleted.Add(job);
                        System.Diagnostics.Debug.WriteLine($"[Delivery] Order {order.Id} delivered by {driver.Name}. Driver Freed.");
                    }
                }
                
                // Case 3: Still New/Preparing. Do nothing. Driver remains IsBusy=true.
            }

            foreach (var job in jobsCompleted)
            {
                _activeDeliveries.Remove(job);
            }
        }
    }
}
