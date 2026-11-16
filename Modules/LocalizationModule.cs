namespace ImprovePlayable
{
    public class LocalizationModule : IContentModule
    {
        public string Name { get { return "LocalizationModule"; } }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableLocalization;
        }

        public void Initialize()
        {
            Patch_Localization.Register("ENV_" + StimEnvironmentManager.StimEventId, "兴奋剂");
        }
    }
}
