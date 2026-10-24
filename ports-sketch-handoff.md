# Ports & Adapters Sketch Tool: Handoff Summary

*Summary of a conversation held 19–24 October 2026. Written to hand off to three fresh chats. Sections 1–3 are shared background; sections 4, 5 and 6 are each meant to seed one new chat, and each can be read on its own alongside the background.*

**Contents**
1. Background: the original diagram
2. Background: the example system (Cumulative Flow)
3. Background: what was settled, and the tool idea
4. **Handoff A: backlog of open questions**
5. **Handoff B: the context diagram for the Ports Sketch tool itself**
6. **Handoff C: what the sketching tool should look like (incl. the jigsaw metaphor)**

---

## 1. Background: the original diagram

A roughly three-year-old whiteboard drawing of a hexagonal (ports and adapters) architecture, drawn as a **radial jigsaw puzzle**. It was made mainly as an example for trying out architecture diagrams and communicating general ideas, not as a design for one specific system.

### Structure

| Ring | Pieces |
|---|---|
| **Core** | **Domain** (black circle in the centre) |
| **Inner ring (ports)** | Left half, red: **User Tasks** (top) and **Queries** (bottom). Right half: **Tracker** (blue, top) and **Storage** (green, bottom) |
| **Outer ring (adapters)** | Left, driving side: **Specs**, **UI** (red), **Console?**. Right, Tracker port: **Sim. App**, **DevOps** (blue), **Fake Tracker**. Right, Storage port: **In-memory Store**, **File System** (green), **dBase?** |

### Notation

- **Colour**: coloured adapters are the real/production ones and match their port's colour (UI red, DevOps blue, File System green). Black means fake, simulated, test or speculative.
- **Socket shapes**: an adapter only fits the socket cut for its port. All three left-hand adapters share a semicircular cut-out (one driving port). Tracker adapters share a triangular notch; Storage adapters share a rectangular notch.
- **Vertical dashed line**: the driving / driven divide.
- **Horizontal dashed line, right**: separates two distinct ports (Tracker, Storage) and runs on out between their adapters.
- **Horizontal dashed line, left (red)**: User Tasks and Queries are distinct (command/query separation) but together form **one port**; it stops at the UI socket because the UI plugs into both halves as a unit. It is *not* a UI-to-Domain dependency.
- **Question marks** (Console?, dBase?): "there could be other adapters", added for variety.
- Placement of Specs nearer User Tasks and Console? nearer Queries is **not meaningful**.

### Why a circle, why a jigsaw, who it's for

- **Circle** (from hexagonal architecture): shows *sides rather than a stack*. Driving and driven are peers around one core, which layered diagrams hide.
- **Jigsaw**: a communication device. An adapter only fits the socket it was cut for, making swappability visible and making fakes look first-class.
- **Audience**: the author (clarifying thinking), and a team, as a shared high-level map of a project and codebase.

---

## 2. Background: the example system (Cumulative Flow)

- Track units of developer work (stories, tasks, bugs, PBIs, issues). **Every item counts as size 1; no estimating.**
- Collect data on items added, items in the system, and items closed over time.
- Display a **cumulative flow diagram (CFD)** with automatic **projections** of progress.
- Keep a **record of past projections** to see whether forecasts settled over time and how they compared with reality at the end.

---

## 3. Background: what was settled, and the tool idea

### Settled design decisions (example system)

