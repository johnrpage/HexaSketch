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

## The plan's opinions (hard-coded)

- Folders by role, mirrored as solution folders; no `src/` or `tests/`:
  - `<System>/`: `<System>.Domain` (no references) ← `<System>.UseCases` (all port interfaces).
  - `Adapters/`: one `<System>.<Port>` project per port (driven, or driving other than User), holding all its Real and Simulated adapters.
  - `Checks/`: `<System>.Fakes` (every driven fake, a folder per port) and `<System>.Specs` (every driving port's fake: xUnit, AwesomeAssertions, template packages kept).
  - Top level: the User port's runnable (non-Fake) adapter is `<System>.App`; if there are several, each is `<System>.<Adapter>`.
- Namespaces: Domain and UseCases share the plain `<System>` namespace, so the port interfaces sit there. Adapters share `<System>.Adapters`; fakes share `<System>.Fakes`; specs `<System>.Specs`; the app its project name. Folders don't change namespaces; `RootNamespace` is set in every project.
- Interfaces take the plain port name, **no `I` prefix** (`Storage`), one file each: the User port's `UserTasks` and `UserQueries` at UseCases' top level, the rest in `Ports/`.
- Implementations carry the meaningful extra, never `Impl`. The class prefix is the adapter's name (`AzureDevOpsTracker`); when that only repeats the port's name (the default when imported from a CD), the technology without its brackets (`File System (JSON)` → `FileSystemSketches`); failing that, the kind (`FakeTracker`).
- **Every port gets a fake**: if none is drawn, the plan adds one (`Fake` for driven ports, `Specs` for driving ports).
- Names are free text; code names are derived (PascalCase, two-letter acronyms kept: `WPF UI` → `WpfUI`). Clashes or unusable names block the plan export, including ports named like a project or namespace (`Domain`, `UseCases`, `Application`, `App`, `Adapters`, `Fakes`, `Specs`, the system, or a runnable User adapter).
- Stubs only, no composition root, `net10.0`. A root `.editorconfig` says interfaces need no `I` prefix; Domain keeps its `Class1.cs` to show the namespace choice.

## Deferred

Configurable opinions/templates, contract-spec stubs, jigsaw-style socket shapes, free positioning, reconciling a changed CD, merging several CD items into one adapter, and regenerating the diagram from code. See `ports-sketch-handoff.md`.
