using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

namespace GGNet.Components
{
    public abstract class PlotBase<T, TX, TY> : ComponentBase, IPlot, IAsyncDisposable
        where TX : struct
        where TY : struct
    {
        [Parameter]
        public Data<T, TX, TY> Data { get; set; }

        [Parameter]
        public RenderPolicy RenderPolicy { get; set; } = RenderPolicy.Never;

        public string Id => Data.Id;

        public Theme Theme => Data.Theme;

        protected override void OnInitialized()
        {
            Policy = RenderPolicyBase.Factory(RenderPolicy, this);
        }

        public RenderPolicyBase Policy { get; private set; }

        public abstract void Render();

        public Task StateHasChangedAsync() => InvokeAsync(() => StateHasChanged());

        protected override bool ShouldRender() => Policy.ShouldRender();

        protected override void OnAfterRender(bool firstRender) => Policy.OnAfterRender(firstRender);

        public Task RefreshAsync() => Policy.RefreshAsync();

        public ValueTask DisposeAsync() => Policy.DisposeAsync();
    }

    public interface IPlot
    {
        RenderPolicyBase Policy { get; }

        void Render();

        Task StateHasChangedAsync();
    }

    public abstract class RenderChildPolicyBase
    {
        public virtual void Refresh() { }

        public virtual bool ShouldRender() => false;
    }

    public abstract class RenderPolicyBase : IAsyncDisposable
    {
        protected readonly IPlot plot;

        public RenderPolicyBase(IPlot plot)
        {
            this.plot = plot;
        }

        public virtual Task RefreshAsync() => Task.CompletedTask;

        public virtual bool ShouldRender() => false;

        public virtual void OnAfterRender(bool firstRender) { }

        public virtual ValueTask DisposeAsync() => default;

        public abstract RenderChildPolicyBase Child();

        public static RenderPolicyBase Factory(RenderPolicy policy, IPlot component) => policy switch
        {
            RenderPolicy.Always => new AlwaysRenderPolicy(component),
            RenderPolicy.Never => new NeverRenderPolicy(component),
            _ => new AutoRenderPolicy(component),
        };
    }

    public class NeverRenderPolicy : RenderPolicyBase
    {
        public NeverRenderPolicy(IPlot plot)
            : base(plot)
        {
        }

        public class RenderChildPolicy : RenderChildPolicyBase
        {

        }

        public override RenderChildPolicyBase Child() => new RenderChildPolicy();
    }

    public class AlwaysRenderPolicy : RenderPolicyBase
    {
        public AlwaysRenderPolicy(IPlot plot)
            : base(plot)
        {
        }

        public override Task RefreshAsync()
        {
            plot.Render();

            return plot.StateHasChangedAsync();
        }

        public override bool ShouldRender() => true;

        public class RenderChildPolicy : RenderChildPolicyBase
        {
            public override bool ShouldRender() => true;
        }

        public override RenderChildPolicyBase Child() => new RenderChildPolicy();
    }

    public class AutoRenderPolicy : RenderPolicyBase
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        private ChannelWriter<int> writer;

        private volatile int render = 0;

        private Task task;

        public AutoRenderPolicy(IPlot plot)
            : base(plot)
        {
            task = Task.Factory.StartNew(async () =>
            {
                try
                {
                    var channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = true
                    });

                    var reader = channel.Reader;
                    writer = channel.Writer;

                    var token = cancellationTokenSource.Token;

                    while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                    {
                        while (reader.TryRead(out _))
                        {
                        }

                        plot.Render();

                        Interlocked.Exchange(ref render, 1);

                        await plot.StateHasChangedAsync().ConfigureAwait(false);

                        try
                        {
                            await semaphore.WaitAsync(token).ConfigureAwait(false);
                        }
                        finally
                        {
                        }
                    }
                }
                catch (Exception e)
                {
                }
            }, TaskCreationOptions.LongRunning);
        }

        public override async Task RefreshAsync()
        {
            await writer.WriteAsync(1, cancellationTokenSource.Token);
        }

        public override bool ShouldRender() => Interlocked.Exchange(ref render, 0) == 1;

        public override void OnAfterRender(bool firstRender)
        {
            if (!firstRender)
            {
                semaphore.Release();
            }
        }

        public class RenderChildPolicy : RenderChildPolicyBase
        {
            private volatile int render = 0;

            public override void Refresh() => Interlocked.Exchange(ref render, 1);

            public override bool ShouldRender() => Interlocked.Exchange(ref render, 0) == 1;
        }

        public override RenderChildPolicyBase Child() => new RenderChildPolicy();

        public override async ValueTask DisposeAsync()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();

            if (task != null)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
