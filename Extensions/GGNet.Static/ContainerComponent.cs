using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

namespace GGNet.Static
{
    [SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "<Pending>")]
    internal class ContainerComponent : IComponent
    {
        private readonly StaticRenderer renderer;
        private readonly int id;

        public ContainerComponent(StaticRenderer renderer)
        {
            this.renderer = renderer;
            id = renderer.AttachContainer(this);
        }

        private RenderHandle handle;

        public void Attach(RenderHandle renderHandle) => handle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();

        public Task RenderAsync(Type componentType, ParameterView parameters) => renderer.Dispatch(() =>
        {
            handle.Render(builder =>
            {
                builder.OpenComponent(0, componentType);

                foreach (var parameterValue in parameters)
                {
                    builder.AddAttribute(1, parameterValue.Name, parameterValue.Value);
                }

                builder.CloseComponent();
            });
        });

        public int Child() => renderer.GetCurrentRenderTreeFrames(id).Array[0].ComponentId;
    }
}
