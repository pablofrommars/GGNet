---
applyTo: "**/*.cs"
---

# C# Coding Style Guide

Scope: every `.cs` file in the solution. GGNet is a single-author, gate-enforced **charting library** (ggplot2-style grammar of graphics, SVG output, .NET 11 / Blazor). These are its real conventions, extracted from the code and `.editorconfig` — not a generic .NET house style.

> GGNet follows tsu's cross-repo C# conventions, with the exceptions `AGENTS.md` lists and this guide states at each rule: the `GGNetException` hierarchy instead of `Result<TError>` (§8), `///` docs on the public DSL surface (§9), NodaTime as the axis type system rather than a domain mandate (§10), and per-file usings for one-file needs (§3). There is no TimescaleDB/MQTT anything; ignore a consuming repository's own instructions.

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

**Analyzer severities are tuned in `.editorconfig`, never per project.** A project-level `<NoWarn>` is not used. A one-off diagnostic that is genuinely wrong at one site is suppressed with a scoped `#pragma warning disable <id>` / `#pragma warning restore <id>` pair around that site, with a comment saying why. Existing examples include `GalleryTests` facts whose inline `new[] { … }` arguments are pinned verbatim to the skill examples (CA1861), and controlled JS test-double forwarding in `PanelLifecycleTests` that must propagate injected failures (BL0016).

The local gates every change must pass — build, test, and both `dotnet format` checks (also in CI):

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
- **Prefer primary constructors** — they capture parameters directly. **No `this.` prefix, anywhere** — not even to disambiguate a field from a constructor parameter: store derived or validated state with a field initializer over a primary-constructor parameter (`private readonly IFormatter<double> formatter = formatter ?? Standard<double>.Instance;`), or name the parameter and the field apart. **Private fields are `camelCase` with no `_` prefix** (`source`, `transformation`).
- Blank line between members; grouped `using`s separated by a blank line.

---

## 3. Namespaces & Global Usings

- Root namespace is **`GGNet`** (file-scoped). Sub-namespaces mirror folders: `GGNet.Exceptions`, `GGNet.Geoms`, `GGNet.Geoms.Point`, `GGNet.Scales`, `GGNet.Shapes`, `GGNet.Elements`, `GGNet.Coords`, `GGNet.Data`, `GGNet.Formats`, `GGNet.Scene`.
- **Match-folder is enforced** (`IDE0130` = warning). The one documented exception: files under `src/GGNet/Stats/` declare the root `namespace GGNet;` on purpose — `Stat.Bin` is DSL surface and sits next to `PlotContext.Build`. That exception is encoded in `.editorconfig`; don't add others without a rationale comment there.
- **One `GlobalUsings.cs` per project** (5 under `src/`, plus one per test project). The core project's globally imports `System.Globalization`, `System.Collections.Frozen`, `System.Text`, `Microsoft.Extensions.ObjectPool`, the `Microsoft.AspNetCore.Components.*` set, and `NodaTime` + `NodaTime.Text`. **Global usings are preferred** — add a broadly-used namespace here, and resolve a name collision between two global usings with a **global alias** in the same file (`GGNet.Headless.Tests` imports both `Moq` and `System.Text.RegularExpressions` globally and pins `global using Match = System.Text.RegularExpressions.Match;`).
- **Exception — per-file usings.** A using needed by one file, or a narrow sub-namespace import (`using Geoms.Line;`, `using Scales;`, `using static Position;`), may stay in that file. When a per-file import is used widely, promote it to `GlobalUsings.cs`.

---

## 4. Project Configuration — Shared Properties and Central Package Management

**`Directory.Build.props` at the repo root carries shared *properties*; `Directory.Packages.props` carries every package version (Central Package Management).** A `PackageReference` in a `.csproj` is versionless. To add a package, add `<PackageVersion Include="X" Version="Y" />` to `Directory.Packages.props` and a versionless `<PackageReference Include="X" />` to the consuming project. Both files sit at GGNet's root on purpose: when GGNet is checked out as a submodule inside a consumer, MSBuild finds them before any consumer-level file, so GGNet always builds against its own versions.

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

It also holds the NuGet metadata shared by the two packable projects (`Version`, `Authors`, `Title`, `Copyright`, `Description`, `PackageTags`, `PackageLicenseExpression`) — so the shipped version is stated **once**. A `.csproj` declares only what is genuinely its own: `IsPackable=true` and its distinct `PackageDescription` on `GGNet`/`GGNet.Headless`, `OutputType`, `IsTestProject` — never a `NoWarn` (§1).

`LangVersion=preview` remains the **required repository setting**; changing language policy is separate from the SDK update. The SDK is pinned to .NET 11 RC3 in `global.json`. C# 15 provides the `union` types used in §6.

---

## 5. Type Idioms

