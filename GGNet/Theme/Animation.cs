namespace GGNet
{
    public partial class Theme
    {
        public class _Animation
        {
            public _Animation(bool dark)
            {
                Point = new _Point();
                Bar = new _Bar(dark);
            }

            public class _Point
            {
                public _Point()
                {
                    Scale = 1.5;

                    Transition = 0.2;
                }

                public double Scale { get; set; }

                public double Transition { get; set; }
            }

            public _Point Point { get; set; }

            public class _Bar
            {
                public _Bar(bool dark)
                {
                    Flood = dark ? "#FFFFFF" : "#000000";

                    Transition = 0.2;
                }

                public string Flood { get; set; }

                public double Transition { get; set; }
            }

            public _Bar Bar { get; set; }
        }
    }
}
