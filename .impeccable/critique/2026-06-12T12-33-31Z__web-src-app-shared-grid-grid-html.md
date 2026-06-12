---
target: all grid pages
total_score: 23
p0_count: 0
p1_count: 3
timestamp: 2026-06-12T12-33-31Z
slug: web-src-app-shared-grid-grid-html
---
#### Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2/4 | Grid/export states are clear; CRUD outcomes remain mostly silent. |
| 2 | Match System / Real World | 4/4 | Natural Arabic terminology and familiar management patterns. |
| 3 | User Control and Freedom | 2/4 | Cancel, retry, and reset exist; no undo or bulk workflows. |
| 4 | Consistency and Standards | 3/4 | Strong shared-grid consistency; expense scope filters remain separate. |
| 5 | Error Prevention | 2/4 | Basic constraints exist, but validation and date-range guards are incomplete. |
| 6 | Recognition Rather Than Recall | 3/4 | Controls are visible and actions have tooltips; some icon-only discovery remains. |
| 7 | Flexibility and Efficiency | 2/4 | Column controls and pagination help; no batch actions or accelerators. |
| 8 | Aesthetic and Minimalist Design | 3/4 | Restrained and purposeful; toolbar hierarchy could be clearer. |
| 9 | Error Recovery | 1/4 | Grid failures recover well, but mutation failures are often silent. |
| 10 | Help and Documentation | 1/4 | Tooltips only; advanced grid behavior has no contextual guidance. |
| **Total** | | **23/40** | **Acceptable** |

#### Anti-Patterns Verdict

**LLM assessment:** Pass. The grid pages use deliberate product restraint: compact PrimeNG controls, Arabic-first copy, stable surfaces, restrained color, and no decorative AI clichés. They may feel familiar, but operational familiarity is appropriate here.

**Deterministic scan:** The exact detector scan returned exit code 0 and `[]` across the shared grid, Clients, Expenses, and Todos templates. No rules, locations, or false positives were reported.

**Visual overlays:** No overlay was produced. The Angular server was available at `http://localhost:4200/`, but no callable browser automation tool existed in this session. Source review and detector output were used as fallback evidence.

#### Overall Impression

The polish pass materially improved clarity, responsiveness, empty/error handling, export feedback, and keyboard-oriented grid controls. The largest remaining weakness is uneven trust: reading and exporting feel reliable, while saving, deleting, and changing business records can still fail or succeed without clear feedback.

#### What's Working

- One coherent grid vocabulary serves Clients, Expenses, and Todos.
- Empty and grid-error states are specific, actionable, and written naturally in Arabic.
- Accessibility intent is visible in sorting labels, search labels, focus styles, tooltips, reduced motion, and mobile target sizing.

#### Priority Issues

**[P1] Form labels are not programmatically connected to controls**

- **Why it matters:** Screen-reader and voice-control users cannot reliably associate labels with dialog and report inputs.
- **Fix:** Add stable native IDs or PrimeNG `inputId` values and matching `<label for>`. Connect inline validation with `aria-describedby`.
- **References:** `clients.html:26`, `expenses.html:6`, `todos.html:33`.
- **Suggested command:** `$impeccable audit grid forms accessibility`

**[P1] CRUD success and failure feedback is inconsistent**

- **Why it matters:** Saving, deleting, changing status, or toggling a client can appear to do nothing. That is unsafe for financial and operational records.
- **Fix:** Preserve dialogs on failure, show actionable inline errors with alert semantics, and provide concise success toasts for every mutation.
- **References:** `clients.ts:88`, `todos.ts:73`, `expenses.ts:3`.
- **Suggested command:** `$impeccable harden grid page mutations`

**[P1] Today's Tasks becomes stale after mutations**

- **Why it matters:** The top summary can disagree with the authoritative grid after save, status change, or delete.
- **Fix:** Centralize Todo refresh so both the grid and Today panel reload after relevant mutations.
- **Reference:** `todos.ts:93`.
- **Suggested command:** `$impeccable harden todos synchronization`

**[P2] Row actions can move outside the horizontal viewport**

- **Why it matters:** Wide tables can hide essential edit/delete/status actions, especially on Clients.
- **Fix:** Make the RTL action column sticky with an opaque surface, divider, and semantic z-index, or introduce a deliberate compact mobile action pattern.
- **References:** `grid.html:9`, `grid.scss:37`.
- **Suggested command:** `$impeccable adapt grid row actions`

**[P2] Text column filters request on every keystroke**

- **Why it matters:** Fast typing generates unnecessary API traffic and unstable loading feedback despite cancellation.
- **Fix:** Debounce text column filters while keeping enum/select filters immediate.
- **References:** `grid.html:83`, `grid.ts:137`.
- **Suggested command:** `$impeccable optimize grid filtering`

#### Persona Red Flags

**Alex (Power User)**

- No bulk selection or batch operations.
- Repeated edits remain one row at a time.
- No keyboard accelerators for create, search focus, or export.

**Sam (Accessibility-Dependent User)**

- Dialog and report labels are not associated with controls.
- CRUD outcomes are not announced consistently.
- Desktop column movement controls are visually small.
- Row actions may require horizontal scrolling to discover.

**Factory Senior Manager**

- Silent expense or client mutation failures undermine record confidence.
- Expense filters permit an inverted date range without prevention or explanation.
- Todo status cycling does not reveal the destination status before activation.

#### Minor Observations

- Excel and PDF would scan better under one labeled Export action.
- “جديد” should be page-specific, such as “عميل جديد” or “مهمة جديدة”.
- Expense scope filters need a visible reset action.
- Loading could use stable row skeletons instead of only PrimeNG's generic loading layer.
- Column customization is permanently present even if most users rarely reorder columns.

#### Questions to Consider

- Is column reordering frequent enough to occupy every header permanently?
- Should executive users receive batch workflows, or should these pages emphasize review and exception handling?
- Why does export provide stronger feedback than changing business data?
- Is cycling Todo status understandable behavior, or implementation convenience?
