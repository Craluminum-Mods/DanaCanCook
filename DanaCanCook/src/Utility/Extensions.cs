using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace DanaCanCook;

public static class Extensions
{
    public static void EnsureAttributesNotNull(this CollectibleObject obj)
    {
        obj.Attributes ??= new JsonObject(new JObject());
    }

    public static bool HasNutrition(this CollectibleObject obj)
    {
        if (obj.NutritionProps != null)
        {
            return true;
        }

        if (obj.Attributes == null)
        {
            return true;
        }

        if (obj.Attributes.KeyExists("nutritionPropsWhenInMeal"))
        {
            return true;
        }

        if (obj.Attributes.KeyExists("waterTightContainerProps") && obj.Attributes["waterTightContainerProps"].KeyExists("nutritionPropsPerLitre"))
        {
            return true;
        }

        return false;
    }

    public static Dictionary<string, CompositeTexture> GetTextures(this ItemStack stack)
    {
        return stack.Class switch
        {
            EnumItemClass.Item => stack.Item.Textures,
            EnumItemClass.Block => stack.Block.Textures.ToDictionary(x => x.Key, y => y.Value.Clone()),
            _ => new()
        };
    }
}
