using System.Text;

using Microsoft.AspNetCore.Components;

namespace GGNet.Components
{
    public partial class Area<T, TX, TY> : ComponentBase
       where TX : struct
       where TY : struct
    {
        [Parameter]
        public Data<T, TX, TY>.Panel Data { get; set; }

        [Parameter]
        public ICoord Coord { get; set; }

        [Parameter]
        public Zone Zone { get; set; }

        [Parameter]
        public string Clip { get; set; }

        private StringBuilder sb = new StringBuilder();
    }
}
