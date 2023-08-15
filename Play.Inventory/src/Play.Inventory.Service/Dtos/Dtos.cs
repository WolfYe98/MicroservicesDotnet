using System;

namespace Play.Inventory.Service.Dtos
{
    public record GrantItemsDto(Guid UserId, Guid ItemID, int Quantity);
    public record InventoryItemDto(Guid ItemId, int Quantity, string Name, string Description, DateTimeOffset AcquiredDate);
    public record CatalogItemDto(Guid Id, string Name, string Description);
}