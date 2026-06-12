---
name: "AlFaris | الفارس"
description: "A clear, efficient Arabic-first management interface for factory leadership."
colors:
  action-blue: "#2563EB"
  action-blue-deep: "#1D4ED8"
  operational-navy: "#0B1220"
  slate-ink: "#0F172A"
  slate-body: "#475569"
  slate-muted: "#64748B"
  slate-soft: "#94A3B8"
  slate-border: "#E2E8F0"
  app-background: "#F1F5F9"
  surface-subtle: "#F8FAFC"
  surface: "#FFFFFF"
  positive: "#15803D"
  negative: "#B91C1C"
typography:
  headline:
    fontFamily: "Cairo, Segoe UI, Tahoma, sans-serif"
    fontSize: "1.5rem"
    fontWeight: 700
    lineHeight: 1.4
    letterSpacing: "-0.02em"
  title:
    fontFamily: "Cairo, Segoe UI, Tahoma, sans-serif"
    fontSize: "1.15rem"
    fontWeight: 700
    lineHeight: 1.5
    letterSpacing: "normal"
  body:
    fontFamily: "Cairo, Segoe UI, Tahoma, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.6
    letterSpacing: "normal"
  label:
    fontFamily: "Cairo, Segoe UI, Tahoma, sans-serif"
    fontSize: "0.82rem"
    fontWeight: 600
    lineHeight: 1.5
    letterSpacing: "normal"
rounded:
  control: "8px"
  navigation: "10px"
  surface: "14px"
  prominent: "16px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "20px"
  page: "32px"
components:
  button-primary:
    backgroundColor: "{colors.action-blue}"
    textColor: "{colors.surface}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "10px 16px"
  button-primary-hover:
    backgroundColor: "{colors.action-blue-deep}"
    textColor: "{colors.surface}"
  button-ghost:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.slate-body}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "10px 12px"
  input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.slate-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "10px 12px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.slate-ink}"
    rounded: "{rounded.surface}"
    padding: "16px"
  navigation-active:
    backgroundColor: "{colors.action-blue}"
    textColor: "{colors.surface}"
    typography: "{typography.body}"
    rounded: "{rounded.navigation}"
    padding: "11px 14px"
---

# Design System: AlFaris | الفارس

## Overview

**Creative North Star: "The Management Desk"**

AlFaris should feel like a well-organized management desk: important records are visible, controls are where experienced users expect them, and every visual decision supports a concrete operational task. The system is Arabic-first and right-to-left, with Cairo typography, a bright working surface, and a stable dark navigation frame.

The interface is clear, efficient, simple, and professional. Density is purposeful rather than sparse: grids, reports, forms, and charts can carry substantial information, but strong grouping and restrained emphasis keep them readable. PrimeNG supplies familiar interaction patterns while custom styles maintain a consistent AlFaris identity.

Generic AI-generated styling is explicitly rejected. Decorative glow effects, glass surfaces, excessive gradients, oversized presentation cards, and ornamental dashboard elements are forbidden. Motion communicates state only and must respect reduced-motion preferences.

**Key Characteristics:**
- Arabic-first RTL structure and Cairo typography.
- Restrained slate surfaces with blue reserved for actions and selection.
- Compact, familiar controls suited to repeated management work.
- Clear hierarchy through spacing, borders, and typography before shadow.
- Responsive structures that preserve task order and keyboard usability.

## Colors

The Clear Slate palette combines a calm working canvas with Operational Navy for orientation and Action Blue for decisive interaction.

### Primary
- **Action Blue:** Reserved for primary actions, active navigation, focus emphasis, links, and selected states. It is functional, never decorative.
- **Action Blue Deep:** Used for hover and pressed states where a stronger interaction response is required.

### Secondary
- **Operational Navy:** Anchors the application shell and login identity surface. It provides orientation without turning the content area into a dark theme.

### Neutral
- **Slate Ink:** Default high-contrast text and important numeric content.
- **Slate Body:** Secondary explanatory text and table headings.
- **Slate Muted:** Metadata, helper text, and low-priority labels only when contrast remains WCAG 2.2 AA compliant.
- **Slate Soft:** Disabled and tertiary icon treatment; never body copy on a light surface.
- **Slate Border:** Structural dividers and container boundaries.
- **App Background:** The main application canvas behind working surfaces.
- **Surface Subtle:** Table headers, hover rows, and quiet grouped regions.
- **Surface:** Forms, grids, dialogs, charts, and primary content containers.
- **Positive / Negative:** Semantic financial and status feedback. Always pair color with text, an icon, or another non-color cue.

### Named Rules

**The Action Rarity Rule.** Action Blue should occupy less than 10% of a typical screen. Its scarcity preserves meaning.

**The Working Surface Rule.** Operational content remains on light surfaces for sustained daytime use; dark color belongs to navigation and identity framing.

**The No Glow Rule.** Glow effects are forbidden. Focus must use a crisp outline or border change, never luminous decoration.

## Typography

**Display Font:** Cairo (with Segoe UI, Tahoma, and sans-serif fallbacks)  
**Body Font:** Cairo (with Segoe UI, Tahoma, and sans-serif fallbacks)

**Character:** One Arabic-capable family provides a stable, unified voice across labels, numbers, forms, tables, and headings. Weight and size create hierarchy without introducing a decorative display face.

