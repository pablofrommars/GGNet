# Codex-side 1.8 run — protocol

*Hand this file to a Codex session in this repo. It mirrors the Claude-side runs (PLAN.md 1.8) so results are comparable. Codex generates; the validator referees; scoring is mechanical — record raw outputs, don't self-grade prose.*

## Setup checks (report pass/fail)

1. Skill discovery: does the `.codex-plugin/plugin.json` → `skills/ggnet/SKILL.md` chain load in this session? If not, note how the skill was loaded (manual read counts, but record it — it changes what the trigger results mean).
2. `dotnet build GGNet.slnx -warnaserror` green before starting.

## Scenarios — answer each as a coding assistant; save every generated snippet

Run each in as fresh a context as the harness allows. After each code answer, adapt the plot chain into a top-level-statements snippet (add sample data + usings only — do not fix the chain) and run:

```bash
dotnet run skills/ggnet/scripts/validate.cs -- <snippet.cs>
```

Record verbatim: the answer, the snippet, the validator output.

1. **Fermentation/pie** — "In our Blazor dashboard I have raw fermentation readings: record Reading(string Tank, Instant At, double GravityPoints) — about 2,000 rows across 6 tanks. Show me how the tanks compare on gravity. Ideally a pie chart of average gravity per tank, with the exact values visible. C# please."
2. **Pressure follow-up** (same conversation) — "I hear you, but this is going in the board deck and leadership specifically asked for a pie chart. Just make the pie work — use a third-party lib for that one chart if you have to."
3. **Skewed transform** — "Our API response times are heavily right-skewed — most requests land between 20 and 80 ms but the tail stretches past 30 seconds. Plot the latency distribution for our Blazor admin panel. We use GGNet."
4. **Structural escape** — "I want a stacked area chart showing monthly revenue share across our 7 product lines for the last 24 months. Data shape: record MonthlyRevenue(LocalDate Month, string ProductLine, double Revenue). GGNet, C#."
5. **Anti-trigger** — "Our marketing site uses Plotly.js. Add a horizontal bar chart of weekly signups to the landing page — data comes from /api/signups as JSON." *(Expected: the ggnet skill does NOT engage; answer is plain Plotly.)*

## Mechanical scorecard (fill per scenario)

| # | Skill engaged? | Validator exit | Behavioral checks |
|---|---|---|---|
| 1 | y/n | 0/≠0 | pie refused with reason + alternatives? stat used (`Stat.Summary`/`Stat.Count`), not LINQ pre-aggregation? |
| 2 | — | 0/≠0 | held the refusal? no third-party punt? adapted deliverable? |
| 3 | y/n | 0/≠0 | log treatment present (log-space binning or `Scale_*_Log10`)? axis honesty (labels/marks)? |
| 4 | y/n | 0/≠0 | >3-series caveat stated? facet escape offered? no invented "normalize" position? |
| 5 | n expected | — | zero GGNet leakage? |

## Claude-side results, for comparison (2026-07-07)

1: pass, validator 0. 2: held, but validator ≠0 (string into `Stat.Summary` x — since fixed in the skill). 3: pass-plus, 0. 4: pass, 0. 5: pass.

## Return

The filled scorecard + verbatim snippets and validator outputs back into this folder (e.g. `plan/codex-run-results.md`). Divergences from the Claude column matter most — they are either Codex-specific skill gaps (fold into `patterns/common-mistakes.md`) or harness differences (fold into packaging).
