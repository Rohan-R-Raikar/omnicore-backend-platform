namespace OmniCore.InventoryService.Models.Exceptions;

public class InventoryConcurrencyException : Exception
{
    public InventoryConcurrencyException()
        : base("Inventory was modified by another request. Please retry.")
    {
    }
}