---
applyTo: "**/*.cs"
---

# C# Coding Style Guide

Scope: every `.cs` file in the solution. GGNet is a single-author, gate-enforced **charting library** (ggplot2-style grammar of graphics, SVG output, .NET 11 / Blazor). These are its real conventions, extracted from the code and `.editorconfig` — not a generic .NET house style.

> This library does **not** follow the ziggy-brew parent conventions. There is no `Result<TError>` domain tier, no NodaTime-exclusive mandate, no TimescaleDB/MQTT anything. Ignore the parent `CLAUDE.md`; this repo stands alone.

---

## 1. `.editorconfig` Is the Enforced Gate — Don't Restate It, Understand It

`.editorconfig` at the repo root is the machine-enforced style contract, and **every suppression in it carries a one-line rationale**. Do not duplicate its severities in code review prose or fight them — read the rationale and follow the idiom. The load-bearing, deliberately-non-default rules and why:

| Rule | Setting | Why |
|---|---|---|
| `CA1707` | **off** | The fluent surface is `Geom_Point`, `Scale_X_Continuous`, `Coord_Polar` — underscores are the ggplot2-style API idiom, not a naming defect |
| `CA1051` | **off** | Visible instance fields on components/shapes are a deliberate hot-path idiom (§7) |
| `CS8509` | **error** | A missing arm in a `switch` over a closed `union` must fail the build (§6) |
| `CA1305` | **error** | Culture-sensitive formatting produces broken SVG on comma-decimal locales — formatting must be invariant (§9) |
| `CS1591` / `CS1573` | **off** | XML docs are additive: the public builder surface is documented, everything else stays undocumented without warnings (§8) |
| `IDE0130` + `dotnet_style_namespace_match_folder` | **warning / true** | Namespace must match folder — with one documented exception for `Stats/` (§3) |

The local gates every change must pass — build, test, and both `dotnet format` checks (also in CI, `ROADMAP.md` "Operating conventions"):

```
dotnet build GGNet.slnx -warnaserror
dotnet test GGNet.slnx
dotnet format whitespace GGNet.slnx --verify-no-changes
dotnet format style GGNet.slnx --verify-no-changes
```

After every `.cs` edit, check diagnostics (LSP or a build) before moving on — `-warnaserror` means a warning is a failure, and `CA1305`/`CS8509` violations surface fastest here.

---

## 2. Formatting

- **Tabs** for indentation (`tab_width = 4`), Allman braces, trailing whitespace trimmed, final newline.
- **Always block braces** on every control-flow statement — no brace-less single-line bodies.
- **File-scoped namespaces**, always.
- **Prefer primary constructors** — they capture parameters directly and remove the need for `this.`. Where a classic constructor is genuinely required (it stores derived or validated state), `this.field = field` disambiguation is the one sanctioned `this.` use; otherwise no `this.` prefix. **Private fields are `camelCase` with no `_` prefix** (`source`, `transformation`).
- Blank line between members; grouped `using`s separated by a blank line.

---

## 3. Namespaces & Global Usings

- Root namespace is **`GGNet`** (file-scoped). Sub-namespaces mirror folders: `GGNet.Exceptions`, `GGNet.Geoms`, `GGNet.Geoms.Point`, `GGNet.Scales`, `GGNet.Shapes`, `GGNet.Elements`, `GGNet.Coords`, `GGNet.Data`, `GGNet.Formats`, `GGNet.Scene`.
- **Match-folder is enforced** (`IDE0130` = warning). The one documented exception: files under `src/GGNet/Stats/` declare the root `namespace GGNet;` on purpose — `Stat.Bin` is DSL surface and sits next to `PlotContext.Build`. That exception is encoded in `.editorconfig`; don't add others without a rationale comment there.
- **One `GlobalUsings.cs` per project** (5 under `src/`, plus one per test project). The core project's globally imports `System.Globalization`, `System.Collections.Frozen`, `System.Text`, `Microsoft.Extensions.ObjectPool`, the `Microsoft.AspNetCore.Components.*` set, and `NodaTime` + `NodaTime.Text`. **Global usings are preferred** — add a broadly-used namespace here. Per-file `using` directives do appear as a local convenience (sub-namespace imports like `using Geoms.Line;` / `using Scales;`, `using static Position;`, plus alias usings for genuine conflicts such as `Moq`'s `Match` vs `System.Text.RegularExpressions.Match`); when a per-file import is used widely, promote it to `GlobalUsings.cs`.

