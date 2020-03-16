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

        internal async Task<RenderedComponent> RenderAsync(Type type, IDictionary<string, object> parameters = null)
        {
            var component = new RenderedComponent(renderer.Value);

            await component.RenderAsync(type, parameters == null ? ParameterView.Empty : ParameterView.FromDictionary(parameters));

            return component;
        }

        private static readonly Lazy<Host> lazy = new Lazy<Host>(() => new Host());

        public static Host Instance => lazy.Value;
    }
}
