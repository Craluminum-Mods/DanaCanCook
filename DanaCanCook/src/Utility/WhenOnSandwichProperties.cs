using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DanaCanCook;

public class WhenOnSandwichProperties
{
    public CompositeShape Shape { get; set; }

    public CompositeShape ShapeLast { get; set; }

    public Dictionary<string, CompositeTexture> Textures { get; set; } = new();

    public float Size { get; set; } = 0.0625f;

    public bool Rotate { get; set; }

    public bool CopyLastRotation { get; set; }

    public NatFloat Rotation { get; set; } = NatFloat.One;

    public float LitresPerLayer { get; set; } = 0.1f;

    public static WhenOnSandwichProperties GetProps(CollectibleObject obj)
    {
        return HasAtribute(obj) ? obj.Attributes[attributeWhenOnSandwich].AsObject<WhenOnSandwichProperties>() : null;
    }
    
    public static bool HasAtribute(CollectibleObject obj)
    {
        return obj != null && obj.Attributes != null && obj.Attributes.KeyExists(attributeWhenOnSandwich);
    }
        
    public static void SetAtribute(CollectibleObject obj, WhenOnSandwichProperties props)
    {
        obj.Attributes.Token[attributeWhenOnSandwich] = JToken.FromObject(props);
    }

    public WhenOnSandwichProperties Clone()
    {
        return new()
        {
            Shape = Shape.Clone(),
            ShapeLast = ShapeLast.Clone(),
            Textures = Textures.ToDictionary(x => x.Key, y => y.Value.Clone()),
            Size = Size,
            Rotate = Rotate,
            CopyLastRotation = CopyLastRotation,
            Rotation = Rotation.Clone(),
            LitresPerLayer = LitresPerLayer
        };
    }
}
