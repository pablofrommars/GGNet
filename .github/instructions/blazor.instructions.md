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
├── CrosshairCursor.cs          # the crosshair of one panel: an internal, class-only component (§5)
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
- **Init accessors, with defaults**: `Width` (720), `Height` (576), `Theme` (`"default"`). Blazor can deliver new parameter values despite the C# init accessor. `Context`, `Width`, and `Height` changes request a refresh through the existing handler: `InteractiveAuto` composes inline, `Interactive` queues a frame, and `Static` keeps its initial frame. Dimension changes preserve the context's view windows. `RenderMode` is selected at initialization.
- **Unmatched attributes**: `[Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes` — forwarded onto the root element so callers can pass `class`, `data-*`, etc.
- **`RenderMode`** is the enum in `src/GGNet/RenderMode.cs` — exactly three values: `Interactive`, `InteractiveAuto`, `Static`. It is **not** the framework's `IComponentRenderMode`; do not confuse the two.
- **`ViewChanged`** is an `EventCallback<ViewChange<TX, TY>>`: one notification per committed change of the view window, carrying the cause (`Gesture`, `HostCommand`, `Reset`) and both windows in data units (§5). Because `Plot`'s type arguments are inferred, a host that binds it to a method names them explicitly (`<Plot T="…" TX="…" TY="…" ViewChanged=@OnViewChanged />`); the Razor compiler cannot convert a method group through an inferred `EventCallback<T>`.
- **`Inspection`** (`Inspection<TX>?`, bindable through `InspectionChanged`) is the crosshair's position: an x value in data units plus whether it is pinned. Null is no inspection. `InspectionChanged` fires for a gesture, a key or `ClearInspectionAsync`, never for a value the host set. **`ReadingsChanged`** carries `InspectionReadings<TX>` (the position, one `Reading<TX>` per inspected series, and whether the position lies outside the visible window) and fires only when that readout differs from the last one reported, so live data under an unmoved pin raises it and nothing else. Both need `Crosshair` (§5).
- **`Interactivity` is initialization-only.** A later unequal value, including switching to or from null, throws an actionable `GGNetUserException` before any parameters in that batch are applied. Equal-valued replacements and omitted parameters are accepted. Recreate the Plot (for example, change its Blazor `@key`) to use different options.

---

## 4. Rendering, State, and Disposal

GGNet does its own render orchestration through the handler; the component is a thin shell.

- **Never call `StateHasChanged` directly.** The only path is `Plot.StateHasChangedAsync() => InvokeAsync(StateHasChanged)`, invoked from the handler's background loop. Marshal through `InvokeAsync` because the render loop runs off the UI thread. Two small overlay components re-render themselves the same way, through `InvokeAsync(StateHasChanged)`, so that showing them does not re-render the marks: the tooltip and the crosshair cursor.
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
- **Panel ownership follows the component.** Panels are keyed by context and index so replacing a context recreates its components and interop. Facet rebuilds can replace panel data within a component; rebind against the actual data reference and unregister only the component that owns that registration. Sequential reuse after headless export is supported; simultaneous rendering of one mutable context in several plots is not an ownership guarantee.

---

## 5. Interactivity — a Server-Side Render Loop Plus Exactly One JS Module

The default is server-side. `InteractiveRenderModeHandler` runs a `System.Threading.Channels` background loop that coalesces `RenderTarget.Render` / `RenderTarget.Loading` signals and calls `StateHasChangedAsync()`. Tooltip content, hover, and refresh all live there.

**The library ships exactly one JS asset**: `src/GGNet/Components/Panel.razor.js` (collocated with `Panel.razor`, served from `./_content/GGNet/Components/Panel.razor.js`). It exists for **continuous gestures only** — wheel coordinates converted through the capture element's inverse screen CTM, drag-pan previewed as a transform on the marks group at frame rate, the cursor-glued tooltip positioned and edge-flipped at frame rate, and the crosshair kept under the pointer between two server frames. These are the cases that cannot cross the circuit per frame; a `mousemove` round trip per event is still never acceptable. `wwwroot/dev/pixelWidthCalculator.html` remains an offline dev tool, not shipped interop.

### The opt-in gate

`Plot.Interactivity` (`InteractivityOptions?`) is unset by default, and when unset there is **no capture group, no gesture handlers, and byte-identical static output** — which is why the goldens do not churn. Interop initialization is additionally gated on `coord.CarvesAxisBands`, so polar plots never wire it up.

### The wiring pattern — follow `PanelInterop`, do not invent a second one

`PanelInterop` (`Components/PanelInterop.cs`) is the model for any future module:

- `internal sealed class` owning the module lifecycle, **one instance per interactive panel**, with **no DI registration** — the component constructs it around the framework-provided `IJSRuntime`, so consumers configure nothing. **Exception:** tsu's per-concern module registered with `AddScoped` does *not* apply — one JS module without DI is GGNet's rule.
- Lazy import from `OnAfterRenderAsync(firstRender)`. Initialization owns an acquired module until it can publish it to the live wrapper. Disposal retires the wrapper without waiting on a pending import or initialization; its continuation releases late acquisitions instead of publishing them. Module-reference release and callback-reference release must still run when JS teardown throws.
- **Batching rule: one `initialize` call carries every feature flag** (`PanelInteropOptions`, serialized camelCase). Do not add a second round trip per feature.
- JS → .NET: `DotNetObjectReference.Create(this)` + `[JSInvokable]` methods (`OnWheelAsync`, `OnPanEndAsync`, `OnCursorAsync`, `OnCursorLeaveAsync`, `OnCursorRestoreAsync`, `OnNavigateEndAsync`), each named in a `[DynamicDependency(nameof(...))]` on the component so trimming preserves it. Dispose the reference alongside the module.
- **Every JS call catches** `JSDisconnectedException`, `ObjectDisposedException`, and `TaskCanceledException` — a disconnected circuit is normal, not exceptional.
- On the JS side, listeners are registered against an `AbortController` signal and torn down by the module's `dispose(id)`.

Pan uses one owning pointer and a transform captured from the stationary capture element, never the translated marks. A press becomes a pan only after travelling a screen-pixel threshold (mouse 4, pen 6, touch 10). Below it the press stays an ordinary tap or click: no capture, no preview, no commit, and its click reaches marks and ancestor handlers untouched. Pointer capture is taken at recognition, not at press, so the release is observed on the window; the click that trails a recognized drag is discarded before it reaches a mark or an ancestor. Additional contacts are ignored; cancellation, a lost or never-activated capture, disposal, a changed viewport transform, or a native pinch rescaling the visual viewport clears the preview without committing. Normal pointer release commits once, and the preview is held until the committed frame mutates the marks (or a timeout expires): clearing it at release shows the marks at their old position for the round trip. A fixed outer panel clip bounds the translated marks while the existing Area clip remains inside. The preview is linked across a plot's panels: the `initialize` payload names the plot (`group`) and whether its x and y scales are shared (`sharedX`, `sharedY`), and at recognition the dragged panel finds the module's other panels of that plot and translates their marks too, scaled by plot-area size, for each panned axis that is shared. Linked previews follow the dragged one's rules: cleared on any cancellation, held after release until that panel's own committed frame mutates its marks. A panel with its own scale on the panned axis (free facet scales, per-panel y scales) does not follow.

Tooltip coordinates are tracked in the capture phase from `pointerover`, `pointerdown` and `pointermove`: a stationary tap sends no `pointermove`. A touch `pointerdown` is not default-prevented, so the compatibility mouse events behind tap-to-hover (mark tooltips) still arrive on a pan-enabled panel; mouse and pen presses keep `preventDefault` against text selection. That also prevents focus, so under `Crosshair` the module focuses the plot itself on a mouse press: the crosshair's keys are read from the focused plot. A touch tooltip has no hover to end it, so a touch pan, a scroll or a visual-viewport change dismisses it client-side, and a later re-render does not resurrect it before the next press.

Under `CursorTooltip` or `Crosshair`, a touch tap replaces the browser's emulated hover inside the panel. The module picks the nearest mark within 24 screen pixels of the contact (rings of `elementsFromPoint` samples, so every mark shape hit-tests by its real geometry), then dispatches `mouseover`/`mouseout` to that mark. The tooltip and host `onmouseover`/`onmouseout` callbacks therefore describe one mark. A contact counts as inside the panel up to half a unit beyond the plot area, because a mark centred on its edge is half outside it. A tap with no mark in reach releases the mark; so do a tap outside the panel, a touch pan, a scrub and a scroll. Compatibility hover events from touch (`sourceCapabilities.firesTouchEvents`) are stopped before they reach the panel's handlers. Mouse and pen keep native hover and native click targeting, as do charts without `CursorTooltip` or `Crosshair`.

