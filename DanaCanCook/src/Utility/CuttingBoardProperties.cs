using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace DanaCanCook;

public class CuttingBoardProperties
{
    public JsonItemStack ConvertTo { get; set; }

    public EnumTool[] Tool { get; set; }

    public static CuttingBoardProperties GetProps(CollectibleObject obj)
    {
        return HasAtribute(obj)
            ? obj.Attributes[attributeCuttingBoardProperties].AsObject<CuttingBoardProperties>()
            : null;
    }

    public static bool HasAtribute(CollectibleObject obj)
    {
        return obj != null && obj.Attributes != null && obj.Attributes.KeyExists(attributeCuttingBoardProperties) && obj.Attributes[attributeCuttingBoardProperties].AsBool();
    }

    public static void SetAtribute(CollectibleObject obj, CuttingBoardProperties props)
    {
        obj.Attributes.Token[attributeCuttingBoardProperties] = JToken.FromObject(props);
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