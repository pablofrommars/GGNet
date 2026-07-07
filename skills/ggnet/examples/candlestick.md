# Candlestick chart

- Chart: `finance (no selector leaf)`
- Pinned SVG: [`GalleryTests.Candlestick.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Candlestick.verified.svg)
- When: OHLC price series; GGNet also ships `Geom_OHLC` and `Geom_Volume` (both pinned).

```csharp
PlotContext.Build(candles, i => i.T, i => i.Close)
	.Geom_Candlestick(i => i.T, i => i.Open, i => i.High, i => i.Low, i => i.Close).Style()
```
Source: `Candle(double T, double Open, double High, double Low, double Close, double Volume)`.
