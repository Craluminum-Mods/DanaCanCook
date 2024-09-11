using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace DanaCanCook;

public static class Extensions
{
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