`internal` is the **default** visibility; `public` is reserved for the deliberate DSL surface (`PlotContext`, `BuilderExtensions`, `Stat`, the DSL enums, `IFormatter<T>` and formatters, the `Stat.*` record structs). Sample patterns, all real:

- **Geoms** — `internal sealed class Point<T, TX, TY> : Geom<T, TX, TY>`, over an `internal closed class Geom<T, TX, TY>`. Constraints `where TX : struct where TY : struct` are pervasive.
- **Shapes** (`Shapes/`) — `internal readonly record struct Circle : IShape`, with `{ get; init; }` properties and `required` on mandatory members.
- **Elements** (`Elements/`) — `internal readonly record struct` with an explicit parameterless ctor supplying init defaults (`public Rectangle() {}` + `Fill { get; init; } = "inherit"`).
- **Data carriers** (`Data/`) — `internal sealed class` with mutable `{ get; set; }` (e.g. `Selectors<T, TX, TY>`, `Aesthetics<T>`); some per-geom carriers are `internal sealed record`.
- **Scales** — `internal closed class` hierarchies (`Scale<TKey, TValue>` → `Continuous<TKey>` → `Position<TKey>` → concrete, plus `Discrete<TKey, TValue>`), retaining shared state and virtual behavior. `closed` is implicitly abstract; concrete scales override `public abstract Guide Guide`.
- **Stats** — `public readonly record struct` with positional params (`public readonly record struct Bin(double Min, double Mid, double Max, int Count, double Density)`), plus a `Bin<TKey>` generic variant.

Defaults: `sealed` on classes/records unless designed for inheritance; **primary constructors** for geoms, scales, exceptions, formatters.

Collection expressions can retain constructor arguments: `List<Zone> zones = [with(panels.Count)];` preserves layout capacity, and `HashSet<IStatSource> seen = [with(ReferenceEqualityComparer.Instance)];` preserves source identity. Owned collection subclasses also qualify: `Legends = [with(Style)];` and `Items = [with(style)];` retain the style reference used when later populating the legend. Keep the concrete target, comparer object, argument evaluation and element order. The custom `SortedBuffer<T>` / `SampleBuffer<T>` / `RankedTree<T>` types retain constructor syntax; they do not expose the enumeration protocol needed for collection expressions.

---

## 6. Unions, Closed Hierarchies & Exhaustive Switch (`CS8509` = error)

GGNet models finite value alternatives with the **C# 15 `union` keyword** over record-struct or sealed record-class variants:

```csharp
internal readonly union Shape(ABLine, Area, Circle, HLine, Line, MultiPolygon, Path, Polygon, Rectangle, Text, VLine);
internal readonly union Element(Circle, HLine, Rectangle, VLine);
internal readonly union ScreenPrimitive(ScreenCircle, ScreenLine, ScreenRect, ScreenFill, ScreenStroke, ScreenPolygon, ScreenRule, ScreenText, ScreenLabel, ScreenAngledLabel);
```

Switch over a union with **no discard arm** so adding a variant is a compile error until every switch handles it — this is exactly what `CS8509`-as-error buys (canonical example: `Scene/ShapeComposer.cs`). When you add a variant to a `union`, expect and fix the resulting build errors across every switch; do not paper over them with `_ =>`.

Distinct idiom: **enum → SVG-string** render switches (`Anchor`, `LineType`, `Units`, `RenderMode`) keep a `_ => throw new NotImplementedException()` (or `GGNetInternalException`) discard arm. Those are not `CS8509` sites.

The chart selector's private `Selection` union (`Selector.Selection.cs`) distinguishes recommendations, insufficient data and no match. Its typed cases own the response field sets; `SelectionJsonContext` generates their System.Text.Json contracts. The public `Select` method still returns `JsonObject` through `SerializeToNode`. These are selection outcomes, not a replacement for the exception policy in §8.

`RenderModeHandler` is an `internal closed class` over the three sealed render handlers; the four scale roots in §5 also use `closed`. `CoordinateSystem` closes the Cartesian/polar implementation boundary while retaining measured state and virtual projection, inverse projection and grid composition. It is internal; public `ICoord` remains an interface. `Geom<T, TX, TY>` is also closed: the public generic `Geom_Text` overloads adapt label selectors lazily through `Formats.InvariantText.Of`, so the internal `Text<T, TX, TY>` needs no extra label type parameter. Preserve evaluation once per included row per render, formatting culture and exception timing. These hierarchies retain reference semantics and virtual dispatch. Closure bounds direct inheritance in the owning assembly; it does not seal open descendants. `Faceting<T>` remains abstract: its descendants introduce type parameters absent from the base type, which RC3 rejects on a closed root (CS9383). Changing its generic contract requires separate review. Public extension contracts such as `TooltipBase` remain open.

