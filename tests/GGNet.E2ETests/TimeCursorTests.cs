namespace GGNet.E2ETests;

// The crosshair in a real browser, on the mouse path: the gesture module keeps
// the line under the pointer, the circuit supplies its label, the readings and
// the sample markers, and one position is shared by every panel of the plot.
public partial class TimeCursorTests(DemoAppFixture fixture) : IClassFixture<DemoAppFixture>
{
	private static readonly string[] series = ["Temperature", "Target", "Ambient", "Gravity", "Pressure", "Cooling"];

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(3)]
	public async Task AHoverInAnyPanelDrawsOneLinePerPanelAtTheSameTime(int origin)
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", origin, 0.4);

		// Act

		await session.Page.Mouse.MoveAsync((float)x, (float)y);
		await session.WaitForTextAsync("pinned", "following");
		var cursors = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		cursors.Should().HaveCount(4).And.OnlyContain(cursor => cursor.Visible && !cursor.Pinned);
		cursors.Should().OnlyContain(cursor => Math.Abs(cursor.Fraction - 0.4) < 0.002);
		cursors.Should().OnlyContain(cursor => cursor.Top >= cursor.PanelTop - 1 && cursor.Bottom <= cursor.PanelBottom + 1);
		cursors.Select(cursor => cursor.Label).Distinct().Should().ContainSingle().Which.Should().MatchRegex("^[0-9]{2}:[0-9]{2}$");
		cursors.Should().OnlyContain(cursor => cursor.LabelWidth > 10, "the label is drawn, not only present");
		(await session.RowsAsync()).Select(row => row.Name).Should().Equal(series);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task AClickPinsAndTheHoverNoLongerMovesTheLine()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.3);
		var (hx, hy) = await session.PointAsync("cursor", 2, 0.7);

		// Act

		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForTextAsync("pinned", "pinned");
		var time = await session.TextAsync("cursor-time");
		var changes = await session.TextAsync("position-changes");
		await session.Page.Mouse.MoveAsync((float)hx, (float)hy);
		await session.Page.WaitForTimeoutAsync(400);
		await session.Page.Mouse.MoveAsync(2, 2);
		await session.Page.WaitForTimeoutAsync(400);
		var cursors = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		cursors.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.3) < 0.002);
		(await session.TextAsync("cursor-time")).Should().Be(time);
		(await session.TextAsync("position-changes")).Should().Be(changes);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task LeavingThePlotAreaEndsAnUnpinnedCursor()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 1, 0.5);
		await session.Page.Mouse.MoveAsync((float)x, (float)y);
		await session.WaitForTextAsync("pinned", "following");

		// Act

		await session.Page.Mouse.MoveAsync(2, 2);
		await session.Page.Locator("[data-qa=empty]").WaitForAsync(new() { Timeout = 10_000 });

		// Assert

		using var _ = new AssertionScope();
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => !cursor.Visible);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ABurstOfMovesSendsTheFirstAndTheLastPositionOnly()
	{
		// Arrange — sixty moves inside one browser task: no reply can arrive between them.

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.2);
		var panel = await session.PanelAsync("cursor", 0);

		// Act

		await session.Page.EvaluateAsync("""
			([x, y, width]) => {
			    const target = document.elementFromPoint(x, y);
			    for (let i = 0; i < 60; i++) {
			        target.dispatchEvent(new PointerEvent('pointermove', {
			            bubbles: true, composed: true, pointerType: 'mouse', isPrimary: true, buttons: 0,
			            clientX: x + width * 0.5 * i / 59, clientY: y
			        }));
			    }
			}
			""", new[] { x, y, panel.Width });
		var during = await session.CursorsAsync("cursor");
		await session.WaitForTextAsync("position-changes", "2");
		await session.Page.WaitForTimeoutAsync(400);
		var after = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		during.Should().OnlyContain(cursor => cursor.Visible && Math.Abs(cursor.Fraction - 0.7) < 0.002, "the line follows the pointer before any reply");
		after.Should().OnlyContain(cursor => cursor.Visible && Math.Abs(cursor.Fraction - 0.7) < 0.002);
		(await session.TextAsync("position-changes")).Should().Be("2");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task MovingAPinNeverShowsTheLineBackAtItsOldPosition()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.2);
		var (nx, ny) = await session.PointAsync("cursor", 0, 0.8);
		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.WaitForTimeoutAsync(300);
		await session.Page.EvaluateAsync("""
			() => {
			    const line = document.querySelector('[data-qa=cursor] [data-ggnet-cursor] line');
			    window.__trace = [];
			    window.__tracing = false;
			    const frame = () => {
			        if (window.__tracing) window.__trace.push(line.getBoundingClientRect().x);
			        requestAnimationFrame(frame);
			    };
			    document.addEventListener('click', () => { window.__tracing = true; window.__trace.push(line.getBoundingClientRect().x); });
			    requestAnimationFrame(frame);
			}
			""");

		// Act

		await session.Page.Mouse.ClickAsync((float)nx, (float)ny);
		await session.WaitForTextAsync("position-changes", "2");
		await session.Page.WaitForTimeoutAsync(500);
		var trace = await session.Page.EvaluateAsync<double[]>("() => window.__trace");

		// Assert

		using var _ = new AssertionScope();
		trace.Should().NotBeEmpty();
		trace.Should().OnlyContain(position => Math.Abs(position - nx) < 2, "the preview holds until the committed line reaches it");
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.002);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task WithPanAMouseDragPansAndDoesNotPin()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 1, 0.6);

		// Act

		await session.Page.Mouse.MoveAsync((float)x, (float)y);
		await session.Page.Mouse.DownAsync();
		await session.Page.Mouse.MoveAsync((float)x - 80, (float)y, new() { Steps = 8 });
		await session.Page.Mouse.MoveAsync((float)x - 160, (float)y, new() { Steps = 8 });
		await session.Page.Mouse.UpAsync();
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(500);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("view-changes")).Should().Be("1");
		(await session.TextAsync("window")).Should().NotBe("—");
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => !cursor.Pinned);
		(await session.Page.Locator("[data-qa=pinned]").AllInnerTextsAsync()).Should().NotContain("pinned");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ASampleMarkerSitsOnItsSampleWhileTheReadoutNamesTheDistance()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 1, 0.37);

		// Act

		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.WaitForTimeoutAsync(300);
		var rows = await session.RowsAsync();
		var marker = await session.Page.EvaluateAsync<double[]>("""
			() => {
			    const cursor = document.querySelectorAll('[data-qa=cursor] [data-ggnet-cursor]')[1];
			    const marker = cursor.parentElement.querySelector('circle.crosshair-marker').getBoundingClientRect();
			    const mx = marker.x + marker.width / 2, my = marker.y + marker.height / 2;
			    const line = cursor.querySelector('line').getBoundingClientRect().x;
			    const marks = [...cursor.closest('svg').querySelectorAll('[data-ggnet-pan] circle')]
			        .map(c => c.getBoundingClientRect())
			        .map(r => Math.hypot(r.x + r.width / 2 - mx, r.y + r.height / 2 - my));
			    return [mx, line, Math.min(...marks)];
			}
			""");

		// Assert

		var gravity = rows.Single(row => row.Name == "Gravity");

		using var _ = new AssertionScope();
		gravity.Kind.Should().Be("Previous");
		gravity.Note.Should().MatchRegex("^sample [0-9]+ (s|min) earlier$");
		marker[2].Should().BeLessThan(1.5, "the marker is drawn on a gravity sample");
		marker[0].Should().BeLessThan(marker[1] - 1, "and that sample lies before the cursor line");
		rows.Single(row => row.Name == "Target").Note.Should().StartWith("in effect since ");
		rows.Select(row => row.Kind).Should().NotContain("Interpolated");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ASeriesWithNoNearbySampleSaysSoInsteadOfShowingADistantValue()
	{
		// Arrange — the ambient probe stops reporting after nine of the twenty-four hours.

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.75);

		// Act

		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForTextAsync("pinned", "pinned");
		var rows = await session.RowsAsync();

		// Assert

		using var _ = new AssertionScope();
		rows.Single(row => row.Name == "Ambient").Should().BeEquivalentTo(new { Kind = "Missing", Value = "No nearby reading", Note = "" });
		rows.Single(row => row.Name == "Temperature").Kind.Should().Be("Observed");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task WithoutAReadoutTheLineAndItsLabelStillFollowAndPin()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("plain", 0, 0.5);

		// Act

		await session.Page.Mouse.MoveAsync((float)x, (float)y);
		await session.Page.WaitForTimeoutAsync(400);
		var hovering = await session.CursorsAsync("plain");
		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.Page.WaitForTimeoutAsync(400);
		await session.Page.Mouse.MoveAsync(2, 2);
		await session.Page.WaitForTimeoutAsync(400);
		var pinned = await session.CursorsAsync("plain");

		// Assert

		using var _ = new AssertionScope();
		hovering.Should().ContainSingle().Which.Should().Match<Cursor>(cursor => cursor.Visible && !cursor.Pinned && cursor.Label.Length > 0);
		pinned.Should().ContainSingle().Which.Should().Match<Cursor>(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.5) < 0.002);
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => !cursor.Visible, "another plot is never synchronized");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task OnADiscreteAxisTheLineSnapsToTheCategory()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (ax, ay) = await session.PointAsync("snap", 0, 0.34);
		var (bx, by) = await session.PointAsync("snap", 0, 0.44);

		// Act

		await session.Page.Mouse.MoveAsync((float)ax, (float)ay);
		await session.WaitForCursorAsync("snap", cursor => cursor.Visible);
		var first = (await session.CursorsAsync("snap"))[0];
		await session.Page.Mouse.MoveAsync((float)bx, (float)by);
		await session.Page.WaitForTimeoutAsync(500);
		var second = (await session.CursorsAsync("snap"))[0];

		// Assert

		using var _ = new AssertionScope();
		first.Label.Should().Be("Boil");
		second.Label.Should().Be("Boil");
		second.X.Should().BeApproximately(first.X, 0.5);
		first.X.Should().NotBeApproximately(ax, 5, "the line sits on the category, not under the pointer");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task WithIndependentXAxesTheLineStaysInItsPanelAndNeverPins()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("free", 1, 0.5);

		// Act

		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForCursorAsync("free", cursor => cursor.Visible);
		var over = await session.CursorsAsync("free");
		await session.Page.Mouse.MoveAsync(2, 2);
		await session.Page.WaitForTimeoutAsync(600);
		var away = await session.CursorsAsync("free");

		// Assert

		using var _ = new AssertionScope();
		over.Select(cursor => cursor.Visible).Should().Equal(false, true);
		over.Should().OnlyContain(cursor => !cursor.Pinned);
		away.Should().OnlyContain(cursor => !cursor.Visible);
		session.Errors.Should().BeEmpty();
	}

	// Playwright fills these from the page's objects, so they need settable members.
	private sealed class Cursor
	{
		public double X { get; set; }

		public double Fraction { get; set; }

		public double Top { get; set; }

		public double Bottom { get; set; }

		public double PanelTop { get; set; }

		public double PanelBottom { get; set; }

		public bool Visible { get; set; }

		public bool Pinned { get; set; }

		public string Label { get; set; } = string.Empty;

		public double LabelWidth { get; set; }
	}

	private sealed class Row
	{
		public string Name { get; set; } = string.Empty;

		public string Kind { get; set; } = string.Empty;

		public string Value { get; set; } = string.Empty;

		public string Note { get; set; } = string.Empty;
	}

	private async Task<Session> OpenAsync(string path = "/time-cursor")
	{
		Assert.SkipUnless(fixture.Available, fixture.Reason);

		// The QA device's shape: a 1200x1920 portrait touchscreen at device scale 1 on a desktop
		// browser, not a mobile one; 1185 CSS px is its measured width beside the scrollbar.
		var context = await fixture.Browser.NewContextAsync(new() { ViewportSize = new() { Width = 1185, Height = 1920 }, HasTouch = true });
		var page = await context.NewPageAsync();
		var session = new Session(context, page, await context.NewCDPSessionAsync(page));

		page.PageError += (_, error) => session.Errors.Add(error);
		page.Console += (_, message) =>
		{
			if (message.Type == "error")
			{
				session.Errors.Add(message.Text);
			}
		};

		await page.GotoAsync(fixture.BaseUrl + path, new() { WaitUntil = WaitUntilState.NetworkIdle });

		// Give the circuit a moment to become interactive after prerender.
		await page.WaitForTimeoutAsync(1000);

		return session;
	}

	private sealed record Session(IBrowserContext Context, IPage Page, ICDPSession Cdp) : IAsyncDisposable
	{
		public List<string> Errors { get; } = [];

		public async Task<LocatorBoundingBoxResult> PanelAsync(string chart, int panel)
		{
			var locator = Page.Locator($"[data-qa={chart}] rect.panel").Nth(panel);
			await locator.ScrollIntoViewIfNeededAsync();
			await Page.WaitForTimeoutAsync(200);

			return (await locator.BoundingBoxAsync())!;
		}

		// A point at a fraction of the panel's width, a third of the way down it.
		public async Task<(double x, double y)> PointAsync(string chart, int panel, double fraction)
		{
			var box = await PanelAsync(chart, panel);

			return (box.X + box.Width * fraction, box.Y + box.Height / 3);
		}

		// The centre of a mark in one panel of a chart.
		public async Task<(double x, double y)> MarkAsync(string chart, int panel, int index)
		{
			await PanelAsync(chart, panel);
			var point = await Page.EvaluateAsync<double[]>("""
				([chart, panel, index]) => {
				    const group = document.querySelectorAll(`[data-qa=${chart}] [data-ggnet-pan]`)[panel];
				    const box = group.querySelectorAll('circle')[index].getBoundingClientRect();
				    return [box.x + box.width / 2, box.y + box.height / 2];
				}
				""", new object[] { chart, panel, index });

			return (point[0], point[1]);
		}

		// The preview transform on each panel's marks; empty when none is applied.
		public Task<string[]> PreviewsAsync(string chart)
			=> Page.EvaluateAsync<string[]>("chart => [...document.querySelectorAll(`[data-qa=${chart}] [data-ggnet-pan]`)].map(marks => marks.getAttribute('transform') ?? '')", chart);

		// The x scale factor of a preview transform; 1 when there is none.
		public static double Scale(string preview)
		{
			var match = System.Text.RegularExpressions.Regex.Match(preview, @"scale\(([0-9.eE+-]+), 1\)");

			return match.Success ? double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 1;
		}

		// The length of the reported x window in minutes.
		public async Task<double> WindowMinutesAsync() => double.Parse(await TextAsync("window-minutes"), System.Globalization.CultureInfo.InvariantCulture);

		public Task<double> ScrollAsync() => Page.EvaluateAsync<double>("() => window.scrollY");

		public Task<string> TouchActionAsync(string chart)
			=> Page.EvaluateAsync<string>("chart => getComputedStyle(document.querySelector(`[data-qa=${chart}] svg[data-ggnet-viewport]`)).touchAction", chart);

		public async Task<Cursor[]> CursorsAsync(string chart)
			=> await Page.EvaluateAsync<Cursor[]>("""
				chart => [...document.querySelectorAll(`[data-qa=${chart}] [data-ggnet-cursor]`)].map(cursor => {
				    let owner = cursor.parentElement;
				    while (owner && !owner.querySelector(':scope > rect.panel')) owner = owner.parentElement;
				    const panel = owner.querySelector(':scope > rect.panel').getBoundingClientRect();
				    const element = cursor.querySelector('line');
				    const line = element.getBoundingClientRect();
				    return {
				        x: line.x,
				        fraction: (line.x - panel.x) / panel.width,
				        top: line.y,
				        bottom: line.bottom,
				        panelTop: panel.y,
				        panelBottom: panel.bottom,
				        visible: getComputedStyle(element).visibility === 'visible',
				        pinned: cursor.hasAttribute('data-ggnet-pinned'),
				        label: cursor.querySelector('text.crosshair-label').textContent.trim(),
				        labelWidth: cursor.querySelector('text.crosshair-label').getBoundingClientRect().width
				    };
				})
				""", chart);

		public async Task<Row[]> RowsAsync()
			=> await Page.EvaluateAsync<Row[]>("""
				() => [...document.querySelectorAll('[data-qa=readout] tr[data-qa-row]')].map(row => ({
				    name: row.getAttribute('data-qa-row'),
				    kind: row.getAttribute('data-qa-kind'),
				    value: row.querySelector('[data-qa-value]').textContent.trim(),
				    note: row.querySelector('[data-qa-note]').textContent.trim()
				}))
				""");

		public async Task<string> TextAsync(string name)
		{
			var locator = Page.Locator($"[data-qa={name}]");

			return await locator.CountAsync() == 0 ? string.Empty : await locator.InnerTextAsync();
		}

		// Assertions report the final state; these only give the circuit time to reach it.
		public async Task WaitForTextAsync(string name, string text)
		{
			for (var i = 0; i < 50 && await TextAsync(name) != text; i++)
			{
				await Task.Delay(100);
			}
		}

		public async Task WaitForCursorAsync(string chart, Func<Cursor, bool> condition)
		{
			for (var i = 0; i < 50 && !(await CursorsAsync(chart)).Any(condition); i++)
			{
				await Task.Delay(100);
			}
		}

		public ValueTask DisposeAsync() => Context.DisposeAsync();
	}
}
