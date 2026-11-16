namespace ImprovePlayable
{
    public class SpawnAndDropModule : IContentModule
    {
        public string Name { get { return "SpawnAndDropModule"; } }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableMonsters;
        }

        public void Initialize()
        {
            RegisterMonsterDrops();
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