What the tap's click does depends on the crosshair. Under `CursorTooltip` without a shared crosshair, the click is delivered to the inspected mark: a native click landing elsewhere in the panel is stopped and replaced by one click on that mark, which bubbles to ancestors once, and a tap with no mark in reach keeps its native click. Under a shared crosshair the tap is inspection input: it pins the crosshair and is stopped there, reaching neither a mark's `onclick` nor an ancestor. A mouse click still reaches both.

Touch policy applies only to the panel viewport. Without a shared crosshair it follows the effective configured pan axes (including AutoFitY's Y restriction): the unused axis retains page scrolling and browser pinch remains native. Under a shared crosshair it is `pan-y pinch-zoom`, because one finger scrubs sideways and scrolls the page vertically, and it becomes `pan-y` only where two fingers navigate the chart (`Pan` with an x axis the gestures drive): Chromium hands the page two-finger input only where `touch-action` excludes pinch, so page pinch-zoom is given up over that plot area and nowhere else. A nested SVG viewport with `display: block` carries `touch-action`: Chromium ignores that policy on SVG groups, rectangles and an inline nested SVG. Matching inline geometry protects its identity mapping against ordinary responsive SVG styles. Labels and facet strips remain outside this viewport; there is no root-SVG scroll capture.

### The crosshair

`Crosshair` is one x position per plot, held by `Plot` as a data value and drawn in every panel that shares the x scale. A panel hosts an internal `CrosshairCursor` component, which draws the line, its x label (the scale's formatter) and a marker at each inspected sample, and re-renders alone. It is opened through the render-tree builder in `Panel.razor`, because Razor markup cannot name an internal component, and it wraps the cursor group in the clip: the module previews by translating that group, and a clip path on a translated element travels with it.

- **Shared or local.** Panels share the position when the plot is not flipped, is not faceted with `freeX` (whatever the number of facets) and has one x scale; `freeX` facets and flipped plots get a local, transient line in the panel under the pointer, with no pin and no readout. Polar panels have no interop at all. The `initialize` payload says which (`cursor`: `shared` or `local`) and whether the axis inverts exactly (`cursorContinuous`): on `LocalDateTime` and discrete x the server snaps and the module does not preview.
- **Latest-wins requests.** The module sends `OnCursorAsync(px, pinned)` with at most one call in flight per plot; a newer position replaces any that is waiting, and a pin or the end of a drag is always the last one sent. There is no round trip per move and no hover strip.
- **Preview and hold.** Between frames the module translates the cursor group to the pointer. After a pin, a restore or the end of a scrub it holds that preview until the committed line reaches it (or a timeout), so the line never falls back to its old position for the round trip. It writes only `transform` and `style` on the cursor group, attributes Blazor never sets there.
- **Mouse.** With no pin, the line follows the pointer and ends when the pointer leaves the plot area. A click pins. Once pinned, hover does not move it. With `Pan`, a drag pans and its trailing click is skipped.
- **Touch.** A tap pins. One finger moving sideways scrubs and release pins; a vertical start is the page's scroll. A second finger abandons the scrub and `OnCursorRestoreAsync` puts back the pinned value the plot remembers (restoring from pixels would cost the pin precision). Nothing new starts until every finger has lifted. A touch double tap does not reset the view; a mouse double click does.
- **Two fingers.** With `Pan` and an x axis the gestures drive, two fingers pan and pinch the x window. The gesture previews as `translate` plus a horizontal `scale` on the marks of every panel sharing x, and commits once through `OnNavigateEndAsync(from, to, scale)` when the first finger lifts: the value under the fingers' midpoint at the start ends under their midpoint at the end. The pinned line rides along. Under a shared crosshair one finger never pans.
- **Keys.** The plot's root element takes `tabindex="0"` under `Crosshair`. Left and Right step to the previous and next sample time across every line, point, area and bar layer, whether or not it has `inspect:`; Escape clears. Stepping exists where readings do, on `Instant` and numeric x.
- **Lifetime.** A pin ends only through Escape, `ClearInspectionAsync` or the host setting `Inspection` to null. It survives new data, resizing, view changes and a new `Context`. Outside the visible window its line is hidden, never clamped, and the readout says so.
- **Click handling stays on the window.** A click listener on the chart group makes Chromium's touch adjustment treat the chart as a target and move taps that land near the plot-area edge.

### Readings

A layer joins the readout with `inspect:` on `Geom_Line`, `Geom_Point`, `Geom_Area` or `Geom_Bar` (`Inspect<T>`: name, value text, `InspectPolicy`, a maximum distance, the missing text). The lookup lives in `src/GGNet/Readings/`: each geom builds one `SeriesIndex<T>` per colour, line-type or fill group (a group with no valid sample included, so that it reads as missing), sorted by x, once it has been asked for, and rebuilds it at the end of every render pass, so a reader never sees a half-built index. Lookups run on raw data coordinates (Unix milliseconds for `Instant`, the value for `double`), so a distance limit means data units on a log axis too. The one exception is `Containing`: a bar's extent is a drawn width, so it is measured in the scale's mapped space, as `Bar` draws it. Readings exist on `Instant` and numeric x only; an inspected layer on any other x axis throws `GGNetUserException` when a plot with `Crosshair` mounts or receives that context. Without `Crosshair`, `inspect:` changes nothing.

### The view window is observable; which view is open is the host's state

`PlotContext.XView` / `YView` read the current windows back in data units, lower bound first. They are null when no window is set, and null on discrete and `LocalDateTime` axes: both store a window as positions in a list of categories or per-minute samples, which has no exact data-unit bounds once panned. `Instant` reads back to the millisecond.

`Plot.ViewChanged` fires once per committed change and never during a drag preview, which lives in the browser until release. Every commit path ends in `Plot.CommitViewAsync(cause, …)`: the panel's wheel, pan-end and reset handlers pass `Gesture` or `Reset`, and the public `RefreshAsync` passes `HostCommand`, which is how `ZoomToXAsync`, `ZoomToYAsync`, `ResetViewAsync`, `ShowLastAsync` and a `SetXView` followed by a refresh are all reported without their own wiring. The plot compares the windows with the last ones it reported, so a command that leaves the window where it was raises nothing, an AutoFitY refit travels in the same notification as its x change, and a window set before the first render is the baseline, not a change. Route any new window mutation through `CommitViewAsync`; a path that calls the render-mode handler directly is silent.

GGNet does not switch between an overview and a detail chart, and holds no "which chart is open" state. A host composes separate `PlotContext`s, keeps the plots mounted, and carries the window across with `XView` and `ZoomToXAsync` under its own policy (`src/GGNet.Demo/Components/Pages/OverviewDetail.razor`).

### Panel activation

`.Panel(…, onActivate:, key:)` is the click a host uses to open something. It differs from `onClick`, which stays on the background rectangle: marks and grid lines are siblings of that rectangle, so a click on them never reaches it. When `onActivate` is set, the plot area (background, grid lines, marks) is wrapped in one `<g data-ggnet-activate>` that carries the handler; labels and strips stay outside. Without the callback the markup is unchanged. The payload is `PanelActivation(Index, Key)`, the index counting `.Panel(…)` calls.

Activation never fires for input GGNet has claimed:

- **No `Interactivity`:** nothing is claimed; a click activates at once.
- **`Interactivity` without `CursorTooltip` or `Crosshair`:** the click that trails a recognized drag is already discarded by the module. The reset gesture is a double click, which delivers two clicks first, so the handler waits 200 ms (a calibration value: 300 ms felt laggy in physical QA, and a double tap slower than the wait activates before it resets) and a second click or the `dblclick` cancels it.
- **`CursorTooltip` or `Crosshair`:** a tap inspects, so the activation group is not rendered at all. The host supplies its own control.

A mark's own `onclick` still fires, before the activation.

### Adding browser-dependent behaviour

Reaching for the browser is still a deliberate architectural decision, not a default. The tiers that landed are wheel-zoom, reset, the shared crosshair with its series readout, drag-pan, two-finger pan and pinch under the crosshair, cursor-glued tooltips, auto-fit y, the imperative view API, the observable view window, and panel activation. The open ones — legend toggle, the anchor-model/shared-series tooltip, rubber-band selection — each carry a design note and a trigger. Extend the existing module and its single `initialize` payload rather than adding a second module.

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

Component tests use **bUnit** (`tests/GGNet.Components.Tests/`, `BunitContext`), rendering the interactive `Plot` in-process (no browser). bUnit covers the circuit surface — tooltips, mouse events, gestures, the opt-in gate — but it cannot execute `Panel.razor.js`. **The only layer that runs the JS for real is the browser suite in `tests/GGNet.E2ETests`** (Playwright over the spawned demo app), and those tests self-skip unless `GGNET_E2E=1`, so a green default gate says nothing about the module. The process-output tests run without a browser. Touching the JS or its wrapper means running `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests` explicitly.

Headless SVG output and goldens are tested separately (rendering + testing guides). See [testing.instructions.md](./testing.instructions.md).
