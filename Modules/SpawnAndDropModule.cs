namespace ImprovePlayable
{
    public class SpawnAndDropModule : IContentModule
    {
        public string Name { get { return "SpawnAndDropModule"; } }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableOres || options.EnableMonsters;
        }

        public void Initialize()
        {
            RegisterOreDropsAndSpawns();
            RegisterMonsterDrops();
        }

        private void RegisterOreDropsAndSpawns()
        {
            if (MiningModule.EnergyCrystalOre != null)
            {
                GameDataManager.RegisterCustomDrop("ImprovePlayable_EnergyCrystalOre", "ImprovePlayable_EnergyCrystal", 1, 3);
            }

            GameDataManager.RegisterOreSpawn(new OreSpawnRule
            {
                OreCodeName = "ImprovePlayable_EnergyCrystalOre",
                ReplacesBlockCodeName = "rock",
                MinY = 400,
                MaxY = 1000,
                VeinSize = 12,
                Frequency = 0.19f
            });
        }

        private void RegisterMonsterDrops()
        {
            GameDataManager.RegisterMonsterDrop(
                ModManager.MONSTER_THANOS_MINION,
                "ImprovePlayable_EnergyCrystal",
                1.0f,
                2,
                5,
                Patch_MonsterLoot.DropCondition.DayOnly
            );

            GameDataManager.RegisterMonsterDrop(
                ModManager.MONSTER_THANOS_MINION,
                "ImprovePlayable_ObsidianWall",
                1.0f,
                1,
                2,
                Patch_MonsterLoot.DropCondition.NightOnly
            );

            GameDataManager.RegisterMonsterDrop(ModManager.MONSTER_ENERGY_CANNON, "ImprovePlayable_EnergyCrystal", 1.0f, 5, 10);
            GameDataManager.RegisterMonsterDrop(ModManager.MONSTER_GHOST_WATCHER, "ImprovePlayable_EnergyCrystal", 0.5f, 1, 3);
            GameDataManager.RegisterMonsterDrop(ModManager.MONSTER_FLAME_LIZARD, "ImprovePlayable_ObsidianWall", 1.0f, 1, 3);

            GameDataManager.RegisterMonsterDrop("hound", "ImprovePlayable_EnergyCrystal", 0.3f, 1, 2);
            GameDataManager.RegisterMonsterDrop("firefly", "ImprovePlayable_EnergyCrystal", 0.5f, 1, 1);
            GameDataManager.RegisterMonsterDrop("bossMadCrab", "ImprovePlayable_EnergyCrystal", 1.0f, 10, 20);
        }
    }
}