RC3 exposes `JsonPolymorphicAttribute.InferClosedTypePolymorphism`, but the coordinate/render/geom/scale roots and rendering unions are not production JSON contracts. The selector uses native union serialization with a source-generated context, snake-case field names and the built-in `JsonUnionTypeStructuralClassifier`; it preserves `error` values, missing fields and explicit nulls without adding a discriminator or union wrapper. Its public contract is an output DOM, not a union deserialization API. Blazor interop retains its flat payloads and framework-owned serialization. Closing an internal hierarchy does not justify a serializer-policy change. Before changing polymorphism configuration on a serialized type, prove exact discriminator output and independently fixed historical payload reads, including the actual source-generated path if one is used. Retain explicit registrations/converters unless that compatibility is established.

Use labeled loop exits when they remove control-only bookkeeping. `Wilkinson.ExtendedBase` names its outer `search` loop so an inner pruning decision can exit directly; the loop counter remains a search parameter rather than doubling as an exit sentinel. Keep flags or counters that carry observable results, and preserve post-loop work.

---

## 7. Visible Fields Are a Deliberate Idiom (`CA1051` = off)

The visible fields the analyzer would flag are `protected readonly` fields on base classes (`Geom`: `protected readonly IReadOnlyList<T> source; protected readonly (bool x, bool y) scale`; `Scale`: `protected readonly ITransformation<TKey> transformation`) and mutable instance fields on Blazor components (`Panel`, `Plot`, `TooltipBase`). This is intentional for the hot render path — keep it. Note this is *not* public fields on the record structs: `Shapes`/`Elements` use `init` properties, not raw fields.

---

## 8. Error Handling — Exceptions, No `Result`

Failure is signaled by **thrown exceptions** via a small custom hierarchy in `src/GGNet/Exceptions/` (all `public class`, primary ctor):

- `GGNetException(string message) : Exception` — the base.
- `GGNetUserException : GGNetException` — **DSL / API misuse**: a null required selector, an incompatible combination (`Flip()` + polar), an uninferrable type, an unsupported coordinate system. Throw this from guards at the top of `Build`/`Geom_*` with a message the caller can act on.
- `GGNetInternalException : GGNetException` — **invariant / "unreachable"** violations (e.g. an unmapped `RenderMode` in a factory).

There is no `Result<TError>` type anywhere and none should be introduced — this is a rendering library whose failures are programmer/caller errors, not business outcomes. **Exception:** tsu's `Result<TError>` model is deliberately not adopted here.

---

## 9. XML Docs — Public DSL Surface Only

**Exception:** tsu carries no `///` anywhere; GGNet keeps them on its public DSL surface, because they are the DSL's manual.

`GenerateDocumentationFile=true` on every project (the emitted `GGNet.xml` is consumed by a test, §Testing). The convention:

- **Every public builder/DSL method carries full `///` docs** — a `<summary>` and a `<param>` for every argument (`Geom_*`, `Scale_*`, `Build`, `Style`, and the formatters). These docs are the DSL's manual in IntelliSense, and `OverloadConsistencyTests` asserts they agree across an overload family (see [dsl.instructions.md](./dsl.instructions.md)).
- **Non-public code carries no XML docs** — internal geoms/scales/shapes stay undocumented, and any comment they keep follows §12. `CS1591`/`CS1573` are off so this produces no warnings.

---

## 10. Formatting Numbers & Dates — Invariant by Default (`CA1305` = error)

Culture-sensitive formatting is a build error because it breaks SVG on comma-decimal locales. The rules:

- **Never call a culture-sensitive `ToString`.** Use `value.ToString(CultureInfo.InvariantCulture)`, `FormattableString.Invariant($"…")`, or `sb.Append(CultureInfo.InvariantCulture, $"…")`.
- **User-facing formatting goes through `IFormatter<T>`** (`Formats/`): `Standard<T>`, `DoubleFormatter`, `InstantFormatter`. All format **invariantly by default**, taking an optional `CultureInfo?` that falls back to `CultureInfo.InvariantCulture`. Localized tick labels are an explicit opt-in via the `formatter:` parameter — never a global default.
- SVG numeric emission has a single choke point (`Components/SvgFormat.cs`) — see [rendering.instructions.md](./rendering.instructions.md) for the geometry-vs-label boundary.

**NodaTime** is GGNet's temporal type system for axes/scales (`LocalDate`, `LocalDateTime`, `Instant`; scales `DateTimePosition`, `InstantPosition`, `DiscreteDates`; `InstantFormatter` via `ZonedDateTimePattern` + `DateTimeZoneProviders.Tzdb`). Use NodaTime patterns with `CreateWithInvariantCulture`. This is a charting concern, not a domain-time mandate — `DateTime` is not forbidden library-wide, but the axis/scale system speaks NodaTime. **Exception:** tsu's NodaTime-exclusive domain rule does not apply.

