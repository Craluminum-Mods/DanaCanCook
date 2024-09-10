using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace DanaCanCook;

public class ItemSandwich : Item, IContainedMeshSource
{
    public static bool TryAdd(ItemSlot slotSandwich, ItemSlot slotHand, IWorldAccessor world)
    {
        bool isSandwich = slotSandwich?.Itemstack?.Collectible is ItemSandwich;
        bool isSandwichIngredient = slotHand?.Itemstack?.Collectible.Attributes.KeyExists(attributeWhenOnSandwich) == true;

        if (!isSandwich || !isSandwichIngredient)
        {
            return false;
        }

        SandwichProperties propsInHand = SandwichProperties.FromStack(slotHand.Itemstack, world);
        bool isSandwichInHand = slotHand?.Itemstack?.Collectible is ItemSandwich;

        if (!isSandwichInHand
            || propsInHand == null
            || !propsInHand.Any)
        {
            SandwichProperties props = SandwichProperties.FromStack(slotSandwich.Itemstack, world);
            ItemStack stackIngredient = slotHand.Itemstack.Clone();
            stackIngredient.StackSize = 1;
            if (props == null || !props.TryAdd(stackIngredient))
            {
                return false;
            }
            props.ToStack(slotSandwich.Itemstack);
            slotHand.TakeOut(1);
            return true;
        }

        return false;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack stack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, stack, target, ref renderinfo);

        string key = GetMeshCacheKey(stack);
        Dictionary<string, MultiTextureMeshRef> InvMeshes = ObjectCacheUtil.GetOrCreate(capi, "danacancook:sandwich-invmeshes", () => new Dictionary<string, MultiTextureMeshRef>());
        if (!InvMeshes.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenMesh(stack, capi.ItemTextureAtlas, null);
            meshref = InvMeshes[key] = capi.Render.UploadMultiTextureMesh(mesh);
        }
        renderinfo.ModelRef = meshref;
    }

    public MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        MeshData mesh = new MeshData(4, 3);

        float prevSize = 0;
        float rotation = 0;

        List<ItemStack> stacks = new() { stack };

        SandwichProperties properties = SandwichProperties.FromStack(stack, api.World);
        IEnumerable<ItemStack> ordered = properties?.GetOrdered(api.World);
        if (ordered != null)
        {
            stacks.AddRange(ordered);
        }

        foreach (ItemStack ingredientStack in stacks)
        {
            MeshData ingredientMesh = GenIngredientMesh(api as ICoreClientAPI, ref prevSize, ref rotation, ingredientStack);
            mesh.AddMeshData(ingredientMesh);
        }
        return mesh;
    }

    private static MeshData GenIngredientMesh(ICoreClientAPI capi, ref float prevSize, ref float rotation, ItemStack stack)
    {
        MeshData mesh = new MeshData(4, 3);

        WhenOnSandwichProperties props = WhenOnSandwichProperties.GetProps(stack?.Collectible);
        if (props == null)
        {
            switch (stack?.Class)
            {
                case EnumItemClass.Block:
                    capi.Tesselator.TesselateBlock(stack.Block, out mesh);
                    prevSize += 0.0625f;
                    return mesh;
                case EnumItemClass.Item:
                    capi.Tesselator.TesselateItem(stack.Item, out mesh);
                    prevSize += 0.0625f;
                    return mesh;
                default:
                    prevSize += 0.0625f;
                    return mesh;
            }
        }

        CompositeShape rcshape = props.Shape.Clone();
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        if (shape == null)
        {
            prevSize += props.Size;
            return mesh;
        }

        ShapeTextureSource texSource = new ShapeTextureSource(capi, shape, shape.ToString());
        foreach (KeyValuePair<string, CompositeTexture> val in stack.Item.Textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex.Bake(capi.Assets);
            texSource.textures[val.Key] = ctex;
        }

        capi.Tesselator.TesselateShape("Sandwich item", shape, out mesh, texSource);

        if (props.Rotate)
        {
            rotation = props.CopyLastRotation ? rotation : props.Rotation.nextFloat();
            mesh = mesh.Translate(-0.5f, -0.5f, -0.5f);
            mesh = mesh.Rotate(Vec3f.Zero, 0, rotation, 0);
            mesh = mesh.Translate(0.5f, 0.5f, 0.5f);
        }
        mesh = mesh.Translate(0, prevSize, 0);
        prevSize += props.Size;
        return mesh;
    }

    public string GetMeshCacheKey(ItemStack stack)
    {
        SandwichProperties props = SandwichProperties.FromStack(stack, (api as ICoreClientAPI).World);
        if (props == null || !props.Any)
        {
            return Code.ToString();
        }
        return Code.ToString() + "-" + props.ToString();
    }

    public override string GetHeldItemName(ItemStack stack)
    {
        SandwichProperties props = SandwichProperties.FromStack(stack, api.World);
        if (props == null || !props.Any)
        {
            return base.GetHeldItemName(stack);
        }
        return Lang.Get(langSandwich);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        SandwichProperties props = SandwichProperties.FromStack(inSlot.Itemstack, world);
        if (props == null || !props.Any)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            return;
        }

        if (withDebugInfo)
        {
            dsc.AppendLine("<font color=\"#bbbbbb\">Id:" + Id + "</font>");
            dsc.AppendLine("<font color=\"#bbbbbb\">Code: " + Code?.ToString() + "</font>");
            ICoreAPI coreAPI = api;
            if (coreAPI != null && coreAPI.Side == EnumAppSide.Client && (api as ICoreClientAPI).Input.KeyboardKeyStateRaw[1])
            {
                dsc.AppendLine("<font color=\"#bbbbbb\">Attributes: " + inSlot.Itemstack.Attributes.ToJsonToken() + "</font>\n");
            }
        }

        props.GetDescription(inSlot, dsc, world);

        EntityPlayer entityPlayer = (world.Side == EnumAppSide.Client) ? (world as IClientWorldAccessor).Player.Entity : null;
        SandwichNutritionProperties nutritionProps = props.GetNutritionProperties(inSlot, world, entityPlayer);

        float spoilState = AppendPerishableInfoText(inSlot, dsc, world);
        if (nutritionProps != null)
        {
            float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, inSlot.Itemstack, entityPlayer);
            float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, inSlot.Itemstack, entityPlayer);
            if (Math.Abs(nutritionProps.TotalHealth * healthLossMul) > 0.001f)
            {
                dsc.AppendLine(Lang.Get("When eaten: {0} sat, {1} hp", Math.Round(nutritionProps.TotalSatiety * satLossMul), nutritionProps.TotalHealth * healthLossMul));
            }
            else
            {
                dsc.AppendLine(Lang.Get("When eaten: {0} sat", Math.Round(nutritionProps.TotalSatiety * satLossMul)));
            }

            dsc.AppendLine("Food Categories: ");
            foreach (EnumFoodCategory category in nutritionProps.FoodCategories.Distinct())
            {
                dsc.AppendLine("- " + Lang.Get("foodcategory-" + category.ToString().ToLowerInvariant())); 
            }
        }

        CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
        for (int i = 0; i < collectibleBehaviors.Length; i++)
        {
            collectibleBehaviors[i].GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        if (dsc.Length > 0)
        {
            dsc.Append("\n");
        }

        float temperature = GetTemperature(world, inSlot.Itemstack);
        if (temperature > 20f)
        {
            dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temperature));
        }

        if (Code != null && Code.Domain != "game")
        {
            Mod mod = api.ModLoader.GetMod(Code.Domain);
            dsc.AppendLine(Lang.Get("Mod: {0}", mod?.Info.Name ?? Code.Domain));
        }
    }
}