---
target: dashboard
total_score: 15
p0_count: 0
p1_count: 4
timestamp: 2026-06-12T12-12-53Z
slug: web-src-app-features-dashboard-dashboard-html
---
#### Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 1/4 | Chart-list, chart-data, delete, and reorder failures are hidden or look like loading/empty states. |
| 2 | Match System / Real World | 2/4 | Arabic language is appropriate, but the page exposes chart mechanics rather than management questions. |
| 3 | User Control and Freedom | 2/4 | Dialog cancellation exists; reorder lacks undo and delete relies on a basic confirmation. |
| 4 | Consistency and Standards | 3/4 | PrimeNG and shared styling are coherent, with minor departures from the documented restrained system. |
| 5 | Error Prevention | 1/4 | Reordering issues multiple immediate updates without dependable rollback or atomic persistence. |
| 6 | Recognition Rather Than Recall | 2/4 | Titles and navigation are visible, but icon-only actions and unexplained metadata weaken recognition. |
| 7 | Flexibility and Efficiency | 1/4 | No keyboard reorder, global period control, drill-through, shortcuts, or management accelerators. |
| 8 | Aesthetic and Minimalist Design | 2/4 | Restrained overall, but all charts have equal weight and chart-type badges add low-value noise. |
| 9 | Error Recovery | 1/4 | Errors are swallowed; users receive no diagnosis, retry, or reliable recovery path. |
| 10 | Help and Documentation | 0/4 | No metric definitions, contextual guidance, chart explanation, or visible help. |
| **Total** | | **15/40** | **Poor** |

#### Anti-Patterns Verdict

**LLM assessment:** Moderate template residue, not blatant AI slop. Cairo typography, compact PrimeNG controls, and the restrained palette avoid the worst generic AI styling. The repeated equal-weight chart cards, decorative chart-type badges, arbitrary palettes, and lack of factory-specific management framing still make the page feel like a configurable chart gallery rather than an intentional executive dashboard.

**Deterministic scan:** `detect.mjs` returned exit code 0 with zero findings for `web/src/app/features/dashboard/dashboard.html`. No banned markup patterns, rule hits, locations, or false positives were reported. The scan cannot detect workflow hierarchy, asynchronous failure handling, canvas accessibility, or decision usefulness, which account for the major findings below.

**Visual overlays:** No reliable user-visible overlay was produced. Browser automation was unavailable (`playwright` module absent and no browser tool exposed), and no dashboard server responded on ports 4200, 4201, 5173, or 3000. Source inspection and the clean deterministic scan were used as fallback evidence.

#### Overall Impression

The dashboard has a calm, credible product shell but does not yet function as a senior-management decision surface. Its biggest opportunity is to replace equal-weight chart presentation with prioritized operational meaning: what changed, what needs attention, how current the data is, and where to investigate next.

#### What's Working

- Arabic-first RTL labels, Cairo typography, and Arabic chart tooltip formatting are consistently considered.
- Permission-gated create, edit, and delete controls keep the ordinary viewer experience quieter.
- Slate surfaces, modest radii, and low elevation broadly match the documented restrained product identity.

#### Priority Issues

**[P1] Failures masquerade as empty or loading states**

- **Why it matters:** A failed list request can look like “no charts,” while a failed data request can leave a permanent ellipsis. Senior managers cannot distinguish no data from broken data and may make decisions from an incomplete dashboard.
- **Fix:** Model list and per-chart loading, success, empty, and error states explicitly. Add Arabic retry controls, `aria-live` announcements, operation success feedback, and optimistic rollback.
- **References:** `dashboard.html:10`, `dashboard.ts:44`, `dashboard.ts:67`.
- **Suggested command:** `$impeccable harden dashboard`

**[P1] Reordering is mouse-only, unreliable, and exposed to read-only users**

- **Why it matters:** Every chart is draggable even without management permission. Native drag-and-drop has no keyboard path, placement affordance, undo, or atomic save.
- **Fix:** Enable ordering only in an explicit management mode. Add keyboard move controls, visible placement feedback, one atomic reorder request, confirmation, and rollback.
- **References:** `dashboard.html:17`, `dashboard.ts:72`.
- **Suggested command:** `$impeccable harden dashboard reorder`

**[P1] The dashboard does not frame management decisions**

- **Why it matters:** Every chart has equal prominence, with no reporting period, comparison, target, anomaly, freshness timestamp, or route to underlying records. The page cannot answer “What needs attention today?”
- **Fix:** Lead with no more than four decision metrics showing period, comparison, and state. Group supporting charts by management question and add Arabic drill-through actions to the relevant records.
- **References:** `dashboard.html:1`, `shared/chart/chart.ts:24`.
- **Suggested command:** `$impeccable shape executive dashboard hierarchy`

**[P1] Charts lack an accessible textual equivalent**

- **Why it matters:** Canvas charts are not enough for screen-reader, keyboard-only, low-vision, or color-vision users. Important factory data becomes inaccessible.
- **Fix:** Add descriptive accessible names, concise Arabic insights, and a screen-reader-accessible data table or “عرض البيانات” disclosure. Never rely on color alone.
- **Reference:** `shared/chart/chart.ts:12`.
- **Suggested command:** `$impeccable audit dashboard accessibility`

**[P2] Responsive behavior risks overflow and navigation overload**

- **Why it matters:** The 380px chart minimum plus page padding can overflow small screens, the dialog is fixed at 560px, and ten navigation items become a wrapping mobile header.
- **Fix:** Use `minmax(min(100%, 380px), 1fr)`, responsive dialog sizing, reduced mobile padding, and a dismissible navigation drawer.
- **References:** `dashboard.scss:1`, `dashboard.html:43`, `shell.scss:53`.
- **Suggested command:** `$impeccable adapt dashboard`

#### Persona Red Flags

**Alex (Power User)**

- No keyboard reorder or dashboard shortcuts.
- No global reporting-period or filter control.
- No drill-through from charts to source records.
- Reorder causes several background requests instead of one dependable save.

**Sam (Accessibility-Dependent User)**

- Canvas charts have no textual equivalent.
- Edit and delete controls are icon-only without explicit accessible labels.
- Drag-and-drop has no keyboard alternative.
- Loading and failure changes are not announced.

**Factory Senior Manager**

- Cannot determine whether figures are current, improving, or outside target.
- No clear answer to “What needs my attention today?”
- Chart type and palette configuration receive more emphasis than thresholds and follow-up actions.
- Equal card weighting makes urgent and routine information look equivalent.

#### Minor Observations

- The chart-type tag repeats information already visible in the chart.
- Line smoothing and area fill may imply precision unsupported by sparse data.
- Sidebar and active-navigation gradients depart from the documented solid-color restraint.
- The empty state should explain how authorized users add a chart and what viewers should expect.
- The global custom scrollbar conflicts with the product register's preference for standard affordances.

#### Questions to Consider

- What are the three decisions a senior manager must make from this page before noon?
- Should chart palettes be configurable, or should color be governed centrally by operational meaning?
- Is this intended to be a dashboard, or a configurable chart gallery?
- What should the page say when everything is normal, and what must become impossible to miss when it is not?
