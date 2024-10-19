using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static Vintagestory.GameContent.BlockLiquidContainerBase;

namespace DanaCanCook;

public class ItemSandwich : Item, IContainedMeshSource
{
    public static bool TryAdd(ItemSlot slotSandwich, ItemSlot slotHand, IPlayer byPlayer, IWorldAccessor world)
    {
        bool isSandwich = slotSandwich?.Itemstack?.Collectible is ItemSandwich;
        var sandwichPropsInHand = SandwichProperties.FromStack(slotHand?.Itemstack, world);
        bool isSandwichInHand = slotHand?.Itemstack?.Collectible is ItemSandwich && sandwichPropsInHand != null && sandwichPropsInHand.Any;

        if (!isSandwich || isSandwichInHand)
        {
            return false;
        }

        if (slotHand?.Itemstack?.Collectible is ILiquidSource && TryAddLiquid(slotSandwich, slotLiquid: slotHand, byPlayer, world))
        {
            return true;
        }

        if (!WhenOnSandwichProperties.HasAtribute(slotHand?.Itemstack?.Collectible))
        {
            return false;
        }

        SandwichProperties propsInHand = SandwichProperties.FromStack(slotHand.Itemstack, world);
        if (propsInHand != null && propsInHand.Any)
        {
            return false;
        }

        SandwichProperties props = SandwichProperties.FromStack(slotSandwich.Itemstack, world);
        ItemStack stackIngredient = slotHand.Itemstack.Clone();
        stackIngredient.StackSize = 1;
        if (props == null || !props.TryAdd(stackIngredient, world))
        {
            return false;
        }
        props.ToStack(slotSandwich.Itemstack);
        slotHand.TakeOut(1);
        return true;
    }

