using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Shared.Message;

namespace ECommerce.Shared.Events
{
    /// <summary>
    /// All events published by the Product Service.
    ///
    /// WHO PUBLISHES: Product Service
    /// WHO CONSUMES:
    ///   - ProductCreatedEvent → Inventory Service
    ///     (creates a stock record when a new product is added)
    ///   - ProductUpdatedEvent → Inventory Service
    ///     (updates product name/price in the stock record)
    ///
    /// WHY DOES INVENTORY CARE ABOUT PRODUCT EVENTS?
    /// Inventory Service stores stock levels per product.
    /// When a new product is created in Product Service,
    /// Inventory Service needs to create a corresponding
    /// stock record for it. It listens to this event
    /// instead of calling Product Service directly —
    /// loose coupling principle.
    /// </summary>

    public record ProductCreatedEvent : BaseMessage
    {
        public Guid ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }

        /// <summary>
        /// How many units are available when the product
        /// is first created. Inventory Service uses this
        /// to set the initial stock level.
        /// </summary>
        public int InitialStock { get; init; }
    }

    public record ProductUpdatedEvent : BaseMessage
    {
        public Guid ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
    }
}
