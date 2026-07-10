---
applyTo: "**/*.razor,**/*.razor.cs,**/*.razor.css"
---

# Blazor Component Guide

Scope: GGNet's Razor components under `src/GGNet/Components/`. Assumes [csharp.instructions.md](./csharp.instructions.md) applies to all C#.

GGNet is a **Razor class library** (`Microsoft.NET.Sdk.Razor`), not an app. There is no `Program.cs`, no `App.razor`/`Routes.razor`, no `ServiceDefaults`, no routing, no Tailwind. The components render **SVG**, and the deep rules for SVG emission, invariant-culture numerics, and the byte-pinned goldens live in [rendering.instructions.md](./rendering.instructions.md) — read it alongside this file when you touch anything that produces markup.

Only rules that matter for *this* library live here. For async, error handling, naming, and formatting, defer to the C# guide.

---

## 1. Component Shape

The public surface is small and generic over the data type and the two axis types: `<T, TX, TY>`.

```
src/GGNet/Components/
├── PlotBase.cs                 # base: ComponentBase, IPlot, IPlotRendering, IAsyncDisposable
├── Plot.razor + Plot.razor.cs  # the hostable plot (720×576 default)
├── SparkLine.razor + .razor.cs # inline 150×50 variant, its own PlotBase subclass
├── Area.razor + Area.razor.cs  # the "dumb walker" over composed ScreenPrimitives
├── Panel / Tooltip / ...       # supporting components
├── SvgFormat.cs                # the invariant-culture numeric choke point (see rendering guide)
└── _Imports.razor              # two lines: the Rendering + Web namespaces
```

- `Plot<T, TX, TY>` and `SparkLine<T, TX, TY>` both derive from `PlotBase<T, TX, TY>`. `SparkLine` is **not** a parameter of `Plot` — it is a separate component.
- Rendering is delegated to an `IRenderModeHandler` (`src/GGNet/Rendering/`), selected by the `RenderMode` parameter. Components hold no render loop of their own.

---

## 2. Three-File Split

Every non-trivial component: `<Name>.razor` (markup) + `<Name>.razor.cs` (`public partial class`) + rarely a `<Name>.razor.css`.

- **Scoped `.razor.css` is essentially unused by design.** `plot.razor.css` exists but is a near-empty placeholder — **all paint lives in `Themes/Default.css`**, driven by CSS variables (§5). Do not add scoped component CSS to style output; add a `--ggnet-*` variable and paint it in the theme instead.
- The code-behind is a `public partial class <Name><T, TX, TY> : PlotBase<T, TX, TY>` (or the relevant base).

---

## 3. Parameters

Grounded in `PlotBase.cs` and `Plot.razor.cs`:

- **Required, on the base**: `[Parameter] public required PlotContext<T, TX, TY> Context` and `[Parameter] public required RenderMode RenderMode`.
- **Init-only, with defaults, on `Plot`**: `Width` (720), `Height` (576), `Theme` (`"default"`). Use `{ get; init; }` for parameters that are set once and not two-way bound.
- **Unmatched attributes**: `[Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes` — forwarded onto the root element so callers can pass `class`, `data-*`, etc.
- **`RenderMode`** is the enum in `src/GGNet/RenderMode.cs` — exactly three values: `Interactive`, `InteractiveAuto`, `Static`. It is **not** the framework's `IComponentRenderMode`; do not confuse the two.

---

## 4. Rendering, State, and Disposal

GGNet does its own render orchestration through the handler; the component is a thin shell.

- **Never call `StateHasChanged` directly.** The only path is `PlotBase.StateHasChangedAsync() => InvokeAsync(StateHasChanged)`, invoked from the handler's background loop. Marshal through `InvokeAsync` because the render loop runs off the UI thread.
- **`ShouldRender` / `OnAfterRender` delegate to the `IRenderModeHandler`.** Don't add ad-hoc render gating in a component — extend the handler.
- **Idempotent async dispose.** `PlotBase` implements `IAsyncDisposable` with the interlocked guard, and the handler repeats it:
  ```csharp
  private int disposing = 0;

  public async ValueTask DisposeAsync()
  {
      if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
      {
          return;
      }

      // dispose the render-mode handler (cancels its CTS, awaits the background task)
  }
  ```
- **Cache `RenderFragment` fields in the constructor** rather than allocating a new fragment per render (see `Plot.razor.cs`, `Area.razor.cs`).

---

## 5. No JavaScript — Interactivity Is a Server-Side Render Loop

**This library ships zero JavaScript.** There is no `IJSRuntime`, no `IJSObjectReference`, no `[JSInvokable]`, no `JSDisconnectedException`, no `.js`/`.ts` asset (the lone `wwwroot/dev/pixelWidthCalculator.html` is an offline dev tool, not shipped interop).

Interactivity — tooltips, hover, refresh — is driven entirely server-side: `InteractiveRenderModeHandler` runs a `System.Threading.Channels` background loop that coalesces `RenderTarget.Render` / `RenderTarget.Loading` signals and calls `StateHasChangedAsync()`. This is the deliberate Blazor-Server design.

- **Do not introduce JS interop as a matter of course.** If a feature genuinely needs the browser (e.g. `mousemove`-driven pan/zoom), that is a deliberate architectural decision tracked in `ROADMAP.md` (interactivity tiers), not a default reached for to implement a handler. The tsu-style "JS-interop module per concern" pattern does **not** apply here.

---

## 6. Exhaustive Switch in Render Helpers

Render helpers walk closed unions of shapes/elements and **must** be exhaustive — a missing arm is a build error (`CS8509` is elevated to error in `.editorconfig`). Write the `switch` with a `_ =>` only when a genuine default is intended; otherwise let the compiler force every case:

```razor
@switch (primitive)
{
    case ScreenCircle circle:  /* emit <circle> */  break;
    case ScreenLine line:      /* emit <line>   */  break;
    // every ScreenPrimitive variant — omission fails the build
}
```

The `Area` component is the canonical walker: it is a "dumb walker" over the `ScreenPrimitive`s produced by `ShapeComposer` (see the rendering guide). Keep projection/geometry out of components — it belongs in `src/GGNet/Scene/`.

---

## 7. Theming — "If It Moves Layout It's C#, If It's Paint It's CSS"

One rule splits styling:

- **Layout is C#** — font sizes, margins, positions — because the server measures text to lay the plot out. It lives in the `Style` object (`src/GGNet/Style*.cs`), reachable via `.Style()` on the DSL chain.
- **Paint is CSS** — every color/background/stroke is read through a `--ggnet-*` CSS variable defined in `src/GGNet/Themes/Default.css` and overridable per theme.

Rules:

- A theme is a **block of variable overrides**, not a stylesheet fork: `.ggnet[theme=name] { --ggnet-bg: …; }`. Themes ship as embedded resources (`GGNet.Themes.<name>.css`); the `Theme` parameter selects one.
- **Paint against the stable semantic classes** (`panel`, `x-break`, `legend-title`, `spinner`, …) — never against generated ids or positions.
- Every emitted class must be painted and every referenced variable must be defined — `ThemeContractTests` (`tests/GGNet.Headless.Tests/ThemeContractTests.cs`) enforces this contract, and a theme file may set **only** variables the base declares. If you add a paintable class or a `var(--ggnet-*)` reference, update `Themes/Default.css` in the same change or the contract test fails.
- Geom parameters accept CSS custom properties: `color: "var(--color-temperature)"` wires a layer to the host app's design tokens.

---

## 8. Component Testing

Component tests use **bUnit** (`tests/GGNet.Components.Tests/`, `BunitContext`), rendering the interactive `Plot` in-process (no browser). Headless SVG output and goldens are tested separately (rendering + testing guides). See [testing.instructions.md](./testing.instructions.md).
