namespace GGNet.Components
{
    public interface ITooltip
    {
        void Show(double x, double y, string content, string color = null, double? alpha = null);

        void Hide();
    }
}
