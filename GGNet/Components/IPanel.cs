namespace GGNet.Components
{
    public interface IPanel
    {
        void Refresh() { }

        ITooltip Tooltip { get; }
    }
}
