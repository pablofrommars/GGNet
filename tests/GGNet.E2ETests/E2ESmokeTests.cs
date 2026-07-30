namespace GGNet.E2ETests;

// The executed-JS smoke layer (implementation-blocks §9 Layer 3): the only
// tests that run the collocated module in a real browser — wheel coordinates
// against a responsive svg, the drag-pan preview transform, and the cursor-
// glued popover. Thin by design; behavior details live in bUnit and xUnit.
public class E2ESmokeTests(DemoAppFixture fixture) : IClassFixture<DemoAppFixture>
{
	private async Task<IPage> PageAsync(string path)
	{
		var page = await fixture.Browser.NewPageAsync();

		await page.GotoAsync(fixture.BaseUrl + path, new() { WaitUntil = WaitUntilState.NetworkIdle });

		// Give the circuit a moment to become interactive after prerender.
		await page.WaitForTimeoutAsync(1000);

		return page;
	}

	private static async Task<string> XLabelsAsync(IPage page, int plot = 0)
		=> string.Join("|", await page.Locator(".ggnet svg").Nth(plot).Locator("text.x-break-label").AllInnerTextsAsync());

	private static async Task WaitUntilAsync(Func<Task<bool>> condition, string because)
	{
		for (var i = 0; i < 50; i++)
		{
			if (await condition())
			{
				return;
			}

			await Task.Delay(100);
		}

		(await condition()).Should().BeTrue(because);
	}

	[SkippableFact]
	public async Task WheelZoomRecomputesTheAxisOnAResponsiveSvg()
	{
		Skip.IfNot(fixture.Available, fixture.Reason);

		// Arrange

		var page = await PageAsync("/wheel-zoom");

		var before = await XLabelsAsync(page);
		var panel = page.Locator("rect.panel").First;

		// Act / Assert

		// The svg renders responsive (no width attribute): the module converts
		// client px to svg units against the rendered size per event.
		await panel.HoverAsync();
		await page.Mouse.WheelAsync(0, -300);

		await WaitUntilAsync(async () => await XLabelsAsync(page) != before, "wheel over the panel should window the x axis");

		await panel.DblClickAsync();

		await WaitUntilAsync(async () => await XLabelsAsync(page) == before, "double-click should restore the authored view");
	}

	[SkippableFact]
	public async Task DragPanPreviewsClientSideAndCommitsOnRelease()
	{
		Skip.IfNot(fixture.Available, fixture.Reason);

		// Arrange

		var page = await PageAsync("/drag-pan");

		var before = await XLabelsAsync(page);
		var panel = page.Locator("rect.panel").First;

		var box = await panel.BoundingBoxAsync();
		var cx = (float)(box!.X + box.Width / 2);
		var cy = (float)(box.Y + box.Height / 2);

		// The marks-only transform target: capture group's direct child.
		var target = page.Locator("svg > g > g").First;

		// Act / Assert

		await page.Mouse.MoveAsync(cx, cy);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync(cx + 120, cy, new() { Steps = 8 });

		(await target.GetAttributeAsync("transform")).Should().NotBeNull("the drag must preview as a client-side transform");

		await page.Mouse.UpAsync();

		await WaitUntilAsync(async () => await XLabelsAsync(page) != before, "releasing the drag should commit a window shift");

		(await target.GetAttributeAsync("transform")).Should().BeNull("the preview transform must clear on commit");
	}

	[SkippableFact]
	public async Task CursorTooltipOpensAsAPopoverAndFollowsThePointer()
	{
		Skip.IfNot(fixture.Available, fixture.Reason);

		// Arrange

		var page = await PageAsync("/tooltips");

		// The second plot on the page is the cursor-glued one. Marks live in
		// g[transform] wrappers — a bare "circle" would match a legend swatch.
		var circle = page.Locator(".ggnet").Nth(1).Locator("g[transform] > circle").First;

		// Act

		await circle.HoverAsync();

		// The container is a zero-size anchor; the bubble is the visible box,
		// and it is only rendered while the popover is in the top layer.
		var popover = page.Locator("div.container[popover]").First;
		var bubble = popover.Locator(".bubble");

		await bubble.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

		// Assert

		var open = await popover.EvaluateAsync<bool>("el => el.matches(':popover-open')");
		open.Should().BeTrue("the glued tooltip renders in the top layer");

		var first = await bubble.BoundingBoxAsync();

		// Nudge within the mark's radius: the bubble must follow, client-side.
		var circleBox = await circle.BoundingBoxAsync();
		await page.Mouse.MoveAsync((float)(circleBox!.X + circleBox.Width / 2 + 3), (float)(circleBox.Y + circleBox.Height / 2));
		await page.WaitForTimeoutAsync(200);

		var second = await bubble.BoundingBoxAsync();

		second!.X.Should().BeGreaterThan(first!.X, "the bubble glues to the pointer");
	}
}
