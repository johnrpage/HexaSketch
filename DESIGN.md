# HexaSketch: design overview (v1)

HexaSketch turns a C4 system context diagram (from Context Sketch) into a **ports-and-adapters sketch**, then exports it as an **SVG diagram** and a **Markdown solution plan** a coding agent can build stubs from. It follows ContextSketch's spirit (`ContextSketch-DESIGN.md`): small, fast, plain JSON files, layout handled for you.

`ARCHITECTURE.md` describes the code. This page records what v1 does and why.

## Running it

```
dotnet run --project src/HexaSketch.Wpf
dotnet run --project src/HexaSketch.Wpf -- "HexaSketch - Context Diagram.contextsketch.json"
dotnet test
```

A file named on the command line is opened: a `.contextsketch.json` starts a sketch, anything else is opened as a sketch.

## The diagram

- A **pointy-topped hexagon** in the middle, with a faint dashed line down the centre of the canvas (behind it) separating **driving (left)** from **driven (right)**.
- **Domain** is a circle at the centre. **Tasks** and **Queries** fill the space between Domain and the hexagon's left edge, split by a dashed line: the User reaches the Domain only through them.
- **Ports** are circles on stalks, placed radially. The **User port** is always present, sits at nine o'clock, and has a heavier outline and stalk. Other driving ports alternate above and below it; driven ports spread evenly down the right, top to bottom.
- **Adapters** hang off their port in a column, linked by a fan of lines. **Real** adapters are solid and filled; **Fake** and **Simulated** ones are dashed. The second line shows the technology (and the kind, if not Real).
- Layout is automatic. You can reorder (drag a port onto another port on the same side, or an adapter onto another adapter) but not place things freely.

## Sketching

1. **New from context diagram** reads the CD. The system becomes the hexagon, the User port is created, and **each primary actor becomes a Real adapter on the User port** (technology from its relationship, e.g. WPF).
2. Every other CD item waits in the **list on the left**. **Drag one onto any port** (driving or driven, no restrictions) and it becomes an adapter there, leaving the list. One CD item makes one adapter. **Drop one on the hexagon** (or near its edge) to make a new port named after it, with the item as its adapter: driving if dropped on the left half, driven on the right.
3. **Add driving port / Add driven port** add ports with default names, ready to rename. **Add adapter** adds a hand-made adapter (no CD source). A port's right-click menu adds a real, fake or simulated one; fakes and simulators are named after their kind.
4. Adapters can be dragged to other ports (including off the User port). Dragging an adapter back onto the list, or deleting it, returns its CD item to the list.
5. **Deleting a port** asks first if it has adapters; CD-sourced adapters return to the list, hand-made ones are deleted. The User port can't be deleted.
6. **Properties panel**: port name and description; adapter name, kind (Real, Fake, Simulated), technology and description. Double-click to rename, right-click for a menu, Delete to delete.
7. **Undo/redo** (Ctrl+Z / Ctrl+Y): every edit is one step.

## Files

- **Sketches** save as `.hexasketch.json`, with a copy of the CD embedded so the file stands alone. Each CD-sourced adapter keeps the CD element's id (`source`).
- **Export diagram** writes an SVG cropped to the content. **Export plan** writes one Markdown file. Both warn (but allow) when CD items aren't placed; the plan lists them under "Not placed".

## The plan's opinions (hard-coded in v1)

- `<System>.Domain` (no references) ← `<System>.Application` (all port interfaces) ← one project per adapter.
- Projects: `<System>.<Port>.<Adapter>`, except User port adapters, which are `<System>.<Adapter>`.
- Interfaces take the plain port name, **no `I` prefix** (`Storage`); the User port is `UserTasks` and `UserQueries`. Implementations carry the meaningful extra: `FileSystemStorage`, `FakeTracker`, never `Impl`.
- **Every port gets a fake**: if none is drawn, the plan adds one (`<Port>.Fake` for driven ports, a `Specs` xUnit project for driving ports). Fakes on driving ports are xUnit spec projects referencing every driven fake.
- Names are free text; code names are derived (PascalCase, two-letter acronyms kept: `WPF UI` → `WpfUI`). Clashes or unusable names block the plan export.
- Stubs only, no composition root, `net10.0`.

## Deferred

Configurable opinions/templates, contract-spec stubs, jigsaw-style socket shapes, free positioning, reconciling a changed CD, merging several CD items into one adapter, and regenerating the diagram from code. See `ports-sketch-handoff.md`.
