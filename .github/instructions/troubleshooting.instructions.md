---
applyTo: "**"
---

# Troubleshooting Guide

Scope: the failures an agent actually hits in this repo, indexed by **symptom**. Read this when something fails or hangs — not before every edit. The other guides say what the conventions *are*; this one says what it looks like when you have broken one, and what the fix is.

Every entry below is grounded in this repo's gates, tests, and tooling. If you hit a failure that is not here and the diagnosis was not obvious, add it.

---

## 1. A `dotnet` command hangs with no output at all

**Symptom.** `dotnet run`, `dotnet test`, a first-time `restore`, or `dotnet format` produces **zero** output and eventually times out. `dotnet build` of an already-restored project works fine, which makes it look like the specific command is slow rather than blocked.

**Cause.** Agent shells often run in a sandbox that blocks local named pipes and socket binding. Every one of those commands spawns a child process and talks to it over IPC, so it stalls at connect time instead of failing loudly.

**The tell.** `dotnet format` is the one command that reports it:

```
at Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager.BuildHostProcess.ConnectAsync(String pipeName)
```

That stack trace names the root cause for *all* of the above.

**Fix.** Re-run the same command with the sandbox disabled (in Claude Code, `dangerouslyDisableSandbox: true`; the user can adjust defaults with `/sandbox`). Do not spend time bisecting a silent hang — it is not your command, it is the pipe.

**Corollaries.**
- A scratch project created outside the repo is un-restored *and* outside `Directory.Build.props` / `global.json`, so it hangs on restore and has no `TargetFramework`. Put scratch projects **inside** the repo tree, declare only `OutputType` + `ProjectReference`, and delete them when done.
- Stopping a backgrounded `dotnet run` kills the parent, not the app child — the port stays bound. Reclaim it with `lsof -ti:<port> | xargs kill`.

---

## 2. `dotnet build -warnaserror` fails on something that looks cosmetic

`-warnaserror` makes any warning a failure. The four that actually bite here, and what each means:

| Diagnostic | Meaning | Fix |
|---|---|---|
| `CA1305` (**error**) | Culture-sensitive formatting. Breaks SVG on comma-decimal locales. | `CultureInfo.InvariantCulture`, `FormattableString.Invariant($"…")`, `sb.Append(CultureInfo.InvariantCulture, $"…")`, or route through `IFormatter<T>`. In markup, `SvgFormat.Num`/`Attr`. |
| `CS8509` (**error**) | A `switch` over a closed `union` (`Shape`, `Element`, `ScreenPrimitive`) is missing an arm. | Add the arm. **Do not** silence it with `_ =>` — that discard is exactly what this rule buys you. Enum→SVG-string switches are a different idiom and *do* keep a throwing discard arm. |
| `IDE0130` | Namespace does not match folder. | Match the folder. The one sanctioned exception is `src/GGNet/Stats/*` in the root `GGNet` namespace, encoded in `.editorconfig`. |
| `IDE0005` | Unused using. | Strip it; do not add a suppression. |

`CS1591`/`CS1573` are **off** on purpose — non-public code stays undocumented without warnings. If you see them, something changed in `.editorconfig`; do not "fix" it by adding `///` to internals.

Read `.editorconfig` before arguing with a severity — every deliberate suppression there carries a one-line rationale.

---

## 3. A gallery golden fails

**Symptom.** A `Verify` test fails and writes `tests/GGNet.Headless.Tests/Gallery/GalleryTests.<Name>.received.svg` next to the `.verified.svg`.

**First question: did you intend to change the rendered output?**

- **No** → you have a real regression. Diff received vs verified; the delta points straight at the change.
- **Yes** → eyeball every diff, then promote received → verified. A re-pin is a deliberate, eyeballed decision, never a reflex (`ROADMAP.md`, Operating conventions). One render-touching change can legitimately move 40+ snapshots; that is normal, but confirm each one is intended rather than accepting in bulk.

**Things that trip people up.**
- Per-instance plot ids (`gg<hash>`) are scrubbed to `ggID` by `VerifyConfig.cs`. The scrubber regex is `gg(?!net-)…` so `--ggnet-*` theme variables survive — if you name something `gg…`, expect it to be scrubbed.
- `PublicApiTests` snapshots route to `Api/`, everything else to `Gallery/` (`Verifier.DerivePathInfo`). A new snapshot appearing in the wrong folder means the type name did not match.
- `GalleryTests.VerifyPlot` runs `XDocument.Parse` on the SVG, so a *parse* failure means you emitted something that is not well-formed pure SVG (an HTML fragment, an unescaped attribute), not a snapshot mismatch.
- A changed public surface fails `PublicApiTests` as a `.txt` diff. That file is the deliberate public-surface lock — widening it is an API decision, not a test fix.
- CI uploads `tests/**/*.received.*` as the `received-snapshots` artifact on failure, so a red CI run is inspectable without reproducing locally.

