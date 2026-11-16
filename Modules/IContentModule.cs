namespace ImprovePlayable
{
    public interface IContentModule
    {
        string Name { get; }
        bool IsEnabled(ModContentOptions options);
        void Initialize();
    }
}
