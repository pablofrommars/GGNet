namespace GGNet.Components
{
    public interface IPanel
    {
        void Render();

        ITooltip Tooltip { get; }
    }
}
