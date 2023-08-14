using System;
using Play.Common.Entities;

namespace Play.Catalog.Service.Entities
{
    public class Item : IEntity<Guid>// This is an Entity class, is different than a DTO that will just be send or be received, Entity object will be modified while the DTO objects shouldn't be modified.
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }
}