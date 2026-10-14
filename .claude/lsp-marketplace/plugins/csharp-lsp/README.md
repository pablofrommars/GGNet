# csharp-lsp

Local Claude Code plugin that wires the C# (Roslyn) language server into Claude
Code's built-in LSP tool. Gives Claude go-to-definition, find-references, hover
type info, and automatic post-edit diagnostics on `.cs` files — the fastest way
to surface the `-warnaserror` violations and stray culture-sensitive formatting
that this codebase gates on.

## Prerequisites

The plugin only configures the *connection*. The server binary must resolve on
its own:

- **.NET 11 preview SDK** (`11.0.100-preview.6.26359.118`, pinned in
  `global.json`). Verify the active SDK with `dotnet --version`.
- `ClaudeCodeRoslynLspProxy` and `roslyn-language-server` must both be installed
  and available on `PATH`. Verify them with
  `command -v ClaudeCodeRoslynLspProxy` and
  `command -v roslyn-language-server`.

The checked-in plugin does not install or download either tool. If `/plugin`
shows `Executable not found in $PATH`, the named command is unavailable in the
Claude Code process environment.

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
  workspace root + `--autoLoadProjects`. Do not add a checkout-specific
  `--solution` path: it silently redirects worktree navigation to another tree —
  and GGNet is checked out as a submodule inside its consumers, so an absolute
  path is wrong in every copy but one.

The standalone `.lsp.json` format is a bare map of `serverName -> config`
(no `lspServers` wrapper). The wrapper is only used when the block is inlined
inside `plugin.json`. `plugin.json` here points at this file via
`"lspServers": "./.lsp.json"`.

## Known caveats for this stack

- **Razor markup is partial.** Roslyn handles `.razor.cs` code-behind (plain C#).
  The `.razor` / `.cshtml` markup mappings register the extensions, but Roslyn
  alone does not serve markup — that needs a separate Razor server (`rzls`),
  which is not bundled here. Logic in `.razor.cs` and the shared core is fully
  covered; `.razor` markup falls back to text search.
- **`.slnx` solutions.** `--autoLoadProjects` discovers projects directly rather
  than parsing the solution file, so the XML solution format is fine. If
  resolution looks incomplete across project boundaries, that flag is the first
  thing to check.
- **Grep-first default.** Claude Code's built-in system prompt steers toward
  Grep/Glob even when LSP is available. The navigation table in `AGENTS.md` is
  what pushes back on that; expect to reinforce it in review.
