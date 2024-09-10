using Vintagestory.API.Common;

namespace DanaCanCook;

public class ItemSlotCuttingBoard : ItemSlot
{
    public ItemSlotCuttingBoard(InventoryBase inventory) : base(inventory)
    {
        this.inventory = inventory;
    }

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
        return IsStorable(sourceSlot?.Itemstack?.Collectible) && base.CanTakeFrom(sourceSlot, priority);
    }

    public override bool CanHold(ItemSlot fromSlot)
    {
        return IsStorable(fromSlot?.Itemstack?.Collectible) && base.CanHold(fromSlot);
    }

    public static bool IsStorable(CollectibleObject obj)
    {
        return obj?.Attributes?.KeyExists(attributeCodeCuttingBoard) == true && obj.Attributes[attributeCodeCuttingBoard].AsBool();
    }
}