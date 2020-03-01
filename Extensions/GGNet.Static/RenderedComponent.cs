using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

namespace GGNet.Static
{
    public class RenderedComponent<TComponent> where TComponent : IComponent
    {
        private readonly StaticRenderer renderer;
        private readonly ContainerComponent container;
        private int id;

        internal RenderedComponent(StaticRenderer renderer)
        {
            this.renderer = renderer;
            container = new ContainerComponent(this.renderer);
        }

        internal async Task RenderAsync(ParameterView parameters)
        {
            await container.RenderAsync(typeof(TComponent), parameters);

            id = container.Child();
        }

        public void WriteHTML(TextWriter writer) => SVGRenderer.Render(renderer, id, writer);
    }
}
