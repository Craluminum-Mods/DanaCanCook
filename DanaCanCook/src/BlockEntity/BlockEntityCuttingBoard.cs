﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DanaCanCook;

public class BlockEntityCuttingBoard : BlockEntityDisplay
{
    private readonly InventoryGeneric inventory;
   
    public const int SlotCount = 1;

    public override InventoryBase Inventory => inventory;
    public override string InventoryClassName => cuttingBoardInvClassName;
    public override string AttributeTransformCode => attributeOnCuttingBoardTransform;

    public BlockEntityCuttingBoard()
    {
        inventory = new InventoryGeneric(SlotCount, $"{cuttingBoardInvClassName}-0", Api, (_, inv) => new ItemSlotCuttingBoard(inv));
    }

    public bool OnInteract(IPlayer byPlayer)
    {
        ItemSlot invSlot = inventory.First();
        ItemSlot activeslot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (TryCustomInteraction(byPlayer, invSlot, activeslot))
        {
            activeslot.MarkDirty();
            invSlot.MarkDirty();
            updateMeshes();
            MarkDirty(redrawOnClient: true);
            return true;
        }

        if (ItemSandwich.TryAdd(invSlot, activeslot, byPlayer, byPlayer.Entity.World))
        {
            activeslot.MarkDirty();
            invSlot.MarkDirty();
            updateMeshes();
            MarkDirty(redrawOnClient: true);
            return true;
        }

        bool storable = ItemSlotCuttingBoard.IsStorable(activeslot?.Itemstack?.Collectible);

        if ((activeslot.Empty || !storable) && TryTake(byPlayer, 0))
        {
            activeslot.MarkDirty();
            invSlot.MarkDirty();
            updateMeshes();
            MarkDirty(redrawOnClient: true);
            return true;
        }

        AssetLocation sound = activeslot?.Itemstack?.Block?.Sounds?.Place;
        if (storable && TryPut(activeslot, 0))
        {
            Api.World.PlaySoundAt(sound ?? soundBuild, byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
            activeslot.MarkDirty();
            invSlot.MarkDirty();
            updateMeshes();
            MarkDirty(redrawOnClient: true);
            return true;
        }

        return false;
    }

    private bool TryCustomInteraction(IPlayer byPlayer, ItemSlot invSlot, ItemSlot activeslot)
    {
        if (invSlot.Empty || activeslot.Empty) return false;

        CuttingBoardProperties props = CuttingBoardProperties.GetProps(invSlot?.Itemstack?.Collectible);
        JsonItemStack output = props?.ConvertTo?.Clone();

        foreach (KeyValuePair<string, string> variant in invSlot.Itemstack.Collectible.Variant)
        {
            output?.FillPlaceHolder(variant.Key, variant.Value);
        }

        output?.Resolve(Api.World, "cuttingBoard");
        if (output == null || output.ResolvedItemstack == null) return false;

        if (activeslot?.Itemstack?.Collectible?.Tool != null
            && props.Tool != null
            && props.Tool.Contains((EnumTool)activeslot.Itemstack.Collectible.Tool)
            && activeslot.Itemstack.Collectible.GetRemainingDurability(activeslot.Itemstack) > 0)
        {
            invSlot.TakeOut(1);
            activeslot.Itemstack.Collectible.DamageItem(Api.World, byPlayer.Entity, activeslot);
            ItemStack stack = output.ResolvedItemstack;
            if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true))
            {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            return true;
        }
        return false;
    }

    private bool TryPut(ItemSlot slot, int slotId)
    {
        if (Inventory.Count > slotId && inventory[slotId].Empty)
        {
            int amount = slot.TryPutInto(Api.World, inventory[slotId]);
            return amount > 0;
        }
        return false;
    }

    private bool TryTake(IPlayer byPlayer, int slotId)
    {
        if (Inventory.Count > slotId && !inventory[slotId].Empty)
        {
            ItemStack stack = inventory[slotId].TakeOutWhole();
            if (byPlayer.InventoryManager.TryGiveItemstack(stack))
            {
                AssetLocation sound = stack?.Block?.Sounds?.Place;
                Api.World.PlaySoundAt(sound ?? soundBuild, byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
            }
            if (stack.StackSize > 0)
            {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            return true;
        }
        return false;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
        ItemSlot slot = inventory.First();
        sb.AppendLine(slot.Empty ? Lang.Get(langEmpty) : Lang.Get(langContents0x1, slot.StackSize, slot.GetStackName()));

        if (slot.Empty)
        {
            return;
        }

        SandwichProperties props = SandwichProperties.FromStack(slot.Itemstack, forPlayer.Entity.World);
        if (props == null)
        {
            return;
        }

        props.GetDescription(slot, sb, Api.World);
    }

    protected override float[][] genTransformationMatrices()
    {
        Vec3f[] Offsets = Block?.Attributes?[attributeOffsets]?.AsObject<Vec3f[]>();
        if (Offsets == null || Offsets.Length < SlotCount)
        {
            return Array.Empty<float[]>();
        }
        float[][] tfMatrices = new float[SlotCount][];
        for (int index = 0; index < tfMatrices.Length; index++)
        {
            Vec3f off = Offsets[index];
            off = new Matrixf()
                .RotateDeg(Block.Shape.RotateXYZCopy)
                .TransformVector(off.ToVec4f(0f))
                .XYZ;

            Matrixf mat = new Matrixf()
            .Translate(off.X, off.Y, off.Z)
            .Translate(0.5f, 0.5f, 0.5f)
            .RotateDeg(Block.Shape.RotateXYZCopy)
            .Translate(-0.5f, -0.5f, -0.5f);

            tfMatrices[index] = mat.Values;
        }
        return tfMatrices;
    }
}
