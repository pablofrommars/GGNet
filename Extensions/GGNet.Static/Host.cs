using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace GGNet.Static
{
    public class Host
    {
        private readonly ServiceCollection _serviceCollection = new ServiceCollection();

        private readonly Lazy<StaticRenderer> renderer;
        private readonly Lazy<IServiceProvider> provider;

        private Host()
        {
            provider = new Lazy<IServiceProvider>(() => _serviceCollection.BuildServiceProvider());

            renderer = new Lazy<StaticRenderer>(() => new StaticRenderer(provider.Value, new NullLoggerFactory()));
        }

        public async Task<RenderedComponent<TComponent>> RenderAsync<TComponent>(IDictionary<string, object> parameters = null) where TComponent : IComponent
        {
            var component = new RenderedComponent<TComponent>(renderer.Value);

            await component.RenderAsync(parameters == null ? ParameterView.Empty : ParameterView.FromDictionary(parameters));

            return component;
        }

        private static readonly Lazy<Host> lazy = new Lazy<Host>(() => new Host());

        public static Host Instance => lazy.Value;
    }
}
