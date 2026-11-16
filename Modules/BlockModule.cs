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
            ItemsConfigContainer container = JsonConfigHelper.LoadOrCreateConfig<ItemsConfigContainer>("Items.json", null);
            if (container == null || container.blocks == null || container.blocks.Count == 0) return;

            foreach (var cfg in container.blocks)
            {
                if (cfg == null || string.IsNullOrEmpty(cfg.codeName)) continue;

                CustomItemWall wall = BlockFactory.CreateAndRegisterBlock(
                    codeName: cfg.codeName,
                    displayName: cfg.displayName,
                    description: cfg.description,
                    health: cfg.health,
                    hardness: cfg.hardness,
                    weight: cfg.weight,
                    blockType: CItem_Wall.Type.WallBlock,
                    textureResourceName: cfg.textureResource
                );

                if (ObsidianWall == null && cfg.codeName == "ImprovePlayable_ObsidianWall")
                {
                    ObsidianWall = wall;
                }
            }
        }
    }
}
