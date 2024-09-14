using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace DanaCanCook;

public class Core : ModSystem
{
    public Dictionary<string, WhenOnSandwichProperties> PropertiesPatches { get; set; } = new();

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("DanaCanCook.ItemSandwich", typeof(ItemSandwich));
        api.RegisterBlockClass("DanaCanCook.BlockCuttingBoard", typeof(BlockCuttingBoard));
        api.RegisterBlockEntityClass("DanaCanCook.CuttingBoard", typeof(BlockEntityCuttingBoard));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        if (!api.World.Config.HasAttribute(worldConfigSandwichLayersLimit))
        {
            api.World.Config.SetInt(worldConfigSandwichLayersLimit, defaultSandwichLayersLimit);
        }
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        foreach (IAsset asset in api.Assets.GetMany("config/danacancook/sandwich_ingredients/"))
        {
            try
            {
                PropertiesPatches.AddRange(asset.ToObject<Dictionary<string, WhenOnSandwichProperties>>());
            }
            catch (Exception e)
            {
                api.Logger.Error($"[Dana Can Cook] Failed loading sandwich ingredients from file {asset.Location}:");
                api.Logger.Error(e);
            }
        }
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        foreach (CollectibleObject obj in api.World.Collectibles)
        {
            if (obj == null || obj.Code == null)
            {
                continue;
            }

            foreach ((string code, WhenOnSandwichProperties props) in PropertiesPatches)
            {
                if (!WhenOnSandwichProperties.HasAtribute(obj) && obj.HasNutrition() && obj.WildCardMatch(code))
                {
                    obj.EnsureAttributesNotNull();
                    WhenOnSandwichProperties.SetAtribute(obj, props);
                    break;
                }
            }

            if (WhenOnSandwichProperties.HasAtribute(obj) && !obj.CreativeInventoryTabs.Contains(ModId))
            {
                obj.CreativeInventoryTabs = obj.CreativeInventoryTabs.Append(ModId);
            }
        }
    }

    public override void Dispose()
    {
        PropertiesPatches?.Clear();
    }
}