### Hierarchy
- **Headline** (700, 1.5rem, 1.4): Page titles and major dialog headings. Keep letter spacing no tighter than -0.02em.
- **Title** (700, 1.15rem, 1.5): Grid titles, chart titles, and section headings.
- **Body** (400, 1rem, 1.6): Primary interface copy and form values. Explanatory prose should remain within 65-75 characters per line.
- **Label** (600, 0.82rem, 1.5): Form labels, compact metadata, and table support text. Labels remain in natural Arabic case with no artificial tracking.

### Named Rules

**The One-Family Rule.** Use Cairo throughout the product UI. Do not introduce display or Latin-first fonts for visual novelty.

**The Arabic Rhythm Rule.** Preserve comfortable Arabic line height and natural RTL reading order; never compress labels to imitate dense Latin dashboards.

## Elevation

AlFaris is flat and structured by default. Background contrast, full borders, and spacing establish hierarchy. Low ambient shadows may separate a major working surface from the application canvas; stronger shadows are limited to active navigation or temporary overlays. Borders and broad shadows must not be combined as decoration.

### Shadow Vocabulary
- **Surface Low** (`0 1px 3px rgba(15, 23, 42, 0.06)`): Grids and chart containers on the application canvas.
- **Active Navigation** (`0 4px 12px rgba(37, 99, 235, 0.35)`): Active sidebar destination only.

### Named Rules

**The Flat-by-Default Rule.** A resting component uses either a structural border or a low shadow. It never uses a border plus a wide decorative shadow.

**The State Elevation Rule.** Elevation indicates hierarchy or interaction state, not prestige or visual richness.

## Components

Components are restrained, familiar, and task-focused. PrimeNG behavior remains intact; custom styling should standardize the AlFaris vocabulary rather than reinvent standard controls.

### Buttons
- **Shape:** Compact gently curved controls using the control radius.
- **Primary:** Action Blue background, white text, medium-bold label, and balanced horizontal padding.
- **Hover / Focus:** Deepen the blue on hover. Use a visible high-contrast `:focus-visible` outline with a clear offset. State transitions remain between 150ms and 200ms.
- **Secondary / Ghost:** Quiet neutral text or transparent treatments for cancel, utility, and row actions. Destructive actions use semantic red with an explicit Arabic tooltip or label.

### Chips
- **Style:** Compact PrimeNG tags use soft semantic backgrounds and readable text. Rounded tags are acceptable because they identify status rather than contain primary actions.
- **State:** Status meaning must be written in Arabic and must not depend on hue alone.

### Cards / Containers
- **Corner Style:** Standard working surfaces use the surface radius; prominent summary containers may use the prominent radius but never exceed it.
- **Background:** White for primary working surfaces; Surface Subtle for headers, hover rows, and quiet subdivisions.
- **Shadow Strategy:** Flat or Surface Low only.
- **Border:** Use a full Slate Border when a boundary is needed. Colored side stripes are prohibited.
- **Internal Padding:** Usually 16px, increasing to 20px only for prominent page-level sections.

### Inputs / Fields
- **Style:** White background, clear neutral stroke, control radius, full-width alignment within forms, and persistent Arabic labels above controls.
- **Focus:** Crisp blue border or outline with sufficient contrast. Never use glow.
- **Error / Disabled:** Show an Arabic message near the field and use semantic color plus iconography or text. Disabled controls must remain legible and visibly unavailable.

### Navigation

The sidebar uses Operational Navy with cool slate labels. Active destinations use Action Blue, white text, and the navigation radius. Hover is a subtle light overlay; inactive items remain quiet. On narrow screens, navigation changes structure rather than shrinking text, and reading plus focus order must remain logical in RTL.

### Data Grid

The shared grid is the system's signature working component. It combines a compact toolbar, global and column filtering, sortable headers, column management, export actions, row actions, loading feedback, empty states, errors, and pagination. Preserve numeric alignment and tabular figures, keep headers visually distinct from rows, and ensure horizontal scrolling does not hide essential row actions without a deliberate responsive treatment.

## Do's and Don'ts

### Do:
- **Do** reserve Action Blue for primary actions, active navigation, links, focus, and selection.
- **Do** use Cairo consistently and preserve Arabic-first RTL reading, navigation, and form order.
- **Do** use familiar PrimeNG interaction behavior while standardizing spacing, radius, state, and copy.
- **Do** use borders, spacing, and surface contrast before adding elevation.
- **Do** provide hover, focus-visible, active, disabled, loading, empty, and error states for every interactive workflow.
- **Do** meet WCAG 2.2 AA contrast, keyboard navigation, screen-reader, and reduced-motion requirements.
- **Do** pair semantic colors with Arabic text, icons, or another non-color indicator.

### Don't:
- **Don't** use interfaces that visibly resemble generic AI-generated designs.
- **Don't** use decorative glow effects, glassmorphism, or luminous focus styling.
- **Don't** use excessive gradients, ornamental dashboard decoration, or visual novelty that competes with operational data.
- **Don't** inflate ordinary content into repeated oversized cards or nest cards inside cards.
- **Don't** combine a `1px` border with a wide soft shadow on the same component.
- **Don't** exceed a 16px radius on cards, sections, or inputs.
- **Don't** use colored side-stripe borders, gradient text, or repeating stripe backgrounds.
- **Don't** use decorative motion or orchestrated page-load sequences; transitions must communicate state and honor reduced motion.
- **Don't** rely on color alone for status, financial direction, validation, or selection.
- **Don't** hide complexity behind unfamiliar controls when a standard management-software pattern exists.