---

## 4. Project Configuration — Shared Properties, No Central Package Management

**`Directory.Build.props` at the repo root carries shared *properties*. There is still no `Directory.Packages.props`, no CPM** — every `PackageReference` keeps its `Version` inline in the consuming `.csproj`. To add a package, add a `<PackageReference Include="X" Version="Y" />` to that project directly.

These are set once in `Directory.Build.props` and inherited by every project — **do not restate them in a `.csproj`**:

```xml
<TargetFramework>net11.0</TargetFramework>
<LangVersion>preview</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<AnalysisMode>Recommended</AnalysisMode>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<IsPackable>false</IsPackable>
```

It also holds the NuGet metadata shared by the two packable projects (`Version`, `Authors`, `Title`, `Copyright`, `Description`, `PackageTags`, `PackageLicenseExpression`) — so the shipped version is stated **once**. A `.csproj` declares only what is genuinely its own: `IsPackable=true` and its distinct `PackageDescription` on `GGNet`/`GGNet.Headless`, `OutputType`, `IsTestProject`, project-specific `NoWarn`.

`LangVersion=preview` is **required**, not optional — the codebase uses the preview `union` keyword (§6). The SDK is pinned in `global.json` (`11.0.100-preview.*`).

---

## 5. Type Idioms

`internal` is the **default** visibility; `public` is reserved for the deliberate DSL surface (`PlotContext`, `BuilderExtensions`, `Stat`, the DSL enums, `IFormatter<T>` and formatters, the `Stat.*` record structs). Sample patterns, all real:

- **Geoms** — `internal sealed class Point<T, TX, TY> : Geom<T, TX, TY>`, over an `internal abstract class Geom<T, TX, TY>`. Constraints `where TX : struct where TY : struct` are pervasive.
- **Shapes** (`Shapes/`) — `internal readonly record struct Circle : IShape`, with `{ get; init; }` properties and `required` on mandatory members.
- **Elements** (`Elements/`) — `internal readonly record struct` with an explicit parameterless ctor supplying init defaults (`public Rectangle() {}` + `Fill { get; init; } = "inherit"`).
- **Data carriers** (`Data/`) — `internal sealed class` with mutable `{ get; set; }` (e.g. `Selectors<T, TX, TY>`, `Aesthetics<T>`); some per-geom carriers are `internal sealed record`.
- **Scales** — `internal abstract class` hierarchies (`Scale<TKey, TValue>` → `Continuous<TKey>` → concrete), each overriding `public abstract Guide Guide`.
- **Stats** — `public readonly record struct` with positional params (`public readonly record struct Bin(double Min, double Mid, double Max, int Count, double Density)`), plus a `Bin<TKey>` generic variant.

Defaults: `sealed` on classes/records unless designed for inheritance; **primary constructors** for geoms, scales, exceptions, formatters.

---

## 6. Closed Unions & Exhaustive Switch (`CS8509` = error)

GGNet models closed sets with the **preview `union` keyword** — genuine discriminated unions over existing record-struct variants (not abstract-record + sealed subclasses, not enums):

```csharp
internal readonly union Shape(ABLine, Area, Circle, HLine, Line, MultiPolygon, Path, Polygon, Rectangle, Text, VLine);
internal readonly union Element(Circle, HLine, Rectangle, VLine);
internal readonly union ScreenPrimitive(ScreenCircle, ScreenLine, ScreenRect, ScreenFill, ScreenStroke, ScreenPolygon, ScreenRule, ScreenText, ScreenLabel, ScreenAngledLabel);
```

Switch over a union with **no discard arm** so adding a variant is a compile error until every switch handles it — this is exactly what `CS8509`-as-error buys (canonical example: `Scene/ShapeComposer.cs`). When you add a variant to a `union`, expect and fix the resulting build errors across every switch; do not paper over them with `_ =>`.

Distinct idiom: **enum → SVG-string** render switches (`Anchor`, `LineType`, `Units`, `RenderMode`) keep a `_ => throw new NotImplementedException()` (or `GGNetInternalException`) discard arm. Those are not `CS8509` sites.

---

## 7. Visible Fields Are a Deliberate Idiom (`CA1051` = off)