---

## 4. `OverloadConsistencyTests` or `BuilderForwardingTests` fails

These guard the 22 hand-synced `BuilderExtensions.*.cs` overload partials. Source generation of these families was tried and **permanently retired** in favour of verification, so consistency is on you.

`OverloadConsistencyTests` asserts four invariants per name-family — the failure message tells you which:

1. **Defaults agree** — the same parameter name has an identical default across every overload.
2. **Parameter shapes agree** — types agree within a `(name, receiver)` sub-family (`source`/`palette`/`polygons` are excluded as legitimate dispatch variance).
3. **Sugar overloads preserve canonical order** — a shorter overload keeps the longest sibling's parameter order.
4. **Docs agree** — each `<param>`'s XML-doc text is identical across the family, loaded from the generated `GGNet.xml`.

**Fix:** make the family consistent — do not relax the test. Gate 4 failing usually means you edited one overload's `///` and not its siblings; note that `inheritdoc` does **not** resolve between overloads, so the docs really are stamped per-overload.

`BuilderForwardingTests` catches a sugar overload forwarding to the canonical implementation with arguments in the wrong slots. A positional-forwarding bug of exactly this kind shipped for years before this gate existed — when adding an overload, forward **by name**, not by position.

---

## 5. `ThemeContractTests` fails

The contract: every emitted CSS class is painted, every referenced `var(--ggnet-*)` is defined, and a theme file may set **only** variables the base declares.

- Added a paintable class or a new `var(--ggnet-*)` reference? Update `src/GGNet/Themes/Default.css` **in the same change**.
- A theme introducing its own variable fails by design — a theme is a block of variable overrides, not a stylesheet fork.
- Remember the split: **layout is C#** (`Style`, because the server measures text to lay the plot out), **paint is CSS**. Reaching for a scoped `.razor.css` to change a colour is the wrong lever; add a variable and paint it in the theme.

---

## 6. A skill or eval drift test fails

The skill is wired to the library and the goldens by tests, so a DSL change is a multi-file change.

