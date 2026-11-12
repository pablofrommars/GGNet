using System.Runtime.CompilerServices;
using System.Text.Json;

using static GGNet.E2ETests.Fixtures.TouchInput;

namespace GGNet.E2ETests;

public class GestureModuleTests(DemoAppFixture fixture) : IClassFixture<DemoAppFixture>
{
	[Theory]
	[InlineData("width:720px;height:576px")]
	[InlineData("width:720px;height:300px")]
	[InlineData("width:600px;height:600px;transform:rotate(8deg) skewX(5deg);transform-origin:0 0")]
	public async Task WheelAndPanUseTheCaptureScreenTransform(string style)
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);
		await page.Locator("#plot").EvaluateAsync("(el, style) => el.style.cssText=style", style);
		var start = await page.EvaluateAsync<double[]>("() => { const p = new DOMPoint(250,250).matrixTransform(document.querySelector('#capture').getScreenCTM()); return [p.x,p.y]; }");

		// Act

		await page.Mouse.MoveAsync((float)start[0], (float)start[1]);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync((float)start[0] + 30, (float)start[1] + 20);
		await page.EvaluateAsync("() => document.querySelector('#capture').dispatchEvent(new WheelEvent('wheel',{clientX:300,clientY:350,deltaY:-100}))");
		await page.Mouse.MoveAsync((float)start[0] + 60, (float)start[1] + 40);
		var preview = await page.Locator("#target").GetAttributeAsync("transform");
		await page.Mouse.UpAsync();
		var actual = await CallsAsync(page);
		var expected = await page.EvaluateAsync<double[]>("() => { const m=document.querySelector('#capture').getScreenCTM().inverse();const p=new DOMPoint(300,350).matrixTransform(m);const d=new DOMPoint(60,40).matrixTransform(new DOMMatrix([m.a,m.b,m.c,m.d,0,0]));return [p.x,p.y,d.x,d.y]; }");

		// Assert

		using var _ = new AssertionScope();
		actual.GetArrayLength().Should().Be(2);
		actual[0][0].GetString().Should().Be("OnWheelAsync");
		actual[0][1].GetDouble().Should().BeApproximately(expected[0], 0.001);
		actual[0][2].GetDouble().Should().BeApproximately(expected[1], 0.001);
		actual[1][0].GetString().Should().Be("OnPanEndAsync");
		actual[1][1].GetDouble().Should().BeApproximately(expected[2], 0.001);
		actual[1][2].GetDouble().Should().BeApproximately(expected[3], 0.001);
		preview.Should().StartWith("translate(");
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
	}

	[Theory]
	[InlineData("document.querySelector('#plot').style.height='300px'")]
	[InlineData("document.querySelector('#plot').style.transform='scale(0)'")]
	[InlineData("document.querySelector('#capture').getScreenCTM=()=>null")]
	[InlineData("document.querySelector('#capture').remove()")]
	public async Task ChangingOrRemovingTheViewportCancelsAnActivePan(string change)
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);
		await page.Mouse.MoveAsync(300, 400);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync(340, 400);

		// Act

		await page.EvaluateAsync("() => { window.savedTarget=document.querySelector('#target'); " + change + "; }");
		await FramesAsync(page);
		var preview = await page.EvaluateAsync<string?>("() => window.savedTarget.getAttribute('transform')");
		await page.Mouse.UpAsync();

		// Assert

		using var _ = new AssertionScope();
		preview.Should().BeNull();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task MouseDragStartingOnAnAxisCannotPanThePanel()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);

		// Act

		await page.Mouse.MoveAsync(400, 645);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync(350, 645);
		await page.Mouse.UpAsync();

		// Assert

		using var _ = new AssertionScope();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
	}

	[Fact]
	public async Task SingularAndDetachedCapturesDoNotSendWheelCoordinates()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);

		// Act

		await page.EvaluateAsync("() => { const c=document.querySelector('#capture');document.querySelector('#plot').style.transform='scale(0)';c.dispatchEvent(new WheelEvent('wheel',{clientX:300,clientY:400,deltaY:-1})); c.remove();c.dispatchEvent(new WheelEvent('wheel',{clientX:300,clientY:400,deltaY:-1})); }");

		// Assert

		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task LostCaptureCancelsThePreviewAndReturningWithoutButtonsDoesNotPan()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);
		await page.Mouse.MoveAsync(300, 400);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync(330, 400);

		// Act

		await page.EvaluateAsync("() => document.querySelector('#capture').releasePointerCapture(1)");
		await page.Mouse.MoveAsync(850, 650);
		await page.Mouse.UpAsync();
		await page.Mouse.MoveAsync(350, 400);

		// Assert

		using var _ = new AssertionScope();
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0, await page.EvaluateAsync<string>("() => JSON.stringify({events:window.events,calls:window.calls})"));
	}

	[Fact]
	public async Task OnlyTheOwningTouchCanMoveOrCommitThePan()
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context);
		// Isolate pointer ownership from native pinch, which has separate real-input coverage.
		await page.Locator("#viewport").EvaluateAsync("el => el.style.touchAction='none'");
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await TouchAsync(cdp, "touchStart", Point(250, 400, 1));
		await TouchAsync(cdp, "touchStart", Point(250, 400, 1), Point(450, 400, 2));
		await TouchAsync(cdp, "touchMove", Point(280, 400, 1), Point(450, 400, 2));
		var preview = await page.Locator("#target").GetAttributeAsync("transform");
		await TouchAsync(cdp, "touchEnd");
		var calls = await CallsAsync(page);

		// Assert

		using var _ = new AssertionScope();
		preview.Should().Be("translate(30, 0)");
		calls.GetArrayLength().Should().Be(1, await page.EvaluateAsync<string>("() => JSON.stringify({events:window.events,scrollX,scrollY,innerWidth,innerHeight,scale:visualViewport.scale,body:document.body.getBoundingClientRect().toJSON()})"));
		calls[0][1].GetDouble().Should().Be(30);
		calls[0][2].GetDouble().Should().Be(0);
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
	}

	[Fact]
	public async Task OwnerCancellationAndDisposalClearActivePreviewsWithoutCommitting()
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await TouchAsync(cdp, "touchStart", Point(250, 400));
		await TouchAsync(cdp, "touchMove", Point(280, 400));
		var beforeCancel = await page.Locator("#target").GetAttributeAsync("transform");
		await TouchAsync(cdp, "touchCancel");
		var cancelled = await page.Locator("#target").GetAttributeAsync("transform");
		await TouchAsync(cdp, "touchStart", Point(250, 400));
		await TouchAsync(cdp, "touchMove", Point(280, 400));
		var beforeDispose = await page.Locator("#target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => { window.module.dispose('panel');window.module.dispose('panel'); }");
		await TouchAsync(cdp, "touchEnd");
		await page.EvaluateAsync("() => document.querySelector('#capture').dispatchEvent(new WheelEvent('wheel',{deltaY:-1}))");

		// Assert

		using var _ = new AssertionScope();
		beforeCancel.Should().Be("translate(30, 0)");
		beforeDispose.Should().Be("translate(30, 0)");
		cancelled.Should().BeNull();
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task ReinitializationAndIndependentIdsKeepExactlyOneListenerEach()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);

		// Act

		await page.EvaluateAsync("() => { window.initialize();window.initialize();document.querySelector('#capture').dispatchEvent(new WheelEvent('wheel',{clientX:300,clientY:400,deltaY:-1}));window.reinitializedCount=window.calls.length;window.calls=[];const other=document.querySelector('#plot').cloneNode(true);document.body.append(other);window.module.initialize('other',other.querySelector('#capture'),other.querySelector('#target'),{invokeMethodAsync:async(...a)=>window.otherCalls.push(a)},window.options);window.module.dispose('panel');other.querySelector('#capture').dispatchEvent(new WheelEvent('wheel',{clientX:300,clientY:400,deltaY:-1}));document.querySelector('#capture').dispatchEvent(new WheelEvent('wheel',{clientX:300,clientY:400,deltaY:-1})); }");

		// Assert

		using var _ = new AssertionScope();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
		(await page.EvaluateAsync<JsonElement>("() => window.otherCalls")).GetArrayLength().Should().Be(1);
		(await page.EvaluateAsync<int>("() => window.reinitializedCount")).Should().Be(1);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(2)]
	[InlineData(6)]
	public async Task TapWithFingerJitterClicksTheMarkWithoutPanning(int jitter)
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await TouchAsync(cdp, "touchStart", Point(300, 400));
		await TouchAsync(cdp, "touchMove", Point(300 + jitter, 400));
		var preview = await page.Locator("#target").GetAttributeAsync("transform");
		await TouchAsync(cdp, "touchEnd");
		await page.WaitForTimeoutAsync(400);

		// Assert

		using var _ = new AssertionScope();
		preview.Should().BeNull();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
		(await page.EvaluateAsync<string[]>("() => window.clicks")).Should().Equal("mark");
	}

	[Fact]
	public async Task StationaryTapsPositionTheCursorTooltipAtEachContact()
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, true, false, true);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await TouchAsync(cdp, "touchStart", Point(300, 400));
		await TouchAsync(cdp, "touchEnd");
		var first = await ShowTooltipAsync(page);
		await TouchAsync(cdp, "touchStart", Point(420, 250));
		await TouchAsync(cdp, "touchEnd");
		var second = await ShowTooltipAsync(page);

		// Assert

		using var _ = new AssertionScope();
		first.Should().Equal("312px", "412px");
		second.Should().Equal("432px", "262px");
	}

	[Theory]
	[InlineData(false, 40)]
	[InlineData(true, 12)]
	[InlineData(true, 40)]
	public async Task RecognizedDragCommitsOnceAndSuppressesTheTrailingClick(bool touch, int distance)
	{
		// Arrange

		await using var context = await ContextAsync(touch);
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await PressDragReleaseAsync(page, cdp, touch, 300, 400, distance);
		var afterDrag = await page.EvaluateAsync<string[]>("() => window.clicks");
		await PressDragReleaseAsync(page, cdp, touch, 300, 400, 0);
		var calls = await CallsAsync(page);

		// Assert

		using var _ = new AssertionScope();
		calls.GetArrayLength().Should().Be(1);
		calls[0][1].GetDouble().Should().Be(distance);
		afterDrag.Should().BeEmpty("the click that trails a drag reaches neither the mark nor an ancestor");
		(await page.EvaluateAsync<string[]>("() => window.clicks")).Should().Equal(["mark"], "the next ordinary press still clicks");
	}

	[Fact]
	public async Task ReleasedPreviewIsHeldUntilTheCommittedFrameArrives()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);
		await page.EvaluateAsync("() => { window.holdFrames=true; }");

		// Act

		await PressDragReleaseAsync(page, cdp, false, 300, 400, 40);
		var released = await page.Locator("#target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => window.frame()");
		await FramesAsync(page);
		var committed = await page.Locator("#target").GetAttributeAsync("transform");
		await PressDragReleaseAsync(page, cdp, false, 300, 400, 40);
		await page.WaitForTimeoutAsync(1500);

		// Assert

		using var _ = new AssertionScope();
		released.Should().Be("translate(40, 0)", "clearing at release would flash the marks at their old position");
		committed.Should().BeNull();
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull("a frame that never arrives must not strand the preview");
	}

	[Fact]
	public async Task MouseJitterBelowTheThresholdIsAClickNotAPan()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await PressDragReleaseAsync(page, cdp, false, 300, 400, 2);
		var clicks = await page.EvaluateAsync<string[]>("() => window.clicks");
		await PressDragReleaseAsync(page, cdp, false, 300, 400, 40);

		// Assert

		using var _ = new AssertionScope();
		clicks.Should().Equal("mark");
		(await CallsAsync(page)).GetArrayLength().Should().Be(1, "an unrecognized press must not block the next pan");
	}

	[Fact]
	public async Task SecondFingerJoiningAnActivePanHandsTheGestureToNativePinch()
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await TouchAsync(cdp, "touchStart", Point(250, 400, 1));
		await TouchAsync(cdp, "touchMove", Point(280, 400, 1));
		var preview = await page.Locator("#target").GetAttributeAsync("transform");
		await TouchAsync(cdp, "touchStart", Point(280, 400, 1), Point(480, 400, 2));
		for (var i = 1; i <= 10; i++)
		{
			await TouchAsync(cdp, "touchMove", Point(280 - i * 10, 400, 1), Point(480 + i * 10, 400, 2));
			await page.WaitForTimeoutAsync(30);
		}
		await TouchAsync(cdp, "touchEnd");

		// Assert

		using var _ = new AssertionScope();
		preview.Should().Be("translate(30, 0)");
		(await page.EvaluateAsync<double>("() => visualViewport.scale")).Should().BeGreaterThan(1.1);
		(await CallsAsync(page)).GetArrayLength().Should().Be(0, await page.EvaluateAsync<string>("() => JSON.stringify(window.events)"));
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
	}

	[Fact]
	public async Task OwningFingerLiftingFirstCommitsAndTheRemainingFingerCannotPan()
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context);
		// Isolate pointer ownership from native pinch, which has separate real-input coverage.
		await page.Locator("#viewport").EvaluateAsync("el => el.style.touchAction='none'");
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await TouchAsync(cdp, "touchStart", Point(250, 400, 1));
		await TouchAsync(cdp, "touchStart", Point(250, 400, 1), Point(450, 400, 2));
		await TouchAsync(cdp, "touchMove", Point(280, 400, 1), Point(450, 400, 2));
		await TouchAsync(cdp, "touchEnd", Point(280, 400, 1));
		var committed = (await CallsAsync(page)).GetArrayLength();
		await TouchAsync(cdp, "touchMove", Point(500, 400, 2));
		var preview = await page.Locator("#target").GetAttributeAsync("transform");
		await TouchAsync(cdp, "touchEnd");

		// Assert

		using var _ = new AssertionScope();
		committed.Should().Be(1);
		preview.Should().BeNull();
		(await CallsAsync(page)).GetArrayLength().Should().Be(1, await page.EvaluateAsync<string>("() => JSON.stringify(window.events)"));
	}

	[Fact]
	public async Task TouchTooltipIsDismissedByScrollingAndReturnsOnTheNextPress()
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, true, false, true);
		var cdp = await context.NewCDPSessionAsync(page);
		await TouchAsync(cdp, "touchStart", Point(300, 400));
		await TouchAsync(cdp, "touchEnd");
		await ShowTooltipAsync(page);
		var shown = await TooltipOpenAsync(page);

		// Act

		await page.EvaluateAsync("() => window.scrollBy(0, 60)");
		await FramesAsync(page);
		var scrolled = await TooltipOpenAsync(page);
		await ShowTooltipAsync(page);
		var rerendered = await TooltipOpenAsync(page);
		await TouchAsync(cdp, "touchStart", Point(300, 340));
		await TouchAsync(cdp, "touchEnd");
		await ShowTooltipAsync(page);

		// Assert

		using var _ = new AssertionScope();
		shown.Should().BeTrue();
		scrolled.Should().BeFalse();
		rerendered.Should().BeFalse("a re-render must not resurrect a dismissed tooltip");
		(await TooltipOpenAsync(page)).Should().BeTrue();
	}

	[Fact]
	public async Task TouchPanDismissesTheTooltipWhileAMouseDragKeepsIt()
	{
		// Arrange

		await using var touchContext = await ContextAsync(true);
		var touchPage = await HarnessAsync(touchContext, true, false, true);
		var cdp = await touchContext.NewCDPSessionAsync(touchPage);
		await using var mouseContext = await ContextAsync();
		var mousePage = await HarnessAsync(mouseContext, true, false, true);

		// Act

		await TouchAsync(cdp, "touchStart", Point(300, 400));
		await TouchAsync(cdp, "touchEnd");
		await ShowTooltipAsync(touchPage);
		await PressDragReleaseAsync(touchPage, cdp, true, 300, 400, 40);
		await mousePage.Mouse.MoveAsync(300, 400);
		await ShowTooltipAsync(mousePage);
		await PressDragReleaseAsync(mousePage, cdp, false, 300, 400, 40);

		// Assert

		using var _ = new AssertionScope();
		(await TooltipOpenAsync(touchPage)).Should().BeFalse();
		(await TooltipOpenAsync(mousePage)).Should().BeTrue();
	}

	[Theory]
	[InlineData(60, "518px", "558px")]
	[InlineData(580, "4px", "558px")]
	public async Task TooltipFlipsAndClampsInsideTheViewportAtTheEdges(int width, string left, string top)
	{
		// Arrange

		// Mobile emulation widens the layout viewport to the harness page; a desktop
		// context keeps it at the window size the flip is measured against.
		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false, true);
		await page.SetViewportSizeAsync(610, 610);
		await page.Locator("#tip .bubble").EvaluateAsync("(el, width) => el.style.width=width+'px'", width);
		var edge = await page.EvaluateAsync<int[]>("() => [innerWidth, innerHeight]");

		// Act

		await page.Mouse.MoveAsync(590, 590);
		var position = await ShowTooltipAsync(page);

		// Assert

		position.Should().Equal([left, top], $"the viewport is {edge[0]}x{edge[1]}");
	}

	[Theory]
	[InlineData(true, false, 120, 0)]
	[InlineData(false, true, 0, -120)]
	[InlineData(true, true, 120, -120)]
	public async Task TouchPansTheConfiguredAxes(bool panX, bool panY, int dx, int dy)
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, panX, panY);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await SwipeAsync(page, cdp, 300, 400, dx, dy);
		var calls = await CallsAsync(page);

		// Assert

		using var _ = new AssertionScope();
		calls.GetArrayLength().Should().Be(1, await page.EvaluateAsync<string>("() => JSON.stringify({events:window.events,scrollX,scrollY,innerWidth,innerHeight,scale:visualViewport.scale,body:document.body.getBoundingClientRect().toJSON()})"));
		calls[0][0].GetString().Should().Be("OnPanEndAsync");
		calls[0][1].GetDouble().Should().BeApproximately(dx, 0.01);
		calls[0][2].GetDouble().Should().BeApproximately(dy, 0.01);
	}

	[Theory]
	[InlineData(true, false, 0, -180)]
	[InlineData(false, true, -180, 0)]
	public async Task UnusedTouchAxisStillScrollsThePage(bool panX, bool panY, int dx, int dy)
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, panX, panY);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await SwipeAsync(page, cdp, 400, 400, dx, dy);
		var scroll = await page.EvaluateAsync<double[]>("() => [visualViewport.pageLeft,visualViewport.pageTop]");

		// Assert

		using var _ = new AssertionScope();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
		scroll[panX ? 1 : 0].Should().BeGreaterThan(50);
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task NativePinchRemainsAvailable(bool panX, bool panY)
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, panX, panY);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await TouchAsync(cdp, "touchStart", Point(250, 400, 1), Point(450, 400, 2));
		for (var i = 1; i <= 10; i++)
		{
			await TouchAsync(cdp, "touchMove", Point(250 - i * 10, 400, 1), Point(450 + i * 10, 400, 2));
			await page.WaitForTimeoutAsync(30);
		}
		await TouchAsync(cdp, "touchEnd");

		// Assert

		using var _ = new AssertionScope();
		(await page.EvaluateAsync<double>("() => visualViewport.scale")).Should().BeGreaterThan(1.1);
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
	}

	[Theory]
	[InlineData(650, 400)]
	[InlineData(400, 645)]
	public async Task LegendAndOuterAxisHitSurfacesKeepNativeScrolling(int x, int y)
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await SwipeAsync(page, cdp, x, y, -150, 0);

		// Assert

		using var _ = new AssertionScope();
		(await page.EvaluateAsync<double>("() => visualViewport.pageLeft")).Should().BeGreaterThan(50);
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task DemoOwnsTouchPolicyOnlyInsideTheEffectivePanViewport()
	{
		// Arrange

		await using var context = await ContextAsync(true);
		var page = await context.NewPageAsync();
		await page.GotoAsync(fixture.BaseUrl + "/drag-pan", new() { WaitUntil = WaitUntilState.NetworkIdle });
		await page.WaitForTimeoutAsync(1000);
		await page.AddStyleTagAsync(new() { Content = ".ggnet svg { width: 100%; height: auto; }" });
		var viewport = page.Locator("[data-ggnet-viewport]").First;
		var panel = viewport.Locator("rect.panel");
		await panel.ScrollIntoViewIfNeededAsync();
		var box = await panel.BoundingBoxAsync();
		var mark = viewport.Locator("[data-ggnet-pan] g[transform] > circle").First;
		var before = await mark.EvaluateAsync<string>("el => el.parentElement.getAttribute('transform')");
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await SwipeAsync(page, cdp, (int)(box!.X + box.Width / 2), (int)(box.Y + box.Height / 2), 90, 0);
		await page.WaitForFunctionAsync("before => {const mark=document.querySelector('[data-ggnet-pan] g[transform] > circle');return mark && mark.parentElement.getAttribute('transform')!==before;}", before);
		var policy = await viewport.EvaluateAsync<string>("el => getComputedStyle(el).touchAction");
		var geometry = await viewport.EvaluateAsync<double[]>("el => {const b=el.viewBox.baseVal;const r=el.querySelector('rect.panel');return [b.x,b.y,b.width,b.height,...['x','y','width','height'].map(a=>Number(r.getAttribute(a)))];}");

		// Assert

		using var _ = new AssertionScope();
		policy.Should().Be("pan-y pinch-zoom");
		(await viewport.EvaluateAsync<string>("el => el.style.display")).Should().Be("block");
		(await viewport.EvaluateAsync<bool>("el => {const a=el.querySelector('rect.panel').getScreenCTM();const b=el.parentElement.getScreenCTM();return ['a','b','c','d','e','f'].every(k=>Math.abs(a[k]-b[k])<1e-9);}")).Should().BeTrue("consumer responsive SVG rules must preserve panel coordinates");
		geometry.Take(4).Should().Equal(geometry.Skip(4));
		(await mark.EvaluateAsync<string>("el => el.parentElement.getAttribute('transform')")).Should().NotBe(before);
		(await viewport.Locator("[data-ggnet-pan]").GetAttributeAsync("transform")).Should().BeNull();
		(await viewport.Locator(".x-break-label,.y-break-label,.x-strip,.y-strip").CountAsync()).Should().Be(0);
		(await page.Locator(".ggnet > svg").First.EvaluateAsync<string>("el => getComputedStyle(el).touchAction")).Should().Be("auto");
	}

	private async Task<IBrowserContext> ContextAsync(bool touch = false)
	{
		Assert.SkipUnless(fixture.Available, fixture.Reason);
		return await fixture.Browser.NewContextAsync(new() { ViewportSize = new() { Width = 900, Height = 700 }, HasTouch = touch, IsMobile = touch });
	}

	private static async Task<IPage> HarnessAsync(IBrowserContext context, bool panX = true, bool panY = true, bool tooltip = false)
	{
		var page = await context.NewPageAsync();
		await page.SetContentAsync("""
			<!doctype html>
			<meta name="viewport" content="width=device-width, initial-scale=1">
			<style>body{margin:0;min-width:2400px;min-height:2400px}#plot{width:720px;height:576px;display:block;margin-top:100px}</style>
			<svg id="plot" viewBox="0 0 720 576">
			<rect id="legend" x="630" y="0" width="90" height="576" fill="silver"/>
			<g id="capture"><svg id="viewport" x="100" y="100" width="500" height="400" viewBox="100 100 500 400" overflow="visible">
			<rect class="panel" x="100" y="100" width="500" height="400" fill="white"/>
			<g id="target"><circle id="mark" cx="300" cy="300" r="10"/></g></svg>
			<rect id="axis" x="100" y="520" width="500" height="40" fill="silver"/></g></svg>
			<div id="tip" popover="manual" style="position:fixed;inset:auto;margin:0;border:0;padding:0"><div class="bubble" style="width:60px;height:20px"></div></div>
			""");
		var source = await File.ReadAllTextAsync(Path.Combine(RepoRoot(), "src/GGNet/Components/Panel.razor.js"), TestContext.Current.CancellationToken);
		await page.EvaluateAsync("""
			async o => {
			    window.module=await import('data:text/javascript;base64,'+btoa(o.source));
			    window.calls=[];window.otherCalls=[];window.events=[];window.clicks=[];
			    document.addEventListener('click',e=>window.clicks.push(e.target.id));
			    for(const type of ['pointerdown','pointermove','pointerup','pointercancel','gotpointercapture','lostpointercapture'])document.addEventListener(type,e=>window.events.push({type,x:e.clientX,y:e.clientY,target:e.target.id,primary:e.isPrimary}),true);
			    window.options={pan:true,panX:o.panX,panY:o.panY,tooltip:o.tooltip};
			    document.querySelector('#viewport').style.display='block';
			    document.querySelector('#viewport').style.touchAction=o.panX?(o.panY?'pinch-zoom':'pan-y pinch-zoom'):(o.panY?'pan-x pinch-zoom':'auto');
			    window.frame=()=>document.querySelector('#mark').setAttribute('data-frame',String(window.calls.length));
			    window.holdFrames=false;
			    window.initialize=()=>window.module.initialize('panel',document.querySelector('#capture'),document.querySelector('#target'),{invokeMethodAsync:async(...a)=>{window.calls.push(a);if(a[0]==='OnPanEndAsync'&&!window.holdFrames)window.frame();}},window.options);
			    window.initialize();
			}
			""", new { source, panX, panY, tooltip });
		return page;
	}

	private static Task<string[]> ShowTooltipAsync(IPage page)
		=> page.EvaluateAsync<string[]>("() => { const tip=document.querySelector('#tip');window.module.showTooltip('panel',tip);return [tip.style.left,tip.style.top]; }");

	private static Task<bool> TooltipOpenAsync(IPage page)
		=> page.EvaluateAsync<bool>("() => document.querySelector('#tip').matches(':popover-open')");

	private static async Task PressDragReleaseAsync(IPage page, ICDPSession cdp, bool touch, int x, int y, int distance)
	{
		if (touch)
		{
			await TouchAsync(cdp, "touchStart", Point(x, y));
			if (distance != 0)
			{
				await TouchAsync(cdp, "touchMove", Point(x + distance, y));
			}
			await TouchAsync(cdp, "touchEnd");
		}
		else
		{
			await page.Mouse.MoveAsync(x, y);
			await page.Mouse.DownAsync();
			await page.Mouse.MoveAsync(x + distance, y);
			await page.Mouse.UpAsync();
		}

		// A tap's click is dispatched after the gesture recognizer's delay.
		await page.WaitForTimeoutAsync(400);
	}

	private static Task<JsonElement> CallsAsync(IPage page) => page.EvaluateAsync<JsonElement>("() => window.calls");

	private static Task<JsonElement?> FramesAsync(IPage page) => page.EvaluateAsync("() => new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)))");

	private static string RepoRoot([CallerFilePath] string path = "")
	{
		var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);
		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GGNet.slnx")))
		{
			directory = directory.Parent;
		}
		return directory?.FullName ?? throw new InvalidOperationException("GGNet.slnx not found above the test source");
	}
}
