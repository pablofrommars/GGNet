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

	[Theory]
	[InlineData(true, false, "translate(15, 0)")]
	[InlineData(false, true, "translate(0, 1)")]
	[InlineData(true, true, "translate(15, 1)")]
	[InlineData(false, false, null)]
	public async Task ADragPreviewsOnEveryPanelOfThePlotSharingThePannedAxis(bool sharedX, bool sharedY, string? expected)
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);
		await LinkAsync(page, sharedX, sharedY);

		// Act

		await MouseDragAsync(page, 250, 400, 30, 40);
		var dragged = await page.Locator("#target").GetAttributeAsync("transform");
		var sibling = await page.Locator("#sibling-target").GetAttributeAsync("transform");
		var stranger = await page.Locator("#stranger-target").GetAttributeAsync("transform");
		await CancelMouseAsync(page);

		// Assert

		using var _ = new AssertionScope();
		dragged.Should().Be("translate(30, 40)");
		sibling.Should().Be(expected);
		stranger.Should().BeNull();
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
		(await page.Locator("#sibling-target").GetAttributeAsync("transform")).Should().BeNull();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
	}

	[Fact]
	public async Task ASiblingPreviewIsHeldAfterReleaseUntilItsOwnCommittedFrame()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);
		await LinkAsync(page, sharedX: true, sharedY: false);
		await page.EvaluateAsync("() => { window.holdFrames = true; }");

		// Act

		await MouseDragAsync(page, 250, 400, 30, 0);
		await page.Mouse.UpAsync();
		var draggedHeld = await page.Locator("#target").GetAttributeAsync("transform");
		var siblingHeld = await page.Locator("#sibling-target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => window.frame()");
		await page.WaitForFunctionAsync("() => !document.querySelector('#target').hasAttribute('transform')");
		var siblingAfterOtherFrame = await page.Locator("#sibling-target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => window.stamp('#sibling', '#sibling-frame')");
		await page.WaitForFunctionAsync("() => !document.querySelector('#sibling-target').hasAttribute('transform')");

		// Assert

		using var _ = new AssertionScope();
		draggedHeld.Should().Be("translate(30, 0)");
		siblingHeld.Should().Be("translate(15, 0)");
		siblingAfterOtherFrame.Should().Be("translate(15, 0)");
		(await CallsAsync(page)).GetArrayLength().Should().Be(1);
	}

	[Fact]
	public async Task OwnerCancellationAndDisposalClearActivePreviewsWithoutCommitting()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context);

		// Act

		await MouseDragAsync(page, 250, 400, 30, 0);
		var beforeCancel = await page.Locator("#target").GetAttributeAsync("transform");
		await CancelMouseAsync(page);
		var cancelled = await page.Locator("#target").GetAttributeAsync("transform");
		await MouseDragAsync(page, 250, 400, 30, 0);
		var beforeDispose = await page.Locator("#target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => { window.module.dispose('panel');window.module.dispose('panel'); }");
		await page.Mouse.UpAsync();
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
	public async Task RecognizedDragCommitsOnceAndSuppressesTheTrailingClick(bool pen, int distance)
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await PressDragReleaseAsync(page, cdp, pen, 300, 400, distance);
		var afterDrag = await page.EvaluateAsync<string[]>("() => window.clicks");
		await PressDragReleaseAsync(page, cdp, pen, 300, 400, 0);
		var calls = await CallsAsync(page);

		// Assert

		using var _ = new AssertionScope();
		calls.GetArrayLength().Should().Be(1);
		calls[0][1].GetDouble().Should().Be(distance);
		afterDrag.Should().BeEmpty("the click that trails a drag reaches neither the mark nor an ancestor");
		(await page.EvaluateAsync<string[]>("() => window.clicks")).Should().Equal(["mark"], "the next ordinary press still clicks");
	}

	[Fact]
	public async Task TheSecondOfTwoQuickPansWaitsForItsOwnFrame()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);
		await page.EvaluateAsync("() => { window.holdFrames=true; }");

		// Act

		await PressDragReleaseAsync(page, cdp, false, 300, 400, 40);
		await PressDragReleaseAsync(page, cdp, false, 300, 400, 25);
		await page.EvaluateAsync("() => window.frame()");
		await FramesAsync(page);
		var afterFirstFrame = await page.Locator("#target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => window.frame()");
		await FramesAsync(page);

		// Assert

		using var _ = new AssertionScope();
		afterFirstFrame.Should().Be("translate(25, 0)", "the first pan's frame still lacks the second pan");
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
		(await CallsAsync(page)).GetArrayLength().Should().Be(2);
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
		await page.EvaluateAsync("() => window.dataFrame()");
		await page.WaitForTimeoutAsync(2000);
		var waiting = await page.Locator("#target").GetAttributeAsync("transform");
		await page.WaitForTimeoutAsync(8500);

		// Assert

		using var _ = new AssertionScope();
		released.Should().Be("translate(40, 0)", "clearing at release would flash the marks at their old position");
		committed.Should().BeNull();
		waiting.Should().Be("translate(40, 0)", "neither elapsed time short of the recovery bound nor a frame that only brings data is the commit's frame");
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull("a frame that never arrives must not strand the preview");
	}

	// The server renders the commit's frame before it answers the call whenever the host's view
	// callback is slow, and always when it renders inline. The frame names the commit it rendered.
	[Theory]
	[InlineData("own")]
	[InlineData("foreign")]
	public async Task AFrameRenderedBeforeTheAnswerSettlesThePreviewOnlyWhenItIsTheCommitsOwn(string frame)
	{
		// Arrange — the commit advances the plot to epoch 2; a foreign frame carries epoch 1 and
		// the token of an earlier commit.

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);
		await page.EvaluateAsync("""
			own => {
			    window.render = (epoch, token) => { window.epoch = epoch - 1; document.querySelector('#frame').setAttribute('data-ggnet-commit', token); window.frame(); };
			    window.panEnd = (dx, dy, token) => { window.token = token; window.render(own ? 2 : 1, own ? token : token - 1); return new Promise(resolve => window.answer = () => resolve(2)); };
			}
			""", frame == "own");

		// Act

		await PressDragReleaseAsync(page, cdp, false, 300, 400, 40);
		var unanswered = await page.Locator("#target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => window.answer()");
		await FramesAsync(page);
		var answered = await page.Locator("#target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => window.render(2, window.token)");
		await FramesAsync(page);

		// Assert

		using var _ = new AssertionScope();
		unanswered.Should().Be(frame == "own" ? null : "translate(40, 0)", "the commit's own frame already shows the panned marks, and any other frame still shows the old ones");
		answered.Should().Be(frame == "own" ? null : "translate(40, 0)", "a frame below the answered epoch is not the commit's");
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
	}

	[Fact]
	public async Task AFrameBelowTheAnsweredEpochDoesNotSettleThePreview()
	{
		// Arrange — the host advanced the epoch unseen, so the commit answers 2 while the next
		// frame, which only brings data, carries 1.

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);
		await page.EvaluateAsync("() => { window.holdFrames = true; window.committed = 1; }");

		// Act

		await PressDragReleaseAsync(page, cdp, false, 300, 400, 40);
		await page.EvaluateAsync("() => window.frame()");
		await FramesAsync(page);
		var below = await page.Locator("#target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => window.frame()");
		await FramesAsync(page);

		// Assert

		using var _ = new AssertionScope();
		below.Should().Be("translate(40, 0)");
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
	}

	[Fact]
	public async Task APinnedLineStaysOnItsDataWhenReadingsArriveAfterThePanAnswer()
	{
		// Arrange — the pin sits on value 50 of a 0 to 100 window, 500 units wide from x 100. The
		// readings widen the window to 110, and the cursor is rendered ahead of the marks.

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false);
		var cdp = await context.NewCDPSessionAsync(page);
		await page.EvaluateAsync("""
			() => {
			    document.querySelector('#viewport').insertAdjacentHTML('beforeend', '<g id="cursor" data-ggnet-cursor data-ggnet-pinned data-ggnet-view="plot 0 0 0 100"><line id="cursor-line" x1="350" x2="350" y1="100" y2="500" stroke="black"/></g>');
			    window.options = { ...window.options, group: 'plot', cursor: 'shared', cursorContinuous: true };
			    window.initialize();
			    window.holdFrames = true;
			    window.line = () => Number(document.querySelector('#cursor-line').getAttribute('x1')) + (document.querySelector('#cursor').transform.baseVal.consolidate()?.matrix.e ?? 0);
			    window.cursorFrame = (view, x) => { document.querySelector('#cursor').setAttribute('data-ggnet-view', view); document.querySelector('#cursor-line').setAttribute('x1', x); };
			    window.marksFrame = (frame, view) => { document.querySelector('#capture').setAttribute('data-ggnet-frame', frame); const marks = document.querySelector('#frame'); marks.setAttribute('data-ggnet-view', view); marks.setAttribute('data-ggnet-marks-frame', frame); };
			}
			""");

		// Act

		await PressDragReleaseAsync(page, cdp, false, 300, 400, 40);
		var released = await page.EvaluateAsync<double>("() => window.line()");
		await page.EvaluateAsync("() => window.cursorFrame('plot 0 0 0 110', 100 + 50 / 110 * 500)");
		await FramesAsync(page);
		var cursorAhead = await page.EvaluateAsync<double>("() => window.line()");
		await page.EvaluateAsync("() => window.marksFrame('d1', 'plot 0 0 0 110')");
		await FramesAsync(page);
		var telemetry = await page.EvaluateAsync<double>("() => window.line()");
		var held = await page.Locator("#target").GetAttributeAsync("transform");
		await page.EvaluateAsync("() => { window.cursorFrame('plot 1 0 -8.8 101.2', 100 + 58.8 / 110 * 500); window.marksFrame('c1', 'plot 1 0 -8.8 101.2'); }");
		await FramesAsync(page);
		var committed = await page.EvaluateAsync<double>("() => window.line()");

		// Assert

		using var _ = new AssertionScope();
		released.Should().BeApproximately(390, 0.01);
		cursorAhead.Should().BeApproximately(390, 0.01, "the marks still show the old window, where the value sits at 350");
		telemetry.Should().BeApproximately(100 + 50.0 / 110 * 500 + 40, 0.01, "both are now drawn in the widened window");
		held.Should().Be("translate(40, 0)");
		committed.Should().BeApproximately(100 + 58.8 / 110 * 500, 0.01);
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
	}

	// No touch policy, so no touch coordinator: the module writes the root's feedback itself.
	[Theory]
	[InlineData("frame")]
	[InlineData("zero-answer")]
	[InlineData("disposal")]
	public async Task WithoutTouchAMousePanMarksThePlotRootUntilItsPreviewEnds(string end)
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false);
		await page.EvaluateAsync("zero => { document.querySelector('#plot').classList.add('ggnet'); window.initialize(); window.holdFrames = true; if (zero) window.committed = -1; }", end == "zero-answer");
		const string Feedback = "() => { const root = document.querySelector('#plot'); return `${root.getAttribute('data-ggnet-interaction') ?? 'none'} ${root.hasAttribute('data-ggnet-navigation') ? 'navigation' : 'none'}`; }";

		// Act

		await MouseDragAsync(page, 300, 400, 40, 0);
		var dragging = await page.EvaluateAsync<string>(Feedback);
		await page.Mouse.UpAsync();
		await FramesAsync(page);
		var released = await page.EvaluateAsync<string>(Feedback);
		await page.EvaluateAsync("end => { if (end === 'frame') window.frame(); if (end === 'disposal') window.module.dispose('panel'); }", end);
		await FramesAsync(page);
		var ended = await page.EvaluateAsync<string>(Feedback);

		// Assert

		using var _ = new AssertionScope();
		dragging.Should().Be("navigating navigation");
		released.Should().Be(end == "zero-answer" ? "none none" : "pending navigation", "a commit that answers with no epoch has no frame to wait for");
		ended.Should().Be("none none");
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
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
	public async Task ATouchPanDismissesTheTooltip()
	{
		// Arrange — no server stands behind the harness, so only the module can close the tooltip.

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, true, false, true, touchInspect: false);
		var cdp = await context.NewCDPSessionAsync(page);
		await ShowTooltipAsync(page);
		var shown = await TooltipOpenAsync(page);

		// Act

		await SwipeAsync(page, cdp, 300, 400, 80, 0);

		// Assert

		using var _ = new AssertionScope();
		shown.Should().BeTrue();
		(await TooltipOpenAsync(page)).Should().BeFalse();
		(await CallsAsync(page)).GetArrayLength().Should().Be(1, "the swipe was a touch pan");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ARejectedTouchCallMakesTheChartUnavailableAtOnce(bool inspect)
	{
		// Arrange — a rejected call is an explicit failure. A dropped circuit rejects nothing and is
		// noticed only at the ten-second bound, which the three-second wait below cannot reach.

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, true, false, touchInspect: inspect);
		var cdp = await context.NewCDPSessionAsync(page);
		await page.EvaluateAsync("() => { window.rejectTouch = true; }");

		// Act — a tap inspects, a swipe navigates.

		if (inspect)
		{
			await TapAsync(page, cdp, 300, 400);
		}
		else
		{
			await SwipeAsync(page, cdp, 300, 400, 80, 0);
		}
		await page.WaitForFunctionAsync("() => document.querySelector('#plot').dataset.ggnetInteraction === 'unavailable'", null, new() { Timeout = 3_000 });
		var rejected = await CallsAsync(page);
		await TouchAsync(cdp, "touchStart", Point(300, 400));
		await TouchAsync(cdp, "touchMove", Point(330, 400));
		await TouchAsync(cdp, "touchMove", Point(360, 400));
		var refused = await page.Locator("#target").GetAttributeAsync("transform");
		await TouchAsync(cdp, "touchEnd");
		await TapAsync(page, cdp, 300, 400);

		// Assert

		using var _ = new AssertionScope();
		rejected.GetArrayLength().Should().Be(1);
		rejected[0][0].GetString().Should().Be(inspect ? "OnTouchInspectAsync" : "OnTouchNavigateAsync");
		refused.Should().BeNull("an unavailable chart previews no further gesture");
		(await page.Locator("#target").GetAttributeAsync("transform")).Should().BeNull();
		(await page.Locator("[data-ggnet-provisional]").CountAsync()).Should().Be(0);
		(await page.Locator("#plot").GetAttributeAsync("data-ggnet-interaction")).Should().Be("unavailable");
		(await CallsAsync(page)).GetArrayLength().Should().Be(1, "an unavailable chart sends no further command");
	}

	[Fact]
	public async Task AMouseDragKeepsTheTooltip()
	{
		// Arrange

		await using var context = await ContextAsync();
		var page = await HarnessAsync(context, true, false, true);
		var cdp = await context.NewCDPSessionAsync(page);
		await page.Mouse.MoveAsync(300, 400);
		await ShowTooltipAsync(page);

		// Act

		await PressDragReleaseAsync(page, cdp, false, 300, 400, 40);

		// Assert

		(await TooltipOpenAsync(page)).Should().BeTrue();
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
	public async Task WithoutATouchPolicyATouchDragNeverPans(bool panX, bool panY, int dx, int dy)
	{
		// Arrange — mouse and pen keep the panel's pan handler; touch belongs to the touch policy, which this payload omits.

		await using var context = await ContextAsync(true);
		var page = await HarnessAsync(context, panX, panY);
		var cdp = await context.NewCDPSessionAsync(page);

		// Act

		await SwipeAsync(page, cdp, 300, 400, dx, dy);
		var preview = await page.Locator("#target").GetAttributeAsync("transform");

		// Assert

		using var _ = new AssertionScope();
		(await CallsAsync(page)).GetArrayLength().Should().Be(0);
		preview.Should().BeNull();
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
		policy.Should().Be("none");
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

	// touchInspect: null leaves the payload without a touch policy; otherwise the plot owns touch
	// with one-finger pan and pinch on x, and inspects by touch when true.
	private static async Task<IPage> HarnessAsync(IBrowserContext context, bool panX = true, bool panY = true, bool tooltip = false, bool? touchInspect = null)
	{
		var page = await context.NewPageAsync();
		await page.SetContentAsync("""
			<!doctype html>
			<meta name="viewport" content="width=device-width, initial-scale=1">
			<style>body{margin:0;min-width:2400px;min-height:2400px}#plot{width:720px;height:576px;display:block;margin-top:100px}</style>
			<svg id="plot" viewBox="0 0 720 576">
			<rect id="legend" x="630" y="0" width="90" height="576" fill="silver"/>
			<g id="capture" data-ggnet-frame="f0"><svg id="viewport" x="100" y="100" width="500" height="400" viewBox="100 100 500 400" overflow="visible">
			<rect class="panel" x="100" y="100" width="500" height="400" fill="white"/>
			<g id="target"><g id="frame" data-ggnet-marks-frame="f0" data-ggnet-view="plot 0 0 0 100"><circle id="mark" cx="300" cy="300" r="10"/></g></g></svg>
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
			    window.rejectTouch=false;
			    if(o.touch!==null){
			        window.options={...window.options,group:'plot',touch:{navigation:2,inspect:o.touch,ownership:0}};
			        document.querySelector('#plot').classList.add('ggnet');
			        document.querySelector('#viewport').style.touchAction='none';
			    }
			    window.epoch=0;window.committed=0;
			    window.stamp=(panel,marks)=>{const f='f'+window.epoch;document.querySelector(panel).setAttribute('data-ggnet-frame',f);const m=document.querySelector(marks);m.setAttribute('data-ggnet-view',`plot ${window.epoch} 0 0 100`);m.setAttribute('data-ggnet-marks-frame',f);};
			    window.frame=()=>{window.epoch++;window.stamp('#capture','#frame');};
			    window.dataFrame=()=>document.querySelector('#mark').setAttribute('data-frame',String(Date.now()));
			    window.holdFrames=false;
			    window.initialize=()=>window.module.initialize('panel',document.querySelector('#capture'),document.querySelector('#target'),{invokeMethodAsync:async(...a)=>{window.calls.push(a);if(window.rejectTouch&&a[0].startsWith('OnTouch'))throw new Error('rejected');if(a[0]!=='OnPanEndAsync')return;if(window.panEnd)return window.panEnd(...a.slice(1));const committed=++window.committed;if(!window.holdFrames)window.frame();return committed;}},window.options);
			    window.initialize();
			}
			""", new { source, panX, panY, tooltip, touch = touchInspect });
		return page;
	}

	// A second panel of the same plot at half the width and a fortieth of the height, and a
	// panel of another plot; every panel is told which axes its plot shares.
	private static async Task LinkAsync(IPage page, bool sharedX, bool sharedY)
		=> await page.EvaluateAsync("""
			o => {
			    const plot=document.querySelector('#plot');
			    plot.insertAdjacentHTML('beforeend','<g id="sibling" data-ggnet-frame="f0"><rect class="panel" x="100" y="505" width="250" height="10" fill="white"/><g id="sibling-target"><g id="sibling-frame" data-ggnet-marks-frame="f0" data-ggnet-view="plot 0 0 0 100"><circle id="sibling-mark" cx="200" cy="510" r="3"/></g></g></g><g id="stranger"><rect class="panel" x="360" y="505" width="240" height="10" fill="white"/><g id="stranger-target"><circle cx="400" cy="510" r="3"/></g></g>');
			    const ref={invokeMethodAsync:async(...a)=>{window.otherCalls.push(a);}};
			    const shared={sharedX:o.sharedX,sharedY:o.sharedY};
			    window.options={...window.options,...shared,group:'plot'};
			    window.initialize();
			    window.module.initialize('sibling',document.querySelector('#sibling'),document.querySelector('#sibling-target'),ref,{...window.options});
			    window.module.initialize('stranger',document.querySelector('#stranger'),document.querySelector('#stranger-target'),ref,{...window.options,group:'other'});
			}
			""", new { sharedX, sharedY });

	private static Task<string[]> ShowTooltipAsync(IPage page)
		=> page.EvaluateAsync<string[]>("() => { const tip=document.querySelector('#tip');window.module.showTooltip('panel',tip);return [tip.style.left,tip.style.top]; }");

	private static Task<bool> TooltipOpenAsync(IPage page)
		=> page.EvaluateAsync<bool>("() => document.querySelector('#tip').matches(':popover-open')");

	// Presses and drags without releasing, so the preview can be read mid-gesture.
	private static async Task MouseDragAsync(IPage page, int x, int y, int dx, int dy)
	{
		await page.Mouse.MoveAsync(x, y);
		await page.Mouse.DownAsync();
		await page.Mouse.MoveAsync(x + dx, y + dy, new() { Steps = 3 });
	}

	// Injected: real mouse input cannot raise pointercancel. Chromium's mouse is pointer 1.
	private static async Task CancelMouseAsync(IPage page)
	{
		await page.EvaluateAsync("() => window.dispatchEvent(new PointerEvent('pointercancel', { pointerId: 1, pointerType: 'mouse', isPrimary: true }))");
		await page.Mouse.UpAsync();
	}

	private static async Task PressDragReleaseAsync(IPage page, ICDPSession cdp, bool pen, int x, int y, int distance)
	{
		if (pen)
		{
			await cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mousePressed", ["x"] = x, ["y"] = y, ["button"] = "left", ["buttons"] = 1, ["clickCount"] = 1, ["pointerType"] = "pen" });
			await cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mouseMoved", ["x"] = x + distance, ["y"] = y, ["buttons"] = 1, ["pointerType"] = "pen" });
			await cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mouseReleased", ["x"] = x + distance, ["y"] = y, ["button"] = "left", ["buttons"] = 0, ["clickCount"] = 1, ["pointerType"] = "pen" });
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
