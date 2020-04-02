using Microsoft.AspNetCore.Components;

namespace GGNet.Components.Definitions
{
    public partial class Animation : ComponentBase
    {
        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public Theme Theme { get; set; }

        protected override bool ShouldRender() => false;
    }
}
