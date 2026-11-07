# ContextSketch: design overview

ContextSketch is a small Windows desktop tool for sketching **system context diagrams**: one system in the middle, the people and systems around it. A diagram takes under a minute to make, saves as readable JSON and exports to SVG.

`SPEC.md` is the full requirements document. This page explains the ideas behind it.

## 1. Tech stack

| Layer | Choice | Why |
|---|---|---|
| Runtime | **.NET 10** (current LTS), C# 14 | Supported until late 2028. .NET 8 reaches end of support on 10 Nov 2026. |
| UI | **WPF** (`net10.0-windows`) | Mature, binding-friendly and Windows-only, which is all this tool needs. |
| MVVM | **CommunityToolkit.Mvvm** | Source generators (`[ObservableProperty]`, `[RelayCommand]`) keep view models short. |
| Core library | Plain `net10.0` class library, no dependencies | Model, validation, JSON, layout, geometry and SVG export, none of it touching UI. |
| Serialization | `System.Text.Json` | Built in; camelCase and indented output for clean diffs. |
| SVG | `System.Xml.Linq` | Built in; handles escaping and well-formedness. |
| Tests | **xUnit** on the core library | Everything that matters is testable without a window. |
| IDE | **Visual Studio 2026** (or the .NET 10 SDK with VS Code / Rider) | The solution is a classic `.sln`, so any of them open it. |

**Current state:** the projects target `net8.0` / `net8.0-windows` with `RollForward=Major`, so they run on the installed .NET 10 runtime. To move to the stack above, change the `TargetFramework` in the three `.csproj` files to `net10.0` / `net10.0-windows`. Keep `LangVersion` at 12 unless you want newer syntax.

### Shape of the code

```
ContextSketch.sln
  src/ContextSketch.Core/    model, validation, JSON, auto-layout, geometry, SVG export
  src/ContextSketch.App/     WPF views, view models, file dialogs
  tests/ContextSketch.Core.Tests/
```

The view models wrap the core model and write straight through to it. The XAML draws everything from bindings, so the only code-behind is canvas input (pan, zoom, click, drag).

## 2. The sketching idea (UX)

The aim is to get from nothing to a tidy diagram without ever thinking about layout.

1. **A new diagram starts with one system** in the centre. It can be renamed but not deleted.
2. **Add things with one click.** Toolbar buttons add a primary actor, secondary actor, external system or storage. Each new element gets a default name, is placed automatically and is selected.
3. **The link to the system is automatic.** Every element has exactly one relationship, with the system, created when it is added. Fill in a label (a plain sentence, e.g. "Places orders") and a direction (one-way or two-way) in the properties panel. Relationships carry no technology: that belongs to the elements. There is no connect mode.
4. **Layout is automatic, with an escape hatch.** Primary actors line up on the left, everything else on the right. Drag an element anywhere and it is *pinned*: auto-layout leaves it alone from then on. **Auto-layout** re-arranges the unpinned elements, and **Auto-layout all** releases every pin first.
5. **Editing is direct.** Click to select, drag to move, double-click an element to rename it, Delete to remove it (and its line). Drag the background to pan and use the mouse wheel to zoom.
6. **Files are plain.** Save and Open use `.contextsketch.json`. The title bar shows `*` when there are unsaved changes, and New, Open and Close ask before discarding work. **Export SVG** produces a standalone image cropped to the content.

A faint dashed line down the middle of the canvas reinforces the left/right split. It is on screen only and never appears in the SVG.

## 3. The diagram scheme

### Reading a diagram

A context diagram answers one question: *who and what does this system touch?* The left/right split carries the main idea.

- **Left: who starts things.** Primary actors are people, roles or systems that initiate an interaction with the system.
- **Right: what the system calls.** Secondary actors, external systems and storage only take part because the system reaches out to them.

| Element | Meaning | Zone | Look |
|---|---|---|---|
| `system` | The system being designed (exactly one) | Centre | Large white rectangle with a heavy ink outline |
| `primaryActor` | Initiates interactions | Left | Slate box with a person glyph |
| `secondaryActor` | Called by the system (e.g. a payment provider) | Top right | Same glyph, near-white fill, dashed slate outline |
| `externalSystem` | Other software the system integrates with | Middle right | Plain light slate rectangle |
| `storage` | Database, file store, queue | Bottom right | Slate cylinder |

Everything around the system is drawn in shades of slate, so the system's ink outline is what the eye lands on. The selection highlight is orange and appears on screen only. All colours live in `DiagramStyle`, which feeds both the canvas and the SVG export.

An element's **technology** is shown as plain, smaller, slightly faded text under its name (no brackets). For a primary actor it says how they reach the system (e.g. `WPF`, `Web browser`, `CLI`). Relationship labels are plain sentences, wrapped to fit their own arrow (a longer arrow gets wider lines, between 12 and 40 characters) over at most three lines, with "…" if still too long. Screen and SVG wrap them the same way.

The primary/secondary split is information for the reader. The app does not enforce who initiates what, and a relationship's arrow direction is independent of it.

### Layout rules

The system sits at the origin. Primary actors form one column on the left, centred vertically on the system and growing taller rather than wider. On the right, groups stack top to bottom (secondary actors, external systems, storage), centred on the system, and a group with more than five elements wraps into a second column further right. The gap between the system and each side is normally 140, but widens (up to 300) when a label on that side would otherwise be cut; the layout reruns after a label is edited. Order follows creation order, pinned elements are never moved, and the same input always gives the same positions.

### File format

Diagrams are JSON with camelCase names and a stable order, so they are readable, hand-editable and diff-friendly.

```json
{
  "schemaVersion": 1,
  "title": "Online Shop",
  "elements": [
    { "id": "sys", "type": "system", "name": "Shop Platform", "x": 0, "y": 0, "pinned": false },
    { "id": "cust", "type": "primaryActor", "name": "Customer", "technology": "Web browser", "x": -345, "y": 0, "pinned": false },
    { "id": "db", "type": "storage", "name": "Orders DB", "technology": "PostgreSQL", "x": 345, "y": 180, "pinned": false }
  ],
  "relationships": [
    { "id": "r1", "from": "cust", "to": "sys", "label": "Places and tracks orders", "direction": "oneWay" },
    { "id": "r2", "from": "sys", "to": "db", "label": "Reads and writes orders", "direction": "twoWay" }
  ]
}
```

- **Elements** have an `id`, `type`, `name`, optional `description` and `technology`, a centre position (`x`, `y`) and a `pinned` flag.
- **Relationships** have an `id`, `from` and `to` element ids, a `label` and a `direction` of `oneWay` or `twoWay`. A `technology` on a relationship, found in older files, is ignored on load and dropped on the next save.
- **Positions are optional on load.** A file without them is laid out automatically, so a hand-written file needs no coordinates.

### Rules checked on load

A bad file is reported with every problem listed, and the current diagram is left untouched:

- the schema version must be supported;
- ids must be unique across elements and relationships;
- there must be exactly one `system`;
- every element needs a name and a known type;
- relationships must refer to existing elements, must not link an element to itself, and must have the system at one end;
- every non-system element must have exactly one relationship.
