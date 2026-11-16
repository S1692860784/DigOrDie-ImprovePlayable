namespace ImprovePlayable
{
    public class MachineModule : IContentModule
    {
        public string Name { get { return "MachineModule"; } }

        public static CItem ModAssembler { get; private set; }
        public static CItem UraniumGenerator { get; private set; }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableBlocks;
        }

        public void Initialize()
        {
            CItem autoBuilderMk1 = ItemFactory.FindItemByCodeName("autoBuilderMK1");
            if (autoBuilderMk1 != null)
            {
                ModAssembler = ItemFactory.CreateAndRegisterItemFromTemplate(
                    autoBuilderMk1,
                    "ImprovePlayable_AutoBuilder",
                    "modV1",
                    "用于制造模组物品的专用组装机。放置后使用交互键打开。"
                );

                if (ModAssembler != null)
                {
                    RecipeManager.SetModMachineCodeName(ModAssembler.m_codeName);
                }
            }

            CItem reactor = ItemFactory.FindItemByCodeName("reactor");
            if (reactor == null)
            {
                reactor = ItemFactory.FindItemByCodeName("generatorSun");
            }

            if (reactor != null)
            {
                UraniumGenerator = ItemFactory.CreateAndRegisterItemFromTemplate(
                    reactor,
                    "ImprovePlayable_UraniumGenerator",
                    "铀发电机",
                    "铀能自律发电机，制造后即可持续发电（5kW）。"
                );
            }
        }
    }
}
