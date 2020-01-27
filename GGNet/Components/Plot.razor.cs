using Microsoft.AspNetCore.Components;

using static GGNet.Position;

namespace GGNet.Components
{
    public partial class Plot<T, TX, TY> : ComponentBase
        where TX : struct
        where TY : struct
    {
        public Plot()
            : base()
        {
            Id = SVGUtils.Id(this);
        }

        [Parameter]
        public Data<T, TX, TY> Data { get; set; }

        [Parameter]
        public double Width { get; set; } = 720;

        [Parameter]
        public double Height { get; set; } = 576;

        public string Id { get; }

        public Zone Title;
        public Zone SubTitle;

        public Zone Legend;

        public Zone Caption;

        private Zone wrapper;

        protected override void OnInitialized()
        {
            Data.Init();

            Data.Render(true);

            Data.Register(this);

            Render();
        }

        //protected void Render() //TODO
        public void Render()
        {
            wrapper.X = 0;
            wrapper.Y = 0;
            wrapper.Width = Width;
            wrapper.Height = Height;

            if (!string.IsNullOrEmpty(Data.Title))
            {
                var width = Data.Title.Height(Data.Theme.Plot.Title.Size);
                var height = Data.Title.Height(Data.Theme.Plot.Title.Size);

                Title.X = Data.Theme.Plot.Title.Margin.Left;
                Title.Y = Data.Theme.Plot.Title.Margin.Top + height;
                Title.Width = Data.Theme.Plot.Title.Margin.Left + width + Data.Theme.Plot.Title.Margin.Right;
                Title.Height = Data.Theme.Plot.Title.Margin.Top + height + Data.Theme.Plot.Title.Margin.Bottom;

                wrapper.Y += Title.Height;
                wrapper.Height -= Title.Height;
            }

            if (!string.IsNullOrEmpty(Data.SubTitle))
            {
                var width = Data.SubTitle.Height(Data.Theme.Plot.SubTitle.Size);
                var height = Data.SubTitle.Height(Data.Theme.Plot.SubTitle.Size);

                SubTitle.X = Data.Theme.Plot.SubTitle.Margin.Left;
                SubTitle.Y = Title.Height + Data.Theme.Plot.SubTitle.Margin.Top + height;
                SubTitle.Width = Data.Theme.Plot.SubTitle.Margin.Left + width + Data.Theme.Plot.SubTitle.Margin.Right;
                SubTitle.Height = Data.Theme.Plot.SubTitle.Margin.Top + height + Data.Theme.Plot.SubTitle.Margin.Bottom;

                wrapper.Y += SubTitle.Height;
                wrapper.Height -= SubTitle.Height;
            }

            if (!string.IsNullOrEmpty(Data.Caption))
            {
                var width = Data.Caption.Height(Data.Theme.Plot.Caption.Size);
                var height = Data.Caption.Height(Data.Theme.Plot.Caption.Size);

                Caption.Y = Height - Data.Theme.Plot.Caption.Margin.Bottom;
                Caption.Width = Data.Theme.Plot.Caption.Margin.Left + width + Data.Theme.Plot.Caption.Margin.Right;
                Caption.Height = Data.Theme.Plot.Caption.Margin.Top + height + Data.Theme.Plot.Caption.Margin.Bottom;

                wrapper.Height -= Caption.Height;
            }

            if (Data.Legends.Count > 0)
            {
                var (width, height) = Data.Legends.Dimension();

                if (width > 0 && height > 0)
                {
                    if (Data.Theme.Legend.Position == Right)
                    {
                        Legend.X = Width - width - Data.Theme.Legend.Margin.Right;
                        Legend.Y = wrapper.Y + (wrapper.Height - height) / 2.0;
                        Legend.Width = Data.Theme.Legend.Margin.Left + width + Data.Theme.Legend.Margin.Right;
                        Legend.Height = wrapper.Height;

                        wrapper.Width -= Legend.Width;
                    }
                    else if (Data.Theme.Legend.Position == Left)
                    {
                        Legend.X = Data.Theme.Legend.Margin.Left;
                        Legend.Y = wrapper.Y + (wrapper.Height - height) / 2.0; ;
                        Legend.Width = Data.Theme.Legend.Margin.Left + width + Data.Theme.Legend.Margin.Right;
                        Legend.Height = wrapper.Height;

                        wrapper.X += Legend.Width;
                        wrapper.Width -= Legend.Width;
                    }
                    else if (Data.Theme.Legend.Position == Top)
                    {
                        Legend.X = wrapper.X + (wrapper.Width - width) / 2.0;
                        Legend.Y = wrapper.Y + Data.Theme.Legend.Margin.Top;
                        Legend.Width = wrapper.Width;
                        Legend.Height = Data.Theme.Legend.Margin.Top + height + Data.Theme.Legend.Margin.Bottom;

                        wrapper.Y += Legend.Height;
                        wrapper.Height -= Legend.Height;
                    }
                    else if (Data.Theme.Legend.Position == Bottom)
                    {
                        Legend.X = wrapper.X + (wrapper.Width - width) / 2.0;
                        Legend.Y = wrapper.Y + wrapper.Height - height - Data.Theme.Legend.Margin.Bottom;
                        Legend.Width = wrapper.Width;
                        Legend.Height = Data.Theme.Legend.Margin.Top + height + Data.Theme.Legend.Margin.Bottom;

                        wrapper.Height -= Legend.Height;
                    }
                }
            }

            if (Caption.Width > 0)
            {
                Caption.X = wrapper.X + wrapper.Width - Data.Theme.Plot.Caption.Margin.Right;
            }

            for (var i = 0; i < Data.Panels.Count; i++)
            {
                Data.Panels[i].Component?.Render();
            }
        }
    }
}
