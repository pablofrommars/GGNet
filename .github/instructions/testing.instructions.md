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

Keep the **`-mtp-v2` suffix** on every future xunit bump to make the runner choice explicit. On the 4.x package line, plain `xunit.v3` also depends on `xunit.v3.mtp-v2`; the 3.x package line selected MTP v1 by default. Package version 4.0.2 still ships the xUnit.net v3 API and selects Microsoft.Testing.Platform 2.5.1.

`GGNet.Headless.Tests.csproj` declares Verify's `OpenSource` sponsorship exemption through **October 2027**, based on GGNet being open source and generating no revenue. Verify 33 checks this declaration at build time. Recheck eligibility before renewing `Verify_SponsorshipExemptionUntil`; expired declarations fail the build. See [Verify's maintenance-fee documentation](https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md).

Do **not** reintroduce `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, or `coverlet.collector` — MTP is native to xunit v3 and needs no VSTest adapter, and coverlet's collector is a VSTest data collector that does not run under MTP. For coverage, use `Microsoft.Testing.Extensions.CodeCoverage`.

Do **not** introduce: real FluentAssertions, NSubstitute, Shouldly, NUnit, MSTest, Bogus, AutoFixture, raw `Assert.*`. There is **no Aspire, no integration-DB fixture** — GGNet is a library; tests run in-process.

Test projects: `GGNet.Components.Tests` (bUnit), `GGNet.Headless.Tests` (Verify goldens + pipeline), `GGNet.Evals` (deterministic evals), `GGNet.E2ETests` (Playwright smoke over the spawned `GGNet.Demo` app — browser cases self-skip unless `GGNET_E2E=1`; `ProcessOutputTailTests` always runs without a browser). Each has a `GlobalUsings.cs` and pulls xUnit via `<Using Include="Xunit" />`.

The demo fixture drains both output streams using `Process.ReadAllLinesAsync` and retains the last 50 lines, capped at 2048 characters per line, for startup diagnostics. Teardown stops the process tree, awaits the output reader and disposes the process even if browser cleanup fails. Keep that ownership explicit when changing the fixture.

---

## 2. File, Class & Method Naming

- File: `<TypeUnderTest>Tests.cs` (or a scenario name like `LocaleTests.cs`, `OverloadConsistencyTests.cs`), flat at the project root.
- Class: `public class <X>Tests` — no `sealed`/`static`/abstract base. bUnit classes derive from `BunitContext` (§5).
- **Method names are plain PascalCase scenario descriptions** — `RendersSvgWithPanel`, `FactoryMapsModes`, `DensityIntegratesToOne`, `MeasuredCardinalityBeatsSuppliedOne`. **Never** `Method_State_Expected`.

---

## 3. Strict AAA Section Comments

Every test carries **three separate** `// Arrange`, `// Act`, `// Assert` section comments, each on its own line with a blank line around it — **even when a section is empty or a step is a one-liner**. The combined `// Arrange / Act` and `// Act / Assert` forms are not used. A comment may follow the marker on the same line (`// Arrange — why the fixture is shaped this way`) when the reason is non-obvious:

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

Keep **Act** minimal — typically the one or two lines that exercise the subject; collect all setup in Arrange first. A golden test arranges the plot, acts by rendering it (`await plot.AsStringAsync()`), and asserts by verifying the SVG. Prefer several focused tests over one branching test. `[Theory]` + `[InlineData]` for varying inputs, `[MemberData]` when the cases are richer than inline constants — never copy-paste `[Fact]` bodies.

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
        // Arrange

        // Act

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

Moq is used only where an interface seam earns it (`RenderModeHandlerTests`, `ConcurrencyStressTests`) — most tests construct real objects and assert on rendered output.

- `var x = new Mock<IXxx>();`, pass `.Object`. Mocking **internal** interfaces (e.g. `IPlotRendering`) works because `src/GGNet/GGNet.csproj` grants `InternalsVisibleTo("DynamicProxyGenAssembly2")` (Moq's Castle dynamic-proxy assembly), alongside `InternalsVisibleTo` for `GGNet.Headless` / `GGNet.Headless.Tests`.
- `Moq` is a **global using** in `GGNet.Headless.Tests/GlobalUsings.cs`, next to a **global alias** that resolves the collision between `Moq.Match` and the globally-used `System.Text.RegularExpressions.Match`: `global using Match = System.Text.RegularExpressions.Match;`. No test file imports Moq itself.
- Don't verify log messages or over-mock; if a test needs to mock the world, the seam is wrong.

---

## 7. Comments

The AAA markers are mandatory (§3). Beyond them, a comment in a test earns its place only by stating the non-obvious reason a fixture or case is shaped the way it is; prose restating what the test plainly does is not kept. Comments stay self-contained — never cite `plan/` or `specs/` artifacts. The full policy, including the tagged comments production code under `src/` may carry, is the [C# guide](./csharp.instructions.md) §12.

---

## 8. Test Data & Repo Location

- Construct domain objects inline; no builders.
- To locate repo-relative assets (skill files, snippets), tests use a `[CallerFilePath]`-based `RepoRoot()` helper that walks up from the source file — reuse that pattern rather than hardcoding paths or `Directory.GetCurrentDirectory()`.

---

## 9. Async Tests

- Return `async Task` / `Task` — never `async void`. No `.Result` / `.Wait()`.
- Verify-based golden tests are `async Task` bodies like any other test: render in Act, `await` the verification in Assert.
