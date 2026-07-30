---
applyTo: "tests/**/*.cs"
---

# Testing Guide

Scope: all test projects under `tests/`. Assumes [csharp.instructions.md](./csharp.instructions.md) applies. Deterministic behavior-pin evals have their own rules in [evals.instructions.md](./evals.instructions.md); the byte-pinned SVG goldens are documented in [rendering.instructions.md](./rendering.instructions.md) — this guide covers the shared testing conventions.

---

## 1. Frameworks

| Purpose | Library |
|---|---|
| Test framework | **xUnit v3** on **Microsoft.Testing.Platform v2** — package `xunit.v3.mtp-v2` |
| Assertions | **AwesomeAssertions** (the FluentAssertions fork) — exclusively |
| Mocking | **Moq** — only where an interface seam genuinely needs it |
| Blazor components | **bUnit** (v2) |
| Snapshot / goldens | **Verify.XunitV3** |

Test projects are **self-hosting MTP executables**: `<OutputType>Exe</OutputType>` + `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`. The entry point is generated — never write a `Main`. Runner selection lives in `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`), which the SDK 10+ `dotnet test` honours.

Keep the **`-mtp-v2` suffix** on every future xunit bump: plain `xunit.v3` pulls MTP v1. MTP v2 becomes the default only in xunit v4.

Do **not** reintroduce `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, or `coverlet.collector` — MTP is native to xunit v3 and needs no VSTest adapter, and coverlet's collector is a VSTest data collector that does not run under MTP. For coverage, use `Microsoft.Testing.Extensions.CodeCoverage`.

Do **not** introduce: real FluentAssertions, NSubstitute, Shouldly, NUnit, MSTest, Bogus, AutoFixture, raw `Assert.*`. There is **no Aspire, no integration-DB fixture** — GGNet is a library; tests run in-process.

Test projects: `GGNet.Components.Tests` (bUnit), `GGNet.Headless.Tests` (Verify goldens + pipeline), `GGNet.Evals` (deterministic evals), `GGNet.E2ETests` (Playwright smoke over the spawned `GGNet.Demo` app — every test self-skips unless `GGNET_E2E=1`, so the default gate stays browser-free). Each has a `GlobalUsings.cs` and pulls xUnit via `<Using Include="Xunit" />`.

---

## 2. File, Class & Method Naming

- File: `<TypeUnderTest>Tests.cs` (or a scenario name like `LocaleTests.cs`, `OverloadConsistencyTests.cs`), flat at the project root.
- Class: `public class <X>Tests` — no `sealed`/`static`/abstract base. bUnit classes derive from `BunitContext` (§5).
- **Method names are plain PascalCase scenario descriptions** — `RendersSvgWithPanel`, `FactoryMapsModes`, `DensityIntegratesToOne`, `MeasuredCardinalityBeatsSuppliedOne`. **Never** `Method_State_Expected`.

---

## 3. Strict AAA Section Comments

Every test carries `// Arrange`, `// Act`, `// Assert` section comments on their own lines (the combined `// Arrange / Act` and `// Act / Assert` forms are allowed when a step is a one-liner):

```csharp
[Fact]
public void FactoryMapsModes()
{
    // Arrange

    var plot = new Mock<IPlotRendering>();

    // Act

    var interactive = RenderModeHandler.Factory(RenderMode.Interactive, plot.Object);

    // Assert

    interactive.Should().BeOfType<InteractiveRenderModeHandler>();
}
```

Keep **Act** minimal; prefer several focused tests over one branching test. `[Theory]` + `[InlineData]` for varying inputs, `[MemberData]` when the cases are richer than inline constants — never copy-paste `[Fact]` bodies.

---

## 4. Assertions — AwesomeAssertions

- One `.Should()` per logical check; the most specific assertion available (`.Should().BeOfType<T>()`, `.Should().Contain("<svg")`, `.Should().NotMatchRegex(@"\d,\d")`).
- Wrap multiple independent assertions in `using var _ = new AssertionScope();` at the top of the `// Assert` section (discard name always `_`). `AwesomeAssertions.Execution` is globally used in the eval project for this.
- Snapshot/golden assertions go through `Verifier.Verify(...)` (rendering guide), not manual string equality.

---

## 5. bUnit Component Tests

`GGNet.Components.Tests` exercises the interactive `Plot` in-process (no browser):

```csharp
public class PlotComponentTests : BunitContext   // xUnit constructs/disposes it per test
{
    [Fact]
    public void RendersSvgWithPanel()
    {
        // Arrange / Act

        var cut = Render<Plot<Point, double, double>>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.RenderMode, RenderMode.Interactive));

        // Assert

        cut.Markup.Should().Contain("<svg");
        cut.FindAll("rect.panel").Should().NotBeEmpty();
    }
}
```

- Derive from `BunitContext` (bUnit v2 naming) — do not `new` a `TestContext`.
- `global using Bunit;` lives in the project's `GlobalUsings.cs`.

---

## 6. Mocking — Moq, Sparingly

Moq is used in exactly one place today (`RenderModeHandlerTests`) and only where an interface seam earns it — most tests construct real objects and assert on rendered output.

- `var x = new Mock<IXxx>();`, pass `.Object`. Mocking **internal** interfaces (e.g. `IPlotRendering`) works because `src/GGNet/GGNet.csproj` grants `InternalsVisibleTo("DynamicProxyGenAssembly2")` (Moq's Castle dynamic-proxy assembly), alongside `InternalsVisibleTo` for `GGNet.Headless` / `GGNet.Headless.Tests`.
- `using Moq;` is a **per-file** using (not global) with a comment, to avoid `Moq.Match` colliding with `System.Text.RegularExpressions.Match`.
- Don't verify log messages or over-mock; if a test needs to mock the world, the seam is wrong.

---

## 7. Test Data & Repo Location

- Construct domain objects inline; no builders.
- To locate repo-relative assets (skill files, snippets), tests use a `[CallerFilePath]`-based `RepoRoot()` helper that walks up from the source file — reuse that pattern rather than hardcoding paths or `Directory.GetCurrentDirectory()`.

---

## 8. Async Tests

- Return `async Task` / `Task` — never `async void`. No `.Result` / `.Wait()`.
- Verify-based golden tests may return the `Task` from `Verifier.Verify(...)` directly.
