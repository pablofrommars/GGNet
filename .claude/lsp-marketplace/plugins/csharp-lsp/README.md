# csharp-lsp

Local Claude Code plugin that wires the C# (Roslyn) language server into Claude
Code's built-in LSP tool. Gives Claude go-to-definition, find-references, hover
type info, and automatic post-edit diagnostics on `.cs` files — the fastest way
to surface the `-warnaserror` violations and stray culture-sensitive formatting
that this codebase gates on.

## Prerequisites

The plugin only configures the *connection*. The server binary must resolve on
its own:

- **.NET 11 preview SDK** (pinned via `global.json`). `dnx` ships with it.
  Verify: `dnx --version`.
- First launch runs `dnx roslyn-language-server --yes --prerelease`, which pulls
  the server package from NuGet. Confirm it resolves once at a terminal before
  relying on it inside Claude Code:
  `dnx roslyn-language-server --yes --prerelease -- --version`

If `/plugin` shows `Executable not found in $PATH`, `dnx` isn't on PATH — that's
a runtime problem, not a config one.

## Schema notes (why this differs from snippets you may find online)

- **`extensionToLanguage`, not `fileExtensions`.** This is the field name in the
  official Claude Code plugin LSP schema. `fileExtensions` is a non-canonical
  variant that Claude Code's LSP tool ignores, producing
  `No LSP server available for file type: .cs`.
- **`startupTimeout`, not `warmupTimeoutMs`.** Same reason. 120s is deliberately
  generous — Roslyn indexes the full solution on first load.
- **No `cwd`.** `cwd` is not part of the documented field set, and omitting it is
  also *more correct* here: the server launches with the working directory at the
  repo root where Claude Code runs, and Roslyn discovers projects from the LSP
  workspace root + `--autoLoadProjects`. If you find cross-project symbol
  resolution is incomplete, add `"cwd": "${PLUGIN_ROOT}"` back and retest, but
  start without it.

The standalone `.lsp.json` format is a bare map of `serverName -> config`
(no `lspServers` wrapper). The wrapper is only used when the block is inlined
inside `plugin.json`. `plugin.json` here points at this file via
`"lspServers": "./.lsp.json"`.

## Known caveats for this stack

- **`--solution` is an absolute path.** It points at `GGNet.slnx` at this repo's
  checkout location. If the repo moves, update the path in `.lsp.json`.
- **Razor markup is partial.** Roslyn handles `.razor.cs` code-behind (plain C#).
  The `.razor` mapping registers the extension, but Roslyn alone does not serve
  markup — that needs a separate Razor server (`rzls`), which is not bundled
  here. Logic in `.razor.cs` and the shared core is fully covered; `.razor`
  markup falls back to Grep.
- **`.slnx` solutions.** `--autoLoadProjects` discovers projects directly rather
  than parsing the solution file, so the XML solution format is fine. If
  resolution looks incomplete across project boundaries, that flag is the first
  thing to check.
- **Grep-first default.** Claude Code's built-in system prompt steers toward
  Grep/Glob even when LSP is available. The `CLAUDE.md` navigation block is what
  pushes back on that; expect to reinforce it in review.
