using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace GGNet.Static
{
    [SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "<Pending>")]
    internal class StaticRenderer : Renderer
    {
        private Exception _unhandledException;

        private TaskCompletionSource<object> _nextRenderTcs = new TaskCompletionSource<object>();

        public StaticRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
            : base(serviceProvider, loggerFactory)
        {
        }

        public new ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId)
            => base.GetCurrentRenderTreeFrames(componentId);

        public int AttachContainer(ContainerComponent container) 
            => AssignRootComponentId(container);

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public Task NextRender => _nextRenderTcs.Task;

        protected override void HandleException(Exception exception)
        {
            _unhandledException = exception;
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            var prevTcs = _nextRenderTcs;
            _nextRenderTcs = new TaskCompletionSource<object>();
            prevTcs.SetResult(null);
            return Task.CompletedTask;
        }

        public async Task Dispatch(Action callback)
        {
            await Dispatcher.InvokeAsync(callback);

            if (_unhandledException != null)
            {
                ExceptionDispatchInfo.Capture(_unhandledException).Throw();
            }
        }
    }
}
