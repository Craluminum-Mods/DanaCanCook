using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace DanaCanCook;

public class SandwichNutritionProperties
{
    public List<FoodNutritionProperties> NutritionPropertiesMany = new();

    public EnumFoodCategory[] FoodCategories => NutritionPropertiesMany.Select(x => x.FoodCategory).ToArray();

    public float TotalSatiety => NutritionPropertiesMany.Sum(x => x.Satiety);

    public float TotalSaturationLossDelay => NutritionPropertiesMany.Sum(x => x.SaturationLossDelay);

    public float TotalHealth => NutritionPropertiesMany.Sum(x => x.Health);

    public JsonItemStack[] EatenStacks => NutritionPropertiesMany.Select(x => x.EatenStack).ToArray();

    public float TotalIntoxication => NutritionPropertiesMany.Sum(x => x.Intoxication);

    public SandwichNutritionProperties Clone()
    {
        return new SandwichNutritionProperties
        {
            NutritionPropertiesMany = NutritionPropertiesMany.Select(x => x.Clone()).ToList()
        };
    }
}