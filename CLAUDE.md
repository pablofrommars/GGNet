# Claude Code

@AGENTS.md

## Claude-specific Guidance

- The scoped guides in `.github/instructions/` are read on demand, routed by the AGENTS.md table. The `PreToolUse` hook `agent/hooks/guide.sh` enforces it: the first edit of a file each guide governs is blocked once per session with a pointer to that guide; read it, then retry the edit. No guide is loaded at session start.
- Use the tracked `csharp-lsp` plugin (`.claude/lsp-marketplace/`) for C# symbol navigation. Launch Claude Code from the repository root, allow Roslyn to warm up, and retry an initially empty symbol result before falling back to text search.
- Roslyn does not reliably serve `.razor` markup; use text search for markup-only questions.
- The `Stop` hooks run `agent/hooks/format.sh` (a blocking `dotnet format whitespace` gate over `GGNet.slnx`) and `agent/hooks/session.sh` (a completion notification).
- `.claude/settings.json` is the shared project settings authority. `.claude/settings.local.json` is reserved for ignored, machine-local overrides and must not be treated as shared repository policy.
