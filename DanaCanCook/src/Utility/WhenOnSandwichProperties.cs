using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DanaCanCook;

public class WhenOnSandwichProperties
{
    public CompositeShape Shape { get; set; }

    public float Size { get; set; } = 0.0625f;

    public bool Rotate { get; set; }

    public bool CopyLastRotation { get; set; }

    public NatFloat Rotation { get; set; } = NatFloat.One;

    //public Dictionary<string, CompositeTexture> Textures { get; set; } = new();

    public static WhenOnSandwichProperties GetProps(CollectibleObject obj)
    {
        return obj != null
            && obj.Attributes != null
            && obj.Attributes.KeyExists(attributeWhenOnSandwich)
            ? obj.Attributes[attributeWhenOnSandwich].AsObject<WhenOnSandwichProperties>()
            : null;
    }

    public WhenOnSandwichProperties Clone()
    {
        return new()
        {
            Shape = Shape.Clone(),
            //Textures = Textures,
            Size = Size,
            Rotate = Rotate,
            CopyLastRotation = CopyLastRotation,
            Rotation = Rotation
        };
    }
}