| Failing test | What it means | Fix |
|---|---|---|
| `SkillExampleConsistencyTests.ExamplesMatchGallerySource` | A ```csharp block in `skills/ggnet/examples/*.md` no longer appears verbatim (modulo whitespace) in `GalleryTests.cs`. | Reconcile the example with the gallery source. Examples are *extracted*, not authored. |
| `SkillDocDriftEvals.ChartTableAgreesWithConfig` | `skills/ggnet/reference/chart-selection.md` disagrees with `chart_selection.json` on row count, function terms, or `❌` markers. | Update the doc to match the config — the config is the single source of truth. |
| `ChartSelectionEvals` structural facts | `EveryLeafReachable`, `RecipesResolveToExampleFiles`, `SupportedLeavesCarryGuidance`, `ConfigLoadsAndValidates` (pins the leaf count). | You changed `chart_selection.json`; expect these and `SkillDocDriftEvals` to move together. Reconcile both — never silence one. |
| `McpServerEvals.ListsExpectedTools` | The MCP tool-name pin. | Adding a tool means pinning its name here. |

Note the split when editing the MCP server: **geoms/scales are reflected live** from the loaded assembly (`Introspect`), **chart selection is config-driven**. A new `Geom_*` appears in `list_geoms` automatically — there is no list to hand-maintain, and adding one is a bug.

---

## 7. "All tests passed" but the browser layer never ran

`tests/GGNet.E2ETests` **self-skips every test unless `GGNET_E2E=1`** (`Fixtures/DemoAppFixture.cs`), so the default gate stays browser-free. A green `dotnet test GGNet.slnx` therefore says nothing about the executed-JS layer.

If you touched `src/GGNet/Components/Panel.razor.js` or its C# wrapper, run the smoke explicitly:

```
GGNET_E2E=1 dotnet test tests/GGNet.E2ETests
```

It needs a Playwright Chromium installed.

---

## 8. Test-infrastructure surprises

- Test projects are **self-hosting MTP executables** (`OutputType=Exe`, `UseMicrosoftTestingPlatformRunner`). The entry point is generated — never write a `Main`.
- Keep the **`-mtp-v2` suffix** on every xunit bump: plain `xunit.v3` pulls MTP v1. MTP v2 becomes the default only in xunit v4.
- Do **not** add `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, or `coverlet.collector`. MTP is native to xunit v3 and needs no VSTest adapter; coverlet's collector is a VSTest data collector that does not run under MTP. For coverage use `Microsoft.Testing.Extensions.CodeCoverage`.
- Mocking **internal** interfaces works because `GGNet.csproj` grants `InternalsVisibleTo("DynamicProxyGenAssembly2")`. If a mock fails to proxy, check that IVT before redesigning the seam.
- `using Moq;` is deliberately **per-file**, not global — `Moq.Match` collides with `System.Text.RegularExpressions.Match`.

---

## 9. Polar / radar traps

`Coord_Polar` (implied by `Geom_Radar`) changes the rules in ways that produce silently wrong output rather than errors:

- **Setting a scale explicitly opts out of the coordinate system's expansion hints.** The polar angular scale needs the full turn — `XExpansion(discrete: true)` is `(0, 0, 0, 1)`, mapping n categories to `i/n` with the extra slot closing the wrap. If you call `Scale_X_Discrete(...)` yourself you must restate `expand: (0.0, 0.0, 0.0, 1.0)`, or `DiscretePosition`'s default `(0, 0.6, 0, 0.6)` applies and every category lands at the wrong angle.
- **Offsets are radial only.** There is no screen-space `dy`. A `Geom_Text` layer nudged to sit "just below" an axis label drifts sideways by `Δr·|cos θ|` — on the ±30°/±150° axes a one-line vertical drop displaces the text by roughly 2× that drop horizontally. Radial stacking is the only thing that works cleanly.
- **Two-line axis labels are the `titles:` opt-in, not a `tspan`.** Each break label is still a single `<text>` with a single fill — there is no wrapping — but `Scale_X_Discrete(titles: ...)` composes a second one-line `<text>` (class `x-break-title`, `Style.Axis.Title.X.FontSize`) stacked beneath each break label, shifted along the spoke so both lines stay outside the web at every angle. Line 1 paints `--ggnet-break-label`, line 2 `--ggnet-break-title` — the title variable defaults *darker* (it was born as the cartesian year row); override it for a lighter status line. Date scales populate the same titles channel automatically, so a date-discrete x axis under polar grows its month/year row as a second line too — uniform behavior, not a bug.
- **Radial break labels are drawn inside the panel**, up the twelve-o'clock spoke. To keep the rings but drop their numbers, pass a formatter returning `string.Empty`; `hide: true` removes the rings too.
- **`Unproject` throws** (`GGNetUserException`) under polar, so anything depending on screen→data inversion (crosshair, pan) does not apply.
- **The web is inscribed in `min(width, height)`.** Long category labels on the ±30° axes extend horizontally past it — widen the canvas rather than shrinking the font. `Style.Polar.LabelMargin` does *not* help: label radius works out to `min(w, h)/2 - fontHeight` regardless of it.
- `Geom_Radar` sorts its vertices by x and closes the polygon itself; `Geom_Segment` has **no** `scale` parameter, so it always trains the scales — pin `limits` if that matters.

---

## 10. You need to *see* a chart, not just its SVG

Reading SVG text does not tell you whether a chart looks right — clipped labels, collisions, and bad spacing are invisible in the markup. The loop:

1. Render headless: `context.AsStringAsync(width, height, selfContained: true)` (`selfContained` inlines the theme so the file stands alone).
2. Wrap it in a minimal HTML file — this is also where you can apply page-level `--ggnet-*` overrides to preview a theme. In an **app-hosted** page the override selector must target the `.ggnet` element itself (`.my-card .ggnet { --ggnet-bg: ... }`): the theme declares the variables *on* `.ggnet`, so a plain ancestor rule (`.my-card { --ggnet-bg: ... }`) is silently dead — the element's own declaration beats anything inherited.
3. Screenshot it:
   ```
   "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" \
     --headless --disable-gpu --screenshot=out.png --window-size=W,H --hide-scrollbars file.html
   ```

Note the emitted `<svg>` carries a `viewBox` but **no** `width`/`height`, so it scales to its container. `Width`/`Height` on `Plot` are viewBox units — aspect ratio and label room, not display size.

To exercise the real interactive component instead, run `dotnet run --project src/GGNet.Demo` and screenshot the page (see §1 if it hangs).

---

## 11. Code navigation returns nothing

- `workspaceSymbol` returns empty until the C# language server finishes its first warmup on the solution (~1–2 min). **Retry — do not fall back to grep** and conclude the symbol does not exist.
- `.razor` **markup** is not served by Roslyn. Use text search there. `.razor.cs` is served normally.

---

## 12. A guide contradicts the code

The scoped guides are hand-maintained prose and can lag the source. **The code wins.** When a guide's claim is load-bearing for what you are about to do, verify it against the source first — and when you find drift, fix the guide in the same change rather than working around it.

Places drift is most likely, because they describe fast-moving areas: the interactivity/JS surface, the exported-type counts, and the test-suite totals. Counts and inventories in prose are stale by default; treat `PublicApiTests`' pinned manifest and the tests themselves as the real inventory.
