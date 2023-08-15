using Play.Inventory.Service.Dtos;

namespace Play.Inventory.Service.Entities
{
    public static class Extensions
    {
        public static GrantItemsDto AsGrantItemDto(this InventoryItem inventoryItem)
        {
            return new GrantItemsDto(inventoryItem.UserId, inventoryItem.ItemId, inventoryItem.Quantity);
        }
        public static InventoryItemDto AsInventoryItemDto(this InventoryItem inventoryItem, string itemName, string itemDescription)
        {
            return new InventoryItemDto(inventoryItem.ItemId, inventoryItem.Quantity, itemName, itemDescription, inventoryItem.AcquiredDate);
        }
    }
}