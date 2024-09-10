using Vintagestory.API.Common;

namespace DanaCanCook;

public class CuttingBoardProperties
{
    public JsonItemStack ConvertTo { get; set; }

    public EnumTool[] Tool { get; set; }

    public static CuttingBoardProperties GetProps(CollectibleObject obj)
    {
        return obj != null
            && obj.Attributes != null
            && obj.Attributes.KeyExists(attributeCuttingBoardProperties)
            ? obj.Attributes[attributeCuttingBoardProperties].AsObject<CuttingBoardProperties>()
            : null;
    }

    public CuttingBoardProperties Clone()
    {
        return new()
        {
            ConvertTo = ConvertTo.Clone(),
            Tool = Tool
        };
    }
}