using System.Text.Json;

namespace GGNet.E2ETests.Fixtures;

// Real touch input through the DevTools protocol: these events run the browser's
// own gesture recognizer and touch-action handling, which synthetic DOM events skip.
internal static class TouchInput
{
	public static object Point(double x, double y, int id = 1) => new { x, y, id };

	public static Task<JsonElement?> TouchAsync(ICDPSession cdp, string type, params object[] points)
		=> cdp.SendAsync("Input.dispatchTouchEvent", new() { ["type"] = type, ["touchPoints"] = points });

	public static async Task TapAsync(IPage page, ICDPSession cdp, double x, double y, double jitter = 0)
	{
		await TouchAsync(cdp, "touchStart", Point(x, y));
		if (jitter != 0)
		{
			await TouchAsync(cdp, "touchMove", Point(x + jitter, y));
		}
		await TouchAsync(cdp, "touchEnd");

		// A tap's click is dispatched after the gesture recognizer's delay.
		await page.WaitForTimeoutAsync(400);
	}

	public static async Task SwipeAsync(IPage page, ICDPSession cdp, double x, double y, double dx, double dy)
	{
		await TouchAsync(cdp, "touchStart", Point(x, y));
		for (var i = 1; i <= 10; i++)
		{
			await TouchAsync(cdp, "touchMove", Point(x + i * dx / 10, y + i * dy / 10));
			await page.WaitForTimeoutAsync(30);
		}
		await TouchAsync(cdp, "touchEnd");
		await page.WaitForTimeoutAsync(200);
	}
}
