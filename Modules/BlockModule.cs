namespace ImprovePlayable
{
    public class BlockModule : IContentModule
    {
        public string Name { get { return "BlockModule"; } }

        public static CustomItemWall ObsidianWall { get; private set; }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableBlocks;
        }

        public void Initialize()
        {
            string desc = "坚硬的火山岩石墙体。耐久: 500 硬度: 6000 重量: 500";
            ObsidianWall = BlockFactory.CreateAndRegisterBlock(
                codeName: "ImprovePlayable_ObsidianWall",
                displayName: "黑曜石墙",
                description: desc,
                health: 500,
                hardness: 6000,
                weight: 500f,
                blockType: CItem_Wall.Type.WallBlock,
                textureResourceName: ".Resources.ObsidianWall.png"
            );
        }
    }
}