    private static bool TryAddLiquid(ItemSlot slotSandwich, ItemSlot slotLiquid, IPlayer byPlayer, IWorldAccessor world)
    {
        BlockLiquidContainerBase liquidContainer = slotLiquid.Itemstack.Collectible as BlockLiquidContainerBase;

        if (slotLiquid.Itemstack.Collectible is not ILiquidSource liquidSource || !liquidSource.AllowHeldLiquidTransfer)
        {
            return false;
        }

        ItemStack contentStackToMove = liquidSource.GetContent(slotLiquid.Itemstack);
        WhenOnSandwichProperties whenOnSandwichProps = WhenOnSandwichProperties.GetProps(contentStackToMove?.Collectible);
        if (whenOnSandwichProps == null)
        {
            return false;
        }

        SandwichProperties props = SandwichProperties.FromStack(slotSandwich.Itemstack, world);
        if (!props.CanAdd(contentStackToMove, world))
        {
            return false;
        }

        WaterTightContainableProps contentProps = liquidSource.GetContentProps(slotLiquid.Itemstack);
        int moved = (int)(whenOnSandwichProps.LitresPerLayer * contentProps.ItemsPerLitre);
        if (liquidSource.GetCurrentLitres(slotLiquid.Itemstack) < whenOnSandwichProps.LitresPerLayer || moved <= 0)
        {
            return false;
        }

        liquidContainer.CallMethod<int>("splitStackAndPerformAction", byPlayer.Entity, slotLiquid, delegate (ItemStack stack)
        {
            liquidContainer.TryTakeContent(stack, moved);
            return moved;
        });

        ItemStack stackIngredient = contentStackToMove.Clone();
        stackIngredient.StackSize = moved;
        if (props == null || !props.TryAdd(stackIngredient, world))
        {
            return false;
        }
        props.ToStack(slotSandwich.Itemstack);

        liquidContainer.DoLiquidMovedEffects(byPlayer, contentStackToMove, moved, EnumLiquidDirection.Pour);
        return true;
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

        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack ingredientStack = stacks[i];
            bool last = i == stacks.Count - 1 && stacks.Count != 1;
            MeshData ingredientMesh = GenIngredientMesh(api as ICoreClientAPI, ref prevSize, ref rotation, ingredientStack, last);
            mesh.AddMeshData(ingredientMesh);
        }
        return mesh;
    }

    private static MeshData GenIngredientMesh(ICoreClientAPI capi, ref float prevSize, ref float rotation, ItemStack stack, bool last = false)
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
        if (last && props.ShapeLast != null)
        {
            rcshape = props.ShapeLast.Clone();
        }

        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        if (shape == null)
        {
            prevSize += props.Size;
            return mesh;
        }

        ShapeTextureSource texSource = new ShapeTextureSource(capi, shape, shape.ToString());

        Dictionary<string, CompositeTexture> textures;
        if (props.Textures == null || !props.Textures.Any())
        {
            textures = stack.GetTextures();
        }
        else
        {
            textures = props.Textures;
        }

        foreach (KeyValuePair<string, CompositeTexture> val in textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            foreach ((string key, string value) in stack.Collectible.Variant)
            {
                ctex.FillPlaceholder($"{{{key}}}", value);
            }

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

        props.GetDescription(inSlot, dsc, world, noLimit: true);

        EntityPlayer entityPlayer = (world.Side == EnumAppSide.Client) ? (world as IClientWorldAccessor).Player.Entity : null;
        SandwichNutritionProperties nutritionProps = props.GetNutritionProperties(inSlot, world, entityPlayer);

        float spoilState = AppendPerishableInfoText(inSlot, dsc, world);
        if (nutritionProps != null)
        {
            float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, inSlot.Itemstack, entityPlayer);
            float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, inSlot.Itemstack, entityPlayer);

            dsc.AppendLine(Lang.Get(langWhenEaten));

            Dictionary<string, (float TotalSat, float TotalHealth)> categorySummary = new Dictionary<string, (float, float)>();

            foreach (FoodNutritionProperties property in nutritionProps.NutritionPropertiesMany)
            {
                float totalSat = property.Satiety * satLossMul;
                float totalHealth = property.Health * healthLossMul;

                totalSat = (float)Math.Round(totalSat);

                string category = property.FoodCategory.ToString();

                if (categorySummary.ContainsKey(category))
                {
                    categorySummary[category] = (
                        categorySummary[category].TotalSat + totalSat,
                        categorySummary[category].TotalHealth + totalHealth
                    );
                }
                else
                {
                    categorySummary[category] = (totalSat, totalHealth);
                }
            }

            foreach (KeyValuePair<string, (float TotalSat, float TotalHealth)> entry in categorySummary)
            {
                string category = entry.Key;
                float totalSat = entry.Value.TotalSat;
                float totalHealth = entry.Value.TotalHealth;

                string translatedCategory = Lang.Get("foodcategory-" + category.ToLowerInvariant());

                if (Math.Abs(totalHealth) > 0.001f)
                {
                    dsc.AppendLine("- " + Lang.Get(langCategorySaturationHealth,
                        translatedCategory,
                        totalSat,
                        totalHealth));
                }
                else
                {
                    dsc.AppendLine("- " + Lang.Get(langCategorySaturation,
                        translatedCategory,
                        totalSat));
                }
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

    protected override void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1)
    {
        SandwichProperties props = SandwichProperties.FromStack(slot?.Itemstack, byEntity.World);
        if (props == null || !props.Any || props.GetNutritionProperties(slot, byEntity.World, byEntity) == null)
        {
            base.tryEatBegin(slot, byEntity, ref handling, eatSound, eatSoundRepeats);
            return;
        }

        byEntity.World.RegisterCallback(delegate
        {
            playEatSound(byEntity, eatSound, eatSoundRepeats);
        }, 500);
        byEntity.AnimManager?.StartAnimation("eat");
        handling = EnumHandHandling.PreventDefault;
    }

    protected override bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null)
    {
        SandwichProperties props = SandwichProperties.FromStack(slot?.Itemstack, byEntity.World);
        if (props == null || !props.Any || props.GetNutritionProperties(slot, byEntity.World, byEntity) == null)
        {
            return base.tryEatStep(secondsUsed, slot, byEntity, spawnParticleStack);
        }

        Vec3d xYZ = byEntity.Pos.AheadCopy(0.40000000596046448).XYZ;
        xYZ.X += byEntity.LocalEyePos.X;
        xYZ.Y += byEntity.LocalEyePos.Y - 0.40000000596046448;
        xYZ.Z += byEntity.LocalEyePos.Z;
        if (secondsUsed > 0.5f && (int)(30f * secondsUsed) % 7 == 1)
        {
            byEntity.World.SpawnCubeParticles(xYZ, spawnParticleStack ?? slot.Itemstack, 0.3f, 4, 0.5f, (byEntity as EntityPlayer)?.Player);
        }

        if (byEntity.World is IClientWorldAccessor)
        {
            ModelTransform modelTransform = new ModelTransform();
            modelTransform.EnsureDefaultValues();
            modelTransform.Origin.Set(0f, 0f, 0f);
            if (secondsUsed > 0.5f)
            {
                modelTransform.Translation.Y = Math.Min(0.02f, GameMath.Sin(20f * secondsUsed) / 10f);
            }

            modelTransform.Translation.X -= Math.Min(1f, secondsUsed * 4f * 1.57f);
            modelTransform.Translation.Y -= Math.Min(0.05f, secondsUsed * 2f);
            modelTransform.Rotation.X += Math.Min(30f, secondsUsed * 350f);
            modelTransform.Rotation.Y += Math.Min(80f, secondsUsed * 350f);
            byEntity.Controls.UsingHeldItemTransformAfter = modelTransform;
            return secondsUsed <= 1f;
        }

        return true;
    }

    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        SandwichProperties props = SandwichProperties.FromStack(slot?.Itemstack, byEntity.World);
        SandwichNutritionProperties nutritionProperties = props.GetNutritionProperties(slot, byEntity.World, byEntity);

        if (props == null || !props.Any || nutritionProperties == null)
        {
            base.tryEatStop(secondsUsed, slot, byEntity);
            return;
        }

        if (byEntity.World is not IServerWorldAccessor || !(secondsUsed >= 0.95f))
        {
            return;
        }

        float spoilState = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
        float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
        float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);

        bool any = false;
        foreach (FoodNutritionProperties property in nutritionProperties.NutritionPropertiesMany)
        {
            any = true;
            byEntity.ReceiveSaturation(property.Satiety * satLossMul, property.FoodCategory);

            float health = property.Health * healthLossMul;
            float intoxication = byEntity.WatchedAttributes.GetFloat("intoxication");
            byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, intoxication + property.Intoxication));
            if (health != 0f)
            {
                byEntity.ReceiveDamage(new DamageSource
                {
                    Source = EnumDamageSource.Internal,
                    Type = (health > 0f) ? EnumDamageType.Heal : EnumDamageType.Poison
                }, Math.Abs(health));
            }
        }

        if (any)
        {
            IPlayer player = null;
            if (byEntity is EntityPlayer)
            {
                player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            }
            slot.TakeOut(1);
            slot.MarkDirty();
            player.InventoryManager.BroadcastHotbarSlot();
        }
    }

    public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties transitionProps)
    {
        SandwichProperties props = SandwichProperties.FromStack(slot.Itemstack, api.World);
        if (props == null || !props.Any)
        {
            return base.OnTransitionNow(slot, transitionProps);
        }

        ItemStack stack = transitionProps.TransitionedStack.ResolvedItemstack.Clone();
        stack.StackSize = GameMath.RoundRandom(api.World.Rand, slot.StackSize * props.GetOrdered(api.World).Sum(x => x.StackSize) * transitionProps.TransitionRatio);
        return stack;
    }
}