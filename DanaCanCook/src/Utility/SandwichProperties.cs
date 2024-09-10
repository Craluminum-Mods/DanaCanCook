using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace DanaCanCook;

public class SandwichProperties
{
    protected OrderedDictionary<int, ItemStack> Layers { get; set; } = new();

    public int Count => Layers.Count;
    public bool Any => Layers.Any();

    public IEnumerable<ItemStack> GetOrdered(IWorldAccessor world)
    {
        return Layers.OrderBy(x => x.Key).Select(x => x.Value);
    }

    public bool TryAdd(ItemStack stack)
    {
        if (stack == null || stack.StackSize == 0)
        {
            return false;
        }

        Layers[Layers.Count] = stack;
        return true;
    }

    public static SandwichProperties FromStack(ItemStack stack, IWorldAccessor world)
    {
        ITreeAttribute treeSandwichLayers = stack.Attributes.GetOrAddTreeAttribute(attributeSandwichLayers);
        SandwichProperties properties = new SandwichProperties();
        foreach ((string key, IAttribute attribute) in treeSandwichLayers)
        {
            int index = key.ToInt();
            if (!properties.Layers.ContainsKey(index))
            {
                ItemStack _stack = treeSandwichLayers.GetItemstack(key);
                if (_stack?.ResolveBlockOrItem(world) == true)
                {
                    properties.Layers[index] = _stack;
                }
            }
        }
        if (!treeSandwichLayers.Any())
        {
            stack.Attributes.RemoveAttribute(attributeSandwichLayers);
        }
        return properties;
    }

    public void ToStack(ItemStack stack)
    {
        if (Layers.Any())
        {
            ITreeAttribute treeSandwichLayers = stack.Attributes.GetOrAddTreeAttribute(attributeSandwichLayers);

            foreach ((int key, ItemStack val) in Layers)
            {
                treeSandwichLayers.SetItemstack(key.ToString(), val);
            }
        }
    }

    public StringBuilder GetDescription(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
    {
        ItemStack stackWithoutSandwichAttributes = inSlot.Itemstack.Clone();
        stackWithoutSandwichAttributes.Attributes.RemoveAttribute(attributeSandwichLayers);
        IEnumerable<ItemStack> stacks = new List<ItemStack>() { stackWithoutSandwichAttributes }.Concat(GetOrdered(world));

        dsc.AppendLine(Lang.Get(langSandwichContents));
        foreach (ItemStack stack in stacks.TakeLast(6))
        {
            if (stack == null || stack.StackSize <= 0)
            {
                continue;
            }

            dsc.AppendLine("- " + stack.GetName());
        }

        return dsc;
    }
    
    public SandwichNutritionProperties GetNutritionProperties(ItemSlot inSlot, IWorldAccessor world, Entity forEntity)
    {
        FoodNutritionProperties nutritionProps = inSlot.Itemstack.Collectible.GetNutritionProperties(world, inSlot.Itemstack, forEntity);
        IEnumerable<ItemStack> stacks = new List<ItemStack>() { inSlot.Itemstack }.Concat(GetOrdered(world));

        SandwichNutritionProperties sandwichNutritionProps = new SandwichNutritionProperties();
        foreach (ItemStack stack in stacks)
        {
            if (stack != null && stack.StackSize > 0)
            {
                sandwichNutritionProps.NutritionPropertiesMany.Add(stack.Collectible.GetNutritionProperties(world, stack, forEntity));
            }
        }

        return sandwichNutritionProps;
    }

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        if (Layers.Any())
        {
            result.Append('(');
            result.Append(string.Join('|', Layers.Select(x => $"{x.Key}-{StackToString(x.Value)}")));
            result.Append(')');
        }
        return result.ToString();
    }

    private static string StackToString(ItemStack stack)
    {
        return new ItemstackAttribute(stack).ToJsonToken();
    }
}