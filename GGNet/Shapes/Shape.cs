using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Shapes
{
    public abstract class Shape
    {
        public string Classes { get; set; }

        public Func<MouseEventArgs, Task> OnClick { get; internal set; }

        public Func<MouseEventArgs, Task> OnMouseOver { get; internal set; }

        public Func<MouseEventArgs, Task> OnMouseOut { get; internal set; }
    }
}
