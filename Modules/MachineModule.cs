namespace ImprovePlayable
{
    public class MachineModule : IContentModule
    {
        public string Name { get { return "MachineModule"; } }

        public static CItem ModAssembler { get; private set; }
        public static CItem UraniumGenerator { get; private set; }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableMachines;
        }

        public void Initialize()
        {
            ItemsConfigContainer container = JsonConfigHelper.LoadOrCreateConfig<ItemsConfigContainer>("Items.json", null);
            if (container == null || container.machines == null || container.machines.Count == 0) return;

            foreach (var cfg in container.machines)
            {
                if (cfg == null || string.IsNullOrEmpty(cfg.codeName)) continue;

                CItem template = ItemFactory.FindItemByCodeName(cfg.templateCodeName);
                if (template == null) continue;

                CItem item = ItemFactory.CreateAndRegisterItemFromTemplate(
                    template,
                    cfg.codeName,
                    cfg.displayName,
                    cfg.description
                );

                if (item != null)
                {
                    if (cfg.electricValue != 0 && item is CItem_Machine)
                    {
                        HarmonyLib.Traverse.Create(item).Field("m_electricValue").SetValue(cfg.electricValue);
                    }

                    if (ModAssembler == null && cfg.codeName == "ImprovePlayable_AutoBuilder")
                    {
                        ModAssembler = item;
                        RecipeManager.SetModMachineCodeName(ModAssembler.m_codeName);
                    }
                    else if (UraniumGenerator == null && cfg.codeName == "ImprovePlayable_UraniumGenerator")
                    {
                        UraniumGenerator = item;
                    }
                }
            }
        }
    }
}
