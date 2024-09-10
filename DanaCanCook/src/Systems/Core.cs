using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace DanaCanCook;

public class Core : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("DanaCanCook.ItemSandwich", typeof(ItemSandwich));
        api.RegisterBlockClass("DanaCanCook.BlockCuttingBoard", typeof(BlockCuttingBoard));
        api.RegisterBlockEntityClass("DanaCanCook.CuttingBoard", typeof(BlockEntityCuttingBoard));
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        foreach (CollectibleObject obj in api.World.Collectibles)
        {
            if (obj == null || obj.Attributes == null)
            {
                continue;
            }

            if (obj.Attributes.KeyExists(attributeWhenOnSandwich) && !obj.CreativeInventoryTabs.Contains(ModId))
            {
                obj.CreativeInventoryTabs = obj.CreativeInventoryTabs.Append(ModId);
            }
        }
    }
}