---

## 11. Async & Synchronization

- Prefer `ValueTask` / `ValueTask<T>`; the interactive render loop and headless export are the async surfaces. `CancellationToken token` last, always forwarded.
- No `.Result` / `.Wait()`. Marshal UI-thread work through `InvokeAsync` (see the Blazor guide) — never call `StateHasChanged` directly.
- **Prefer `SemaphoreSlim` for new or redesigned mutual exclusion.** Avoid introducing `lock`, `System.Threading.Lock` or direct `Monitor` use. A justified exception needs a scoped `// DEVIATION:` explaining why a semaphore is unsuitable.
- Use `new SemaphoreSlim(1, 1)` for a mutual-exclusion gate. In async code, acquire with `await gate.WaitAsync(token)`; enter `try` only after successful acquisition and always `Release()` in `finally`. Cancellation before acquisition must not release the gate. Do not block on asynchronous acquisition.
- Keep protected work narrow. A semaphore is not reentrant: review nested acquisition, callbacks, acquisition order and disposal races when changing an existing gate. Dispose it only after all users and waiters have finished, and preserve the lifecycle guarantees pinned by the concurrency tests.

---

## 12. Comments & Documentation

- **XML docs (`///`) only on the public DSL surface** (§9). Nothing else carries them.
- **Production code under `src/` carries no `//` comments by default.** Naming and structure carry intent, and a comment describing *what* the code does is a signal the code should be clearer.
- **The gate is one question, asked before the exceptions are consulted: *can a test pin this claim?*** If it can, write the test and delete the comment. A test fails when the claim stops being true; a comment making the same claim drifts silently, and the two copies then disagree with no signal. This is the whole of the rule — the tags below are the residue of that question, not a menu to choose from. In particular, a comment defending a design choice almost always fails the gate: the defence belongs in the test that pins the behaviour.
- **Every surviving comment carries one of four tags**, so the sanctioned set in the tree is one command — `rg -n "// (TODO: calibration|RUNBOOK|INVARIANT|DEVIATION)" src/`. An untagged full-line `//` comment block in `src/` is a policy violation, and `CommentPolicyTests` (`tests/GGNet.Headless.Tests/`) fails on one. A block is tagged on its first line; the lines that continue it need no tag of their own.
- **`// TODO: calibration — <what evidence settles it>` — calibration parameters.** Thresholds, tolerances, and budgets whose value awaits real-world evidence are annotated at the declaration, naming the evidence that would settle them.
- **`// RUNBOOK: <what a person must do>` — operating instructions.** What a person must *do* around this code and cannot infer from it: the command that regenerates something, an ordering prerequisite, a required environment variable. It is not licence for design prose.
- **`// INVARIANT: <what breaks without it>` — a condition a reader would otherwise reintroduce a bug against, and that no name can carry.** The paradigm case is silent failure no test can reach: an ordering whose violation produces no observable event, a concurrency race whose losing interleaving is not schedulable on demand, a serialization or rendering behaviour keyed on something no test observes. It also covers the external-system quirk whose violation a test *does* catch but cannot explain — the browser, SDK, or SVG renderer that accepts one form and rejects another — because there the test stops the bug and the comment stops the hour spent rediscovering why. State the invariant and what breaks without it, never the mechanism. A behavioural claim a test could simply assert is not one of these: write the test.
- **`// DEVIATION: <rule> — <why>` — departure from a documented rule in these guides.** One line naming the rule and the reason, at the site. Anything longer is an argument for changing the guide instead.
- **Tests under `tests/` are governed differently.** The `// Arrange` / `// Act` / `// Assert` section comments are mandatory ([testing guide](./testing.instructions.md) §3). Beyond those, a comment is allowed only where it earns its keep: the non-obvious reason a fixture or case is shaped the way it is. Prose restating what the test plainly does is not.
- **Every comment that survives is self-contained.** Never cite `plan/` or `specs/` artifacts or their decision shorthand — those documents are retired and the shorthand is meaningless to a later reader. When a decision's *content* matters at the site, inline the substance instead of the id. References to durable repository files (guides under `.github/instructions/`, `skills/ggnet/`) are fine.
- **Out of scope:** `.razor` markup comments (`@* … *@`), `.razor.js`, and `skills/**/*.cs` — the policy and its test cover C# under `src/`.

## 13. File Organization

- `#region` is not used for grouping here; keep files small — **one primary type per file, filename matches it.** Deliberate exceptions: a closed `union` sits with its variants (`Scene/ScreenPrimitive.cs`), a generic/non-generic pair shares a file (`Stats/Bin.cs`), and `partial`-per-concern splits use dotted filenames that name the concern, not a bare type (`BuilderExtensions.<Geom>.cs`, `Style.<Concern>.cs`, `PlotContext.Build.cs`).
