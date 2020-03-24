namespace GGNet.Components
{
    public interface ITooltip
    {
        void Show(double x, double y, double offset, string content, string color = null, double? alpha = null);

        void Hide();
    }
}
