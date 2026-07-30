---
applyTo: "src/GGNet/Rendering/**,src/GGNet/Scene/**,src/GGNet/Shapes/**,src/GGNet/Buffers/**,src/GGNet/Components/SvgFormat.cs,src/GGNet.Headless/**"
---

# Rendering & Goldens Guide

Scope: the SVG rendering pipeline, the invariant-culture serialization boundary, headless export, and the byte-pinned gallery goldens. Assumes [csharp.instructions.md](./csharp.instructions.md) applies. This is where GGNet's correctness is *pinned* — small changes here ripple into 40+ snapshot files, so read this before touching anything that emits markup.

---

## 1. Two-Stage Pipeline: Compose in C#, Emit in Razor

1. **Compose** — `Scene/ShapeComposer.Compose` walks the geom layers and projects them from data space to screen space, producing `ScreenPrimitive`s. **All projection and annotation-label geometry lives here.** It uses a pooled `StringBuilder` (`Microsoft.Extensions.ObjectPool`) to build path `d` strings without per-shape allocation.
2. **Emit** — the `Area` component (`Components/Area.razor`) is a **dumb walker** over the composed `ScreenPrimitive`s: an exhaustive `switch` that maps each variant to an SVG element. It contains no geometry.

Keep the split: never compute coordinates in a component, never emit SVG from the composer. `Shapes/` holds data-space `internal readonly record struct` shapes (`Circle`, `Line`, `Path`, `Polygon`, `MultiPolygon`, …) implementing `IShape`; `Scene/` holds their screen-space `ScreenPrimitive` counterparts. `Buffers/SortedBuffer<T>` is a binary-search insertion-sorted list — a data-structure helper, **not** an SVG writer.

Both `Shape` (data) and `ScreenPrimitive` (screen) are closed **`union`** types — the walker's `switch` has no discard arm, so adding a primitive is a compile error until every walker handles it (`CS8509` = error; see the C# guide §6).

---

## 2. The Invariant-Culture Choke Point

Culture-sensitive numeric formatting produces broken SVG on comma-decimal locales, and `CA1305` is elevated to **error**. Every numeric SVG attribute flows through a single helper, `Components/SvgFormat.cs`:

```csharp
public static string Num(double value)             => value.ToString(CultureInfo.InvariantCulture);
public static string Num(int value)                => value.ToString(CultureInfo.InvariantCulture);
public static string Attr(FormattableString value) => FormattableString.Invariant(value);
```

**Rules:**

- In `.razor` markup, every numeric attribute goes through `@Num(...)` or `@Attr($"...")` — e.g. `transform=@Attr($"translate({circle.X}, {circle.Y})")`, `viewBox=@Attr(...)`. Never interpolate a raw `double`/`int` into markup; it would format under the ambient culture.
- In C# that emits SVG text directly, use `sb.Append(CultureInfo.InvariantCulture, $"…")`, `value.ToString(CultureInfo.InvariantCulture)`, or NodaTime patterns built with `CreateWithInvariantCulture`.
- **Geometry is always invariant.** Only *label text* may be localized, and only through an explicit `IFormatter<T>` passed via a `formatter:` parameter (§5) — this is the layout/paint numeric boundary.

---

## 3. Headless Export (`GGNet.Headless`)

`IPlotContextExtensions` exposes the pure-SVG export surface:

- `AsStringAsync(selfContained = false)` — renders to a `StringWriter`. When `selfContained` is `true`, it injects `<style>…</style>` (from `ThemeCss.SelfContained(theme)`) right after the opening `<svg>` tag so the file renders standalone; off by default because app-hosted output is styled by the app's stylesheet.
- `SaveAsync(path, selfContained = false)` — writes `AsStringAsync` output to a file.

`ThemeCss.SelfContained` loads the embedded theme, re-roots `.ggnet` selectors onto `svg` (`css.Replace(".ggnet", "svg")`), and throws `GGNetUserException` for an unbundled theme.

Rendering without a browser: `Host` builds an empty `ServiceCollection` and creates a fresh `HeadlessRenderer` **per render** (Blazor renderers are single-threaded). `SVGRenderer` walks the `RenderTreeFrame`s directly, finds the `<svg>` element, strips the Blazor scoped-CSS `b-…` marker attributes (regex), and serializes **only** the svg subtree (the surrounding div + loading indicator belong to the interactive experience). It carries `[SuppressMessage("...", "BL0006")]` because it touches RenderTree types; the self-closing element set is `line, circle, rect, path, stop`.

---

## 4. Goldens — Byte-Pinned SVG via Verify.XunitV3

Render-touching output is pinned byte-for-byte. Wiring in `tests/GGNet.Headless.Tests/`:

- **`VerifyConfig.cs`** (`[ModuleInitializer]`): a scrubber replaces nondeterministic plot ids `gg…` → `ggID` via a regex with a negative lookahead that preserves `--ggnet-*` variables. `Verifier.DerivePathInfo` routes `PublicApiTests` snapshots to `Api/` and everything else to `Gallery/`.
- **`GalleryTests.VerifyPlot`**: `var svg = await plot.AsStringAsync(); XDocument.Parse(svg); await Verifier.Verify(svg, extension: "svg");`. `XDocument.Parse` additionally asserts the output is well-formed pure SVG (not an HTML fragment). One `[Fact]` per geom/scenario, one snapshot each — `Gallery/GalleryTests.<Name>.verified.svg` (~40 files).
- **`PublicApiTests`** pins the exported-type manifest: `assembly.GetExportedTypes()` ordered, verified as `.txt` (`Api/PublicApiTests.GGNet.verified.txt`, `…Headless.verified.txt`). This is the deliberate public-surface lock (the 74-type cut in `ROADMAP.md`).

**Re-pinning is a deliberate, eyeballed decision — never a reflex.** A failing `Verify` writes `*.received.*`; accept it into `*.verified.*` only after eyeballing the diff (`ROADMAP.md` "Operating conventions"). CI surfaces received files via `actions/upload-artifact` on failure. If a change alters the SVG, expect gallery churn and confirm each diff is intended.

---

## 5. Culture Pinning

The invariance guarantee is tested, not assumed:

- **`LocaleTests`** — `GeometryIsCultureInvariantAcrossAllCultures` renders under invariant, then iterates **every** `CultureInfo.GetCultures(CultureTypes.AllCultures)`, asserting each normalized SVG equals the invariant output, matches no `\d,\d` (comma decimal), and contains no `−` (U+2212 minus). `LocalizedTicksAreAnExplicitOptIn` renders under `sv-SE` with a `formatter:` and asserts label *text* localizes while every *attribute* stays invariant — the crisp statement of the geometry/label boundary. `PolarGeometryIsCultureInvariant` covers `Coord_Polar`.
- **CI `test-culture` job** runs the whole suite under `LANG`/`LC_ALL = sv_SE.UTF-8` to keep the guarantee honest against a comma-decimal ambient culture.

When you add a formatter, a scale, or SVG-emitting code, add or extend the locale assertions — an invariance regression that only shows under an exotic culture is exactly what these tests exist to catch.
