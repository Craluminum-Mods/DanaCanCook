global using static DanaCanCook.Constants;
using Vintagestory.API.Common;

namespace DanaCanCook;

public static class Constants
{
    public const string ModId = "danacancook";

    public const string attributeCodeCuttingBoard = $"{ModId}:canPutOnCuttingBoard";
    public const string attributeCuttingBoardProperties = $"{ModId}:cuttingBoardProperties";
    public const string attributeOffsets = "offsets";
    public const string attributeOnCuttingBoardTransform = $"{ModId}:onCuttingBoardTransform";
    public const string attributeWhenOnSandwich = $"{ModId}:whenOnSandwich";
    public const string attributeSandwichLayers = "sandwichLayers";

    public const string langEmpty = "Empty";
    public const string langContents0x1 = "Contents: {0}x {1}";
    public const string langContents = "Contents: ";
    public const string langNutritionfacts = "nutrition-facts-line-satiety";
    public const string langSandwichContents = $"{ModId}:contents-sandwich";
    public const string langSandwich = $"{ModId}:item-sandwich";
    public const string langWhenEaten = $"{ModId}:when-eaten";

    public const string cuttingBoardInvClassName = $"{ModId}:cuttingBoard";

    public static readonly AssetLocation soundBuild = AssetLocation.Create("sounds/player/build");
}