---
applyTo: "**/*.razor,**/*.razor.cs,**/*.razor.css,**/*.razor.js"
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
├── Plot.razor + Plot.razor.cs  # the hostable plot (720×576 default); ComponentBase, IPlot, IPlotRendering, IAsyncDisposable
├── Area.razor + Area.razor.cs  # the "dumb walker" over composed ScreenPrimitives
├── Panel / Tooltip / ...       # supporting components
├── Panel.razor.js              # the library's only shipped JS asset — continuous gestures (§5)
├── PanelInterop.cs             # typed wrapper owning that module's lifecycle (§5)
├── SvgFormat.cs                # the invariant-culture numeric choke point (see rendering guide)
└── _Imports.razor              # two lines: the Rendering + Web namespaces
```

- `Plot<T, TX, TY>` is the only hostable component: it owns the parameters, the render-mode handler and the dispose guard directly. There is deliberately no shared plot base class — the `PlotBase`/`SparkLine` pair was removed once `SparkLine` went.
- Rendering is delegated to an `IRenderModeHandler` (`src/GGNet/Rendering/`), selected by the `RenderMode` parameter. Components hold no render loop of their own.

---

## 2. Three-File Split

Every non-trivial component: `<Name>.razor` (markup) + `<Name>.razor.cs` (`public partial class`) + rarely a `<Name>.razor.css`. `Panel` additionally has a collocated `<Name>.razor.js` — the one exception, and one that should stay singular (§5).

- **Scoped `.razor.css` is essentially unused by design.** `plot.razor.css` exists but is a near-empty placeholder — **all paint lives in `Themes/Default.css`**, driven by CSS variables (§5). Do not add scoped component CSS to style output; add a `--ggnet-*` variable and paint it in the theme instead.
- The code-behind is a `public partial class <Name><T, TX, TY>` declaring its own base and interfaces (`Plot` takes `ComponentBase, IPlot, IPlotRendering, IAsyncDisposable`); the `.razor` carries no `@inherits`.

---

## 3. Parameters

Grounded in `Plot.razor.cs`:

- **Required**: `[Parameter] public required PlotContext<T, TX, TY> Context` and `[Parameter] public required RenderMode RenderMode`.
- **Init-only, with defaults**: `Width` (720), `Height` (576), `Theme` (`"default"`). Use `{ get; init; }` for parameters that are set once and not two-way bound.
- **Unmatched attributes**: `[Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes` — forwarded onto the root element so callers can pass `class`, `data-*`, etc.
- **`RenderMode`** is the enum in `src/GGNet/RenderMode.cs` — exactly three values: `Interactive`, `InteractiveAuto`, `Static`. It is **not** the framework's `IComponentRenderMode`; do not confuse the two.

---

## 4. Rendering, State, and Disposal

GGNet does its own render orchestration through the handler; the component is a thin shell.

- **Never call `StateHasChanged` directly.** The only path is `Plot.StateHasChangedAsync() => InvokeAsync(StateHasChanged)`, invoked from the handler's background loop. Marshal through `InvokeAsync` because the render loop runs off the UI thread.
- **`ShouldRender` / `OnAfterRender` delegate to the `IRenderModeHandler`.** Don't add ad-hoc render gating in a component — extend the handler.
- **Idempotent async dispose.** `Plot` implements `IAsyncDisposable` with the interlocked guard, and the handler repeats it:
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

## 5. Interactivity — a Server-Side Render Loop Plus Exactly One JS Module

The default is server-side. `InteractiveRenderModeHandler` runs a `System.Threading.Channels` background loop that coalesces `RenderTarget.Render` / `RenderTarget.Loading` signals and calls `StateHasChangedAsync()`. Tooltip content, hover, and refresh all live there.

**The library ships exactly one JS asset**: `src/GGNet/Components/Panel.razor.js` (collocated with `Panel.razor`, served from `./_content/GGNet/Components/Panel.razor.js`). It landed with the interactivity tiers (2026-07-10) and exists for **continuous gestures only** — the wheel converted to plot units against the rendered size, drag-pan previewed as a transform on the marks group at frame rate, and the cursor-glued tooltip positioned and edge-flipped at frame rate. These are the cases that cannot cross the circuit per frame; a `mousemove` round trip per event is still never acceptable. `wwwroot/dev/pixelWidthCalculator.html` remains an offline dev tool, not shipped interop.

### The opt-in gate

`Plot.Interactivity` (`InteractivityOptions?`) is unset by default, and when unset there is **no capture group, no gesture handlers, and byte-identical static output** — which is why the goldens do not churn. Interop initialization is additionally gated on `coord.CarvesAxisBands`, so polar plots never wire it up.

### The wiring pattern — follow `PanelInterop`, do not invent a second one

`PanelInterop` (`Components/PanelInterop.cs`) is the model for any future module:

- `internal sealed class` owning the module lifecycle, **one instance per interactive panel**, with **no DI registration** — the component constructs it around the framework-provided `IJSRuntime`, so consumers configure nothing. (This is where the per-concern-module-registered-`AddScoped` pattern from other codebases does *not* apply.)
- Lazy import: `module ??= await runtime.InvokeAsync<IJSObjectReference>("import", ModulePath)`, from `OnAfterRenderAsync(firstRender)`.
- **Batching rule: one `initialize` call carries every feature flag** (`PanelInteropOptions`, serialized camelCase). Do not add a second round trip per feature.
- JS → .NET: `DotNetObjectReference.Create(this)` + `[JSInvokable]` methods, each named in a `[DynamicDependency(nameof(...))]` on the component so trimming preserves it. Dispose the reference alongside the module.
- **Every JS call catches** `JSDisconnectedException`, `ObjectDisposedException`, and `TaskCanceledException` — a disconnected circuit is normal, not exceptional.
- On the JS side, listeners are registered against an `AbortController` signal and torn down by the module's `dispose(id)`.

### Adding browser-dependent behaviour

Reaching for the browser is still a deliberate architectural decision tracked in `ROADMAP.md`, not a default. The tiers that landed are wheel-zoom, reset, crosshair + readout, drag-pan, cursor-glued tooltips, auto-fit y, and the imperative view API. The open ones — legend toggle, the anchor-model/shared-series tooltip, rubber-band selection — each carry a design note and a trigger. Extend the existing module and its single `initialize` payload rather than adding a second module.

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

Component tests use **bUnit** (`tests/GGNet.Components.Tests/`, `BunitContext`), rendering the interactive `Plot` in-process (no browser). bUnit covers the circuit surface — tooltips, mouse events, gestures, the opt-in gate — but it cannot execute `Panel.razor.js`. **The only layer that runs the JS for real is `tests/GGNet.E2ETests`** (Playwright over the spawned demo app), and every test there self-skips unless `GGNET_E2E=1`, so a green default gate says nothing about the module. Touching the JS or its wrapper means running `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests` explicitly.

Headless SVG output and goldens are tested separately (rendering + testing guides). See [testing.instructions.md](./testing.instructions.md).
