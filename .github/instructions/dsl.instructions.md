---
applyTo: "src/GGNet/**/*.cs"
---

# DSL / Grammar-of-Graphics Guide

Scope: GGNet's public fluent surface — the grammar of graphics — implemented in `src/GGNet`. Assumes [csharp.instructions.md](./csharp.instructions.md) applies. This is the library's crown jewel: the conventions here are also read by the MCP server ([mcp.instructions.md](./mcp.instructions.md)) and the packaged skill ([skill.instructions.md](./skill.instructions.md)), so drift is expensive.

---

## 1. The Grammar — One Fluent Chain

A plot is a single chain, terminated by `.Style()`:

```csharp
PlotContext.Build(source, x, y)   // data + default selectors
    .Geom_*(...)                  // layers, configured in place (repeatable)
    .Scale_*(...)                 // axes, legends, transforms
    .Facet_*(...) / .Coord_Polar() / .Flip()
    .Title("...") .XLab("...")    // labels (Markdown)
    .Style();                     // terminal call → IPlotContext
```

How it is wired (know this before editing the surface):

- **`PlotContext`** is a non-generic `public partial class` holding the static `Build<…>` entry points; the generic `public partial class PlotContext<T, TX, TY> : IPlotContext` holds instance state. `Build` picks default scales by dispatching on the axis types `TX`/`TY` (including NodaTime `LocalDate`/`LocalDateTime`/`Instant` overloads).
- **`Geom_*`, `Scale_*`, `Facet_*`, `Style`** are **static extension methods** in `public static partial class BuilderExtensions`, split across **22 partial files** — `BuilderExtensions.<Geom>.cs` (one per geom) plus the base `BuilderExtensions.cs` (holds `Scale_*` / `Style`).
- `Geom_*` extends `PanelFactory<…>`, constructs the internal geom, and registers it via the internal `panel.AddTyped(() => new <Geom>(...))`. `Scale_*` / `Style` extend `PlotContext<T, TX, TY>` and **return it** for chaining.

---

## 2. The Four DSL Conventions (non-negotiable — they define the surface)

1. **`xxxBy` means data-driven; the unsuffixed twin is a per-layer constant.** `colorBy`/`fillBy`/`sizeBy`/`lineTypeBy` take an aesthetic *mapping* (built by `Scale_Color_Discrete`, `Scale_Fill_Continuous`, …): computed per item, trains a scale, feeds the legend. `color`/`fill`/`size`/`lineType` are constants painting the whole layer. Setting both is meaningful (the mapping wins for its own aesthetic; the constant still colors other aesthetics' legend swatches) — not an error.
2. **Positional arguments stop at the selectors.** Source and selector params (`x`, `y`, `ymin`, `open`, …) may be positional; **every aesthetic, event, or option after them is passed by name.** The signatures are intentionally wide — all configuration in one call — and named arguments keep call sites readable and stable.
3. **The vocabulary is SVG's.** `strokeWidth`, `opacity`, `fillOpacity`, `strokeOpacity`, `strokeColor` mean exactly what they mean in SVG. `width`/`height` are reserved for geometric extent in **data units** (`Geom_Bar`, `Geom_Tile`, `Geom_Violin`).
4. **Interactivity is a uniform block.** Every data-mark geom takes `onclick`, `onmouseover`, `onmouseout`, and (where hover makes sense) `tooltip`. Annotation geoms (`Geom_ABLine`/`HLine`/`VLine`/`Text`) and statistical summaries (`Geom_Boxplot`/`Violin`/`RidgeLine`) deliberately take **no** event block.

---

## 3. Stats Are Sources, Not Layers

`Stat.*` calls return a typed source (`public readonly record struct` — `Bin`, `DensityPoint`, `Count<TKey>`, `Summary`) that **any geom draws unchanged**, recomputed every render pass so streaming data stays current. There is no `Histogram` geom — a histogram is `Stat.Bin` + `Geom_Bar`.

- Grouped variants add a `groupBy:` parameter and prepend `Group` to the output; **per-facet statistics are grouped statistics** — compute with `groupBy:` and facet the output on the same key (the key is stated twice by design; a mismatch is almost certainly a bug).

---

## 4. The Overload-Partial Discipline (this is where regressions hide)

The wide per-geom signatures are **hand-copied across overloads** — source generation of these families was tried and **retired by decision** in favor of verification (`ROADMAP.md`). The guarantee is a test battery, so consistency is on you when you touch a family:

- **`OverloadConsistencyTests`** (`tests/GGNet.Headless.Tests/OverloadConsistencyTests.cs`) reflects over `BuilderExtensions`/`PlotContext`/`Stat` and asserts four invariants per name-family:
  1. **Defaults agree** — the same parameter name has an identical default value across every overload.
  2. **Parameter shapes agree** — parameter types agree within a `(name, receiver)` sub-family (generics erased; `source`/`palette`/`polygons` excluded as legitimate dispatch variance).
  3. **Sugar overloads preserve canonical order** — a shorter overload keeps the parameter order of its longest sibling.
  4. **Docs agree** — each `<param>`'s XML-doc text is identical across the family (loaded from `GGNet.xml`).
- **`BuilderForwardingTests`** guards that overloads forward to the canonical implementation correctly (a positional-forwarding bug shipped for years before the gates caught it — `ROADMAP.md`).

**When you add or edit a geom overload:** keep parameter **names, defaults, order, and `<param>` docs** consistent across the entire family, or these tests fail. Every public method on this surface must carry full `///` docs (§C# guide).

---

## 5. Adding a Geom (the checklist)

1. Add `src/GGNet/Geoms/<Geom>/<Geom>.cs` — `internal sealed class <Geom><T, TX, TY> : Geom<T, TX, TY>` (guards throw `GGNetUserException` for missing required selectors).
2. Add `src/GGNet/BuilderExtensions.<Geom>.cs` — the `Geom_<Geom>` extension family on `PanelFactory`, registering via `AddTyped`, with full `///` docs and family-consistent signatures (§4).
3. Update the skill: a `skills/ggnet/examples/<chart>.md` and the relevant `skills/ggnet/reference/*.md` (signatures extracted from source — [skill.instructions.md](./skill.instructions.md)).
4. Add a pinned gallery golden (`tests/GGNet.Headless.Tests/Gallery/GalleryTests.<Name>.verified.svg`) — [rendering.instructions.md](./rendering.instructions.md).
5. The MCP `list_geoms` tool reflects the new method automatically — no manual list to update ([mcp.instructions.md](./mcp.instructions.md)).

---

## 6. Guard at the Surface

DSL misuse is caught early with `GGNetUserException` and a clear message — null required selectors (`BuilderExtensions.<Geom>.cs`), incompatible combinations (`Flip()` + polar in `PlotContext`), uninferrable types (`"Type could not be inferred"`), unsupported coordinate systems. Put the guard at the top of the `Build`/`Geom_*`/`Style` entry point; never let bad DSL input reach the render pipeline as a `NullReferenceException`.
