using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DanaCanCook;

public class BlockCuttingBoard : Block
{
    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        return world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityCuttingBoard blockEntity
            ? base.OnBlockInteractStart(world, byPlayer, blockSel)
            : blockEntity.OnInteract(byPlayer) || base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}
