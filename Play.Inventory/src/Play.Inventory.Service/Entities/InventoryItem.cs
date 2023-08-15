using System;
using System.Security.Cryptography.X509Certificates;
using Play.Common.Entities;

namespace Play.Inventory.Service.Entities
{
    public class InventoryItem : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public DateTimeOffset AcquiredDate { get; set; }
    }
}