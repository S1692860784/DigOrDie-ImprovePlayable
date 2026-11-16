namespace ImprovePlayable
{
    public class RecipeModule : IContentModule
    {
        public string Name { get { return "RecipeModule"; } }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableRecipes;
        }

        public void Initialize()
        {
            var thanosGun = WeaponModule.ThanosGun;
            var energyCrystal = MiningModule.EnergyCrystal;
            var obsidianWall = BlockModule.ObsidianWall;
            var oreDetectors = MiningModule.OreDetectors;
            var superFlashLight = DeviceModule.SuperFlashLight;
            var modAssembler = MachineModule.ModAssembler;
            var uraniumGenerator = MachineModule.UraniumGenerator;
            var energyCrystalArmor = DeviceModule.EnergyCrystalArmor;
            var teleporter = DeviceModule.Teleporter;
            var energyTurret = DefenseModule.EnergyTurret;

            if (thanosGun != null && energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(thanosGun, GItems.aluminium, 5, energyCrystal, 10);
            }

            if (obsidianWall != null)
            {
                RecipeManager.RegisterRecipe(obsidianWall, GItems.rock, 10);
            }

            if (energyCrystal != null && oreDetectors != null)
            {
                foreach (var detector in oreDetectors)
                {
                    if (detector != null)
                    {
                        RecipeManager.RegisterRecipe(detector, energyCrystal, 10);
                    }
                }
            }

            if (superFlashLight != null && energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(superFlashLight, energyCrystal, 5, GItems.lightGem, 5);
            }

            if (modAssembler != null)
            {
                RecipeManager.RegisterRecipe(modAssembler, GItems.iron, 5, machineLevel: 1);
            }

            if (uraniumGenerator != null && energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(uraniumGenerator, GItems.uranium, 1, energyCrystal, 5);
            }

            if (energyCrystalArmor != null && energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(energyCrystalArmor, energyCrystal, 10);
            }

            if (teleporter != null && energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(teleporter, energyCrystal, 10);
            }

            if (energyTurret != null && energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(energyTurret, energyCrystal, 10);
            }
        }
    }
}
