namespace GGNet.E2ETests;

public class PreviewClippingTests(DemoAppFixture fixture) : IClassFixture<DemoAppFixture>
{
	[Theory]
	[InlineData("left")]
	[InlineData("right")]
	[InlineData("top")]
	[InlineData("bottom")]
	public async Task PreviewCannotPaintOrHitTestBeyondThePanel(string edge)
	{
		Assert.SkipUnless(fixture.Available, fixture.Reason);

		// Arrange

		var page = await fixture.Browser.NewPageAsync(new() { ViewportSize = new() { Width = 1400, Height = 1600 } });

		try
		{
			await page.GotoAsync(fixture.BaseUrl + "/drag-pan", new() { WaitUntil = WaitUntilState.NetworkIdle });
			var target = page.Locator("[data-ggnet-pan]").First;
			await target.WaitForAsync();

			// Apply the same marks-only transform as the gesture module directly:
			// this isolates SVG clipping on both axes from the demo's x-only pan.

			// Act

			var result = await target.EvaluateAsync<bool[]>("""
				async (target, edge) => {
				    let svg = target.ownerSVGElement;
				    while (svg.ownerSVGElement) {
				        svg = svg.ownerSVGElement;
				    }
				    const outer = target.parentElement;
				    const panel = outer.parentElement.querySelector('rect.panel');
				    const mark = target.querySelector('circle');
				    const original = mark.getBoundingClientRect();
				    const center = new DOMPoint(original.x + original.width / 2,
				        original.y + original.height / 2).matrixTransform(svg.getScreenCTM().inverse());
				    const bounds = panel.getBBox();
				    const point = new DOMPoint(
				        edge === 'left' ? bounds.x - 8 : edge === 'right' ? bounds.x + bounds.width + 8 : center.x,
				        edge === 'top' ? bounds.y - 8 : edge === 'bottom' ? bounds.y + bounds.height + 8 : center.y);
				    const clip = outer.getAttribute('clip-path');
				    const previousStyle = mark.getAttribute('style');
				    const previousTransform = target.getAttribute('transform');
				    mark.style.setProperty('fill', '#ff00fe', 'important');
				    mark.style.setProperty('stroke', '#ff00fe', 'important');
				    mark.style.setProperty('fill-opacity', '1', 'important');
				    mark.style.setProperty('pointer-events', 'all', 'important');
				    target.setAttribute('transform', `translate(${point.x - center.x}, ${point.y - center.y})`);
				    const client = point.matrixTransform(svg.getScreenCTM());
				    const hit = () => document.elementsFromPoint(client.x, client.y).includes(mark);
				    const painted = async () => {
				        const clone = svg.cloneNode(true);
				        const view = svg.viewBox.baseVal;
				        clone.setAttribute('width', view.width);
				        clone.setAttribute('height', view.height);
				        const url = URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(clone)],
				            { type: 'image/svg+xml' }));
				        try {
				            const image = new Image();
				            image.src = url;
				            await image.decode();
				            const canvas = document.createElement('canvas');
				            canvas.width = view.width;
				            canvas.height = view.height;
				            const context = canvas.getContext('2d');
				            context.drawImage(image, 0, 0);
				            const [r, g, b, a] = context.getImageData(Math.floor(point.x - view.x),
				                Math.floor(point.y - view.y), 1, 1).data;
				            return r > 240 && g < 15 && b > 240 && a > 240;
				        } finally {
				            URL.revokeObjectURL(url);
				        }
				    };
				    try {
				        const clippedHit = hit();
				        const clippedPaint = await painted();
				        outer.removeAttribute('clip-path');
				        const unboundedHit = hit();
				        const unboundedPaint = await painted();
				        return [clippedHit, clippedPaint, unboundedHit, unboundedPaint];
				    } finally {
				        if (clip !== null) outer.setAttribute('clip-path', clip);
				        if (previousStyle === null) mark.removeAttribute('style');
				        else mark.setAttribute('style', previousStyle);
				        if (previousTransform === null) target.removeAttribute('transform');
				        else target.setAttribute('transform', previousTransform);
				    }
				}
				""", edge);

			// Assert

			using var _ = new AssertionScope();

			result[0].Should().BeFalse("the preview must not intercept events outside the {0} edge", edge);
			result[1].Should().BeFalse("the preview must not paint outside the {0} edge", edge);
			result[2].Should().BeTrue("removing only the fixed outer clip must reproduce the hit-test defect");
			result[3].Should().BeTrue("removing only the fixed outer clip must reproduce the visible spill");
		}
		finally
		{
			await page.CloseAsync();
		}
	}
}