The visible fields the analyzer would flag are `protected readonly` fields on base classes (`Geom`: `protected readonly IReadOnlyList<T> source; protected readonly (bool x, bool y) scale`; `Scale`: `protected readonly ITransformation<TKey> transformation`) and mutable instance fields on Blazor components (`Panel`, `Plot`, `TooltipBase`). This is intentional for the hot render path — keep it. Note this is *not* public fields on the record structs: `Shapes`/`Elements` use `init` properties, not raw fields.

---

## 8. Error Handling — Exceptions, No `Result`

Failure is signaled by **thrown exceptions** via a small custom hierarchy in `src/GGNet/Exceptions/` (all `public class`, primary ctor):

- `GGNetException(string message) : Exception` — the base.
- `GGNetUserException : GGNetException` — **DSL / API misuse**: a null required selector, an incompatible combination (`Flip()` + polar), an uninferrable type, an unsupported coordinate system. Throw this from guards at the top of `Build`/`Geom_*` with a message the caller can act on.
- `GGNetInternalException : GGNetException` — **invariant / "unreachable"** violations (e.g. an unmapped `RenderMode` in a factory).

There is no `Result<TError>` type anywhere and none should be introduced — this is a rendering library whose failures are programmer/caller errors, not business outcomes. (The ziggy-brew two-tier `Result` model does not apply here.)

---

## 9. XML Docs — Public DSL Surface Only

`GenerateDocumentationFile=true` on every project (the emitted `GGNet.xml` is consumed by a test, §Testing). The convention:

- **Every public builder/DSL method carries full `///` docs** — a `<summary>` and a `<param>` for every argument (`Geom_*`, `Scale_*`, `Build`, `Style`, and the formatters). These docs are the DSL's manual in IntelliSense, and `OverloadConsistencyTests` asserts they agree across an overload family (see [dsl.instructions.md](./dsl.instructions.md)).
- **Non-public code stays undocumented** — internal geoms/scales/shapes use plain `//` comments for intent only. `CS1591`/`CS1573` are off so this produces no warnings.

---

## 10. Formatting Numbers & Dates — Invariant by Default (`CA1305` = error)

Culture-sensitive formatting is a build error because it breaks SVG on comma-decimal locales. The rules:

- **Never call a culture-sensitive `ToString`.** Use `value.ToString(CultureInfo.InvariantCulture)`, `FormattableString.Invariant($"…")`, or `sb.Append(CultureInfo.InvariantCulture, $"…")`.
- **User-facing formatting goes through `IFormatter<T>`** (`Formats/`): `Standard<T>`, `DoubleFormatter`, `InstantFormatter`. All format **invariantly by default**, taking an optional `CultureInfo?` that falls back to `CultureInfo.InvariantCulture`. Localized tick labels are an explicit opt-in via the `formatter:` parameter — never a global default.
- SVG numeric emission has a single choke point (`Components/SvgFormat.cs`) — see [rendering.instructions.md](./rendering.instructions.md) for the geometry-vs-label boundary.

**NodaTime** is GGNet's temporal type system for axes/scales (`LocalDate`, `LocalDateTime`, `Instant`; scales `DateTimePosition`, `InstantPosition`, `DiscreteDates`; `InstantFormatter` via `ZonedDateTimePattern` + `DateTimeZoneProviders.Tzdb`). Use NodaTime patterns with `CreateWithInvariantCulture`. This is a charting concern, not a domain-time mandate — `DateTime` is not forbidden library-wide, but the axis/scale system speaks NodaTime.

---

## 11. Async

- Prefer `ValueTask` / `ValueTask<T>`; the interactive render loop and headless export are the async surfaces. `CancellationToken token` last, always forwarded.
- No `.Result` / `.Wait()`. Marshal UI-thread work through `InvokeAsync` (see the Blazor guide) — never call `StateHasChanged` directly.

---

## 12. Comments

- **No `///` on non-public code.** `//` for non-obvious intent or `TODO` with context.
- `#region` is not used for grouping here; keep files small — **one primary type per file, filename matches it.** Deliberate exceptions: a closed `union` sits with its variants (`Scene/ScreenPrimitive.cs`), a generic/non-generic pair shares a file (`Stats/Bin.cs`), and `partial`-per-concern splits use dotted filenames that name the concern, not a bare type (`BuilderExtensions.<Geom>.cs`, `Style.<Concern>.cs`, `PlotContext.Build.cs`).