- **Layers**: User Tasks and Queries are the **application layer** (Uncle Bob's application-specific business rules, with CQS). Domain holds the **domain-specific business rules**, intended as a proper OO model.
- **Rule placement test**: *would this rule still hold if the app had completely different use cases?* Yes → Domain; no → User Task.
- **Anaemia**: in small apps an anaemic domain is the default unless resisted; the design is overkill for small apps. For this example, though, the domain is substantial (CFD maths, throughput rules, dated immutable forecasts, forecast-vs-actual comparison).
- **Driving port**: one port with command and query halves. Some adapters are naturally one-sided (reports, exports, `status`); "one port, two interfaces, adapters take what they need" was accepted as good enough.
- **Tracker port**: about **Tasks**, whatever the source calls them and wherever they come from (Azure DevOps, GitHub, Jira…). Only trackers that keep **transition histories** are supported. Multiple real adapters were intended.
- **State mapping**: the **domain owns the vocabulary** (canonical states and their rules), the **adapter translates**, and the **mapping is configuration passed in**, not decided by the adapter. Mapping is policy (it changes throughput numbers), not just translation.
- **Sim. App and Fake Tracker** both *replace* the DevOps adapter: Fake Tracker is simple and in-process for fast specs; Sim. App is higher fidelity for humans running the whole app locally.
- **Ports are independent**: no adapter knows another; the application layer orchestrates. Storage holds **config and results**. A User Task reads the mapping config from Storage and passes it to Tracker.
- **Likely flow**: a "refresh" command reads config → pulls history from Tracker → Domain computes CFD and projection → results saved. Queries read saved results; some queries (forecast vs actual) need domain logic.
- **Stored projections are a record, not a cache**: they can't be regenerated from the tracker. A *dated, immutable forecast* is a strong domain concept.
- **Console?** stays as a **probe of port cleanliness**: if a console front end can drive everything without UI-flavoured logic leaking in, the driving port is clean. **dBase?** dropped.
- **Specs**: fast automated checks against fakes, in domain-language xUnit tests, driving through the driving port. Keep open the option of running them with some real adapters.
- **Contract specs**: separate tests per driven port, run against fake, simulated and real adapters alike, to stop them drifting apart. **Not on the diagram.**
- **Composition root** (minimal answer): a small piece of code outside the ring (`main` or a factory) that creates the chosen adapters and hands them to the app; one per way of running (production, specs, demo). **Not on the diagram.**
- **Farley's four-layer acceptance tests**: noted, deferred. His protocol-driver layer roughly matches the three left-hand adapters.

### The overall point: a diagram-driven design tool

A tool to help **teams and their coding agents** design, communicate and maintain a system:

1. **Stage 1 (exists)**: **Context Sketch**, a tool that produces a **C4 System Context diagram** and exports JSON and SVG.
2. **Stage 2 (next)**: take the context diagram and **augment it into a ports-and-adapters diagram** like the jigsaw: slightly zoomed in, slightly more technical.
3. **Stage 3**: output a **Visual Studio solution/project/folder structure**, or a structured plan an agent can build from.

Decisions so far:

- **Source of truth**: the diagram generates the code initially; **after that, the code is the truth**. Regenerating the diagram from the code as a "view" at any stage is desirable later.
- **Generated output**: as much as possible, but **stubs only, no functionality**. Can be very opinionated, tailored to one person or team. Includes test projects with dependencies (xUnit, assertion library, etc.), **port interfaces**, **one fake adapter per port** (like `Class1.cs` in a project template), and **dependency rules**. **No composition roots in v1**; left open.
- **v1 of the tool is a manual sketching tool**, like Context Sketch: augmenting the CD is a hand-sketching process, with **"export diagram"** and **"export plan"**. Automation and code-to-diagram regeneration come later.
- **Bootstrapping**: once v1 exists, the tool could be used to design itself.

Assessment so far:

- Ports and adapters maps almost mechanically onto a .NET solution: Domain (no references) ← Application (use cases, port interfaces) ← one project per adapter; test projects for Specs and contract specs; host project as composition root. **Project references enforce the dependency rule at compile time**, which especially constrains coding agents.
- A context diagram yields **adapters** fairly directly, but not the **Domain**, the **shape of ports**, or the **use cases**. Those are design decisions, so the tool is really *a structured interviewer (or sketchpad) that ends in a generator*.

---

## 4. Handoff A: backlog of open questions

*For a chat working through what's still unanswered. Grouped by theme; the first group was explicitly deferred as "too much for now" but is recorded so it isn't lost.*

### A1. The tool: deferred for later versions

1. **Encoding the diagram in code** (needed for "diagram as a view on the code"): project references give structure but not meaning, e.g. driving vs driven, real vs fake vs simulated, which port an adapter belongs to, which external system it talks to. Options: naming conventions (`Tracker.DevOps`, `Tracker.Fake`), code markers (attributes or marker interfaces), or a metadata file (risk: becomes a second source of truth). Suggested lean: conventions for structure, attributes for meaning. Suggested test: **round trip**, diagram → solution → regenerated diagram should match.
2. **Contract-spec stubs**: generate an abstract test class per driven port (e.g. `TrackerContract`) plus a concrete subclass for the fake? Cheap, and sets the pattern for every future adapter.
3. **Wiring without composition roots**: v1 omits them, so Specs will wire fakes by hand. Ad hoc wiring habits tend to stick; worth watching.
4. **Dependency rule enforcement beyond project references**: architecture tests (NetArchTest / ArchUnitNET) for rules references can't express, plus an AGENTS.md / CLAUDE.md stating the rules in plain words for coding agents.
5. **Opinions as configuration**: keep templates/opinions in a config or template folder rather than hard-coded, so "for me" and "for a team" are the same tool with different settings.
6. **Derive vs ask**: what can the tool infer from a context diagram, and what must it ask per external system and per actor? The Tracker discussion (what does it track, whose vocabulary, where does the mapping live, independent of Storage?) is a starting question set.
7. **Where port interfaces live**: Application project (usual), Domain, or a separate contracts project. Opinionated or configurable?
8. **Scaling down**: a collapsed form (folders instead of projects, same rules) for small systems, so the tool doesn't produce a twelve-project solution for a trivial app.

### A2. The example design: parked

9. **Domain states**: fixed set (e.g. not started / in progress / finished, similar to Jira and Azure DevOps status categories) or a configurable ordered list of stages per project? Fixed is simple but lossy for richer CFD bands.
10. **Mapping-config editing**: config lives in Storage and is passed via a User Task, but who edits it and how is undefined.
11. **Mapping edge cases**: backward transitions (reopened items) and unmapped states (fail loudly, ignore, guess?).
12. **Can a Query ever trigger a live Tracker fetch**, or only read stored results? (Author unsure.)
13. **Queries and the Domain**: which queries need domain logic (forecast vs actual, settling over time) and which are a thin read model?

### A3. Diagram notation: open

14. Should the left-hand dashed line ("one port, two halves") look different from the right-hand one ("two separate ports")?
15. Should **contract specs** and the **composition root** appear on the diagram, and how?
16. **Farley's four-layer acceptance tests**: how do they map onto Specs and the driving adapters?

---

## 5. Handoff B: the context diagram for the Ports Sketch tool itself

*For a chat continuing to design the CD of the tool being proposed. The tool's name "Ports Sketch" is a placeholder.*

### Context Sketch's JSON format (observed from an export)

- Top level: `schemaVersion` (1), `title`, `elements[]`, `relationships[]`.
- Element: `id` (8 hex chars), `type`, `name`, optional `description`, optional `technology`, `x`, `y`, `pinned`.
- Element types seen: `system`, `primaryActor`, `secondaryActor`, `externalSystem`, `storage`.
- Relationship: `id`, `from`, `to`, `label`, optional `technology`, `direction` (`oneWay` seen).
- Layout convention: the system at (0, 0); primary actors on the **left** (x ≈ −345); external systems, storage and secondary actors on the **right**. SVG export follows C4 styling (blue system, dark actors, grey externals, cylinder for storage, dashed outline for secondary actors).
- Context Sketch **can import** this JSON.

### The Cumulative Flow CD the author sketched (for reference)

- System: **Cumulative Flow** (.NET Desktop App).
- Primary actors: **Viewer** ("Views stuff", web page?) and **Agent?** (Claude, "Requests report?", via CLI).
- Right side: **Azure DevOps?** and **Jira** (external systems), **Storage 1** (flat files), and a placeholder **Secondary Actor 1**.

Observations made:

- **The CD already encodes sides**: primary actors left, externals/storage/secondary actors right. That is the driving/driven split before any port is drawn, so stage 2 has more to start from than assumed.
- **The key gap is grouping**: a CD lists external systems; a ports diagram groups them by role. Nothing says DevOps and Jira are both "Task sources". Grouping externals into ports is likely the main manual job in stage 2, and probably shouldn't be pushed down into the CD tool.
- **"Agent? via CLI" is the Console? probe in disguise**: a second driving adapter that tests whether the driving port is UI-free.

### Draft CD for Ports Sketch (v1)

Delivered as `Ports Sketch - Context Diagram.contextsketch.json`.

| Element | Type | Role / technology |
|---|---|---|
| **Ports Sketch** | system | Sketch a ports-and-adapters diagram from a CD, export diagram and plan. Stack: same as Context Sketch? |
| **Developer** | primaryActor | Imports a CD, sketches, exports (UI) |
| **Context Sketch** | externalSystem | Source of the starting CD (`.contextsketch.json`) |
| **Sketch Files** | storage | Saved ports sketches (JSON) |
| **Exports** | storage | Exported diagram and plan (SVG / Markdown) |
| **Coding Agent** | secondaryActor | Builds the stub solution from the exported plan (Claude) |

Relationships: Developer → Ports Sketch ("Sketches and exports"); Ports Sketch → Context Sketch ("Imports context diagram", JSON file); → Sketch Files ("Saves and loads sketches"); → Exports ("Writes diagram and plan"); → Coding Agent ("Provides solution plan (via exported file)").

### Observations and open questions on the draft

1. **It's as trivial as expected**, which is fine: almost all the design is inside the box, which is the point of the next diagram down.
2. **The inverse of the DevOps/Jira problem**: there, two external systems share one port; here, four neighbours (Context Sketch, Sketch Files, Exports, Coding Agent) share one technical channel, the file system. Draw them as one "File System" box or as separate role-based neighbours? This decides whether the tool's own ports diagram has one storage port or several role-based ports (import, sketches, exports). *Lean: role-based.* **This was the open question when the chat paused.**
3. **Is the Coding Agent on the diagram at all?** In v1 the tool never talks to it; the developer hands it a file. It was kept as a secondary actor because it's the reason the plan export exists, but it could be left off until the tool hands over directly (e.g. via MCP or CLI).
4. **Is the team an actor?** The team reads the exported diagram as a shared map. Secondary actor, or out of scope for v1?
5. **Is there a second driving actor?** E.g. a coding agent using the tool directly via CLI, mirroring the Cumulative Flow "Agent?" and the Console? probe. Probably not in v1.
6. **Tech stack**: unknown; assumed to match Context Sketch.
7. **Next step**: once the CD settles, sketch the tool's own **ports-and-adapters diagram** as the first bootstrapping exercise.

---

## 6. Handoff C: what the sketching tool should look like

*For a chat exploring the functionality and visual design of the v1 sketching tool. Very little of this has been discussed yet; below are the agreed constraints and a set of starting questions.*

### Agreed constraints

- v1 is a **manual sketching tool**, in the spirit of Context Sketch.
- Starting point: **import a Context Sketch JSON** export.
- Output: **"export diagram"** (e.g. SVG) and **"export plan"** (a structured plan an agent can turn into a stub .NET solution).
- Plan contents: solution/project structure, test projects with dependencies, port interfaces, one fake adapter per port, dependency rules. **No composition roots** in v1. Stubs only.
- Opinionated is fine; tailorable to one person or team.

### What the original drawing gives a general-case tool to work with

- **Concentric rings**: Domain core → ports ring → adapters ring.
- **Two halves**: driving (left) and driven (right), split by a vertical boundary.
- **Socket shapes** encode port identity: each port has a distinct cut, and every adapter for that port carries the matching cut.
- **Colour** encodes port family plus real vs fake/simulated/speculative.
- **Sub-divisions inside a port** (the CQS split) shown with a lighter dashed line.
- **Question-mark adapters** for "possible, not built".

### Starting questions on the jigsaw metaphor in the general case

1. **Shape generation for N ports**: hand-drawn, each port had a distinct, memorable cut (semicircle, triangle, rectangle). How does the tool generate distinct socket shapes for arbitrary numbers of ports? A fixed palette of shapes? What happens beyond the palette?
2. **Geometry for N ports and M adapters**: angular space is shared between ports. How does the ring scale when one port has six adapters and another one? Do sectors size by adapter count, or equally?
3. **Driving vs driven balance**: what if one side has many more ports than the other? Keep the vertical split fixed at the centre, or let it move?
4. **Is the jigsaw cosmetic or structural?** I.e. do shapes simply *show* membership, or does the editor *enforce* it (you literally can't drop an adapter on the wrong port)? The latter makes the metaphor do real work.
5. **Colour semantics**: per-port colour plus real-vs-fake on top is two dimensions in one channel. Is black-for-non-production still right, or should fakes be shown another way (outline style, hatching, a badge)?
6. **Sub-ports**: how is the command/query split (or "one port, two interfaces") represented and edited generally?
7. **Missing pieces**: where do contract specs and the composition root go, if anywhere? (See A3.)
8. **Does the metaphor survive small and large systems?** One driven port looks odd as a ring; twelve ports may be unreadable. Is there a fallback layout?

### Starting questions on the sketching interaction

9. **From CD to ring**: what happens on import? A proposed initial layout (primary actors → driving adapters, external systems and storage → driven adapters, the system → Domain/ports placeholder)? Everything ungrouped and left for the user to arrange?
10. **Grouping into ports**: the main manual job (e.g. DevOps + Jira → Tracker port). Drag adapters together? Draw a port and drop adapters in?
11. **Adding what the CD doesn't have**: fakes, simulators, Specs, Console-style probes, use cases/queries, Domain contents. How are they added, and are fakes auto-suggested (one per port)?
12. **Traceability back to the CD**: should each adapter remember which CD element it came from, so later CD changes can be reconciled?
13. **Element properties**: what does each piece need to carry so "export plan" can produce projects, interfaces and stubs (names, namespaces, technology, kind: real/fake/sim/spec)?
14. **Export plan format**: Markdown for an agent? JSON? Both? How opinionated, and where do the opinions/templates live?
15. **Save format**: a `.portsketch.json` (or similar) mirroring Context Sketch's schema style?
16. **Consistency with Context Sketch**: same app with two views, or separate tools sharing a format?
