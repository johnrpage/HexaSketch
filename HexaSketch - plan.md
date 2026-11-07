# HexaSketch: solution plan

*Exported by HexaSketch from the sketch "HexaSketch - Context Diagram". Stubs only: no functionality.*

## 1. Overview

- **System**: HexaSketch (C# .NET Desktop App)
- **Purpose**: Sketch a ports-and-adapters diagram from a context diagram, then export the diagram and a solution plan
- **Solution**: `HexaSketch.sln`, targeting `net10.0` (`net10.0-windows` for WPF projects)
- **Architecture**: ports and adapters. The application core (Domain and UseCases) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.
- **Ports**: 1 driving, 3 driven

## 2. Solution and project layout

Projects are grouped by role, and the solution folders in the `.sln` mirror the folders on disk. There are no `src/` or `tests/` folders.

```
HexaSketch.sln
  HexaSketch.App/
  HexaSketch/
    HexaSketch.Domain/
    HexaSketch.UseCases/
  Adapters/
    HexaSketch.ContextDiagrams/
    HexaSketch.Sketches/
    HexaSketch.Exports/
  Checks/
    HexaSketch.Fakes/
    HexaSketch.Specs/
```

| Project | Folder | Template | Namespace | Role |
|---|---|---|---|---|
| `HexaSketch.App` | `HexaSketch.App/` | `wpf` | `HexaSketch.App` | The app: runnable adapter on the User port |
| `HexaSketch.Domain` | `HexaSketch/HexaSketch.Domain/` | `classlib` | `HexaSketch` | Domain-specific business rules |
| `HexaSketch.UseCases` | `HexaSketch/HexaSketch.UseCases/` | `classlib` | `HexaSketch` | Application rules (use cases) and all port interfaces |
| `HexaSketch.ContextDiagrams` | `Adapters/HexaSketch.ContextDiagrams/` | `classlib` | `HexaSketch.Adapters` | Adapters on the Context Diagrams port |
| `HexaSketch.Sketches` | `Adapters/HexaSketch.Sketches/` | `classlib` | `HexaSketch.Adapters` | Adapters on the Sketches port |
| `HexaSketch.Exports` | `Adapters/HexaSketch.Exports/` | `classlib` | `HexaSketch.Adapters` | Adapters on the Exports port |
| `HexaSketch.Fakes` | `Checks/HexaSketch.Fakes/` | `classlib` | `HexaSketch.Fakes` | Fake driven adapters, for specs |
| `HexaSketch.Specs` | `Checks/HexaSketch.Specs/` | `xunit` | `HexaSketch.Specs` | Specs: the fake on every driving port |

## 3. Project references (dependency rules)

| Project | References |
|---|---|
| `HexaSketch.App` | `HexaSketch.UseCases` |
| `HexaSketch.Domain` | nothing |
| `HexaSketch.UseCases` | `HexaSketch.Domain` |
| `HexaSketch.ContextDiagrams` | `HexaSketch.UseCases` |
| `HexaSketch.Sketches` | `HexaSketch.UseCases` |
| `HexaSketch.Exports` | `HexaSketch.UseCases` |
| `HexaSketch.Fakes` | `HexaSketch.UseCases` |
| `HexaSketch.Specs` | `HexaSketch.UseCases`, `HexaSketch.Fakes` |

Rules:

- `HexaSketch.Domain` references nothing.
- `HexaSketch.UseCases` references `HexaSketch.Domain` only.
- Every adapter project references `HexaSketch.UseCases` only. **Adapters never reference each other.**
- The specs project may also reference the fakes it wires up by hand.
- These references are the dependency rule. Don't add others.

## 4. Namespaces

- `HexaSketch.Domain` and `HexaSketch.UseCases` both use the plain `HexaSketch` namespace, so the port interfaces are `HexaSketch.UserTasks` and so on.
- Every other project uses the namespace in section 2. Real and simulated adapters share one namespace, and so do the fakes.
- Folders inside a project don't change its namespace.
- Set `RootNamespace` in each `.csproj` to its namespace, so new files created in an IDE get it.
- Every namespace sits inside `HexaSketch`, so adapters, fakes and specs see the port interfaces without a `using`.

## 5. Ports

All port interfaces live in `HexaSketch.UseCases`, namespace `HexaSketch`, one file each. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.

```
HexaSketch.UseCases/
  UserTasks.cs
  UserQueries.cs
  HexaSketchApplication.cs
  Ports/
    ContextDiagrams.cs
    Sketches.cs
    Exports.cs
```

### User (driving)

The User port: how people (and anything acting for them) use the application. It has two halves, commands and queries.

`UserTasks.cs`:

```csharp
namespace HexaSketch;

public interface UserTasks
{
}
```

`UserQueries.cs`:

```csharp
namespace HexaSketch;

public interface UserQueries
{
}
```

### Context Diagrams (driven)

`Ports/ContextDiagrams.cs`:

```csharp
namespace HexaSketch;

public interface ContextDiagrams
{
}
```

### Sketches (driven)

`Ports/Sketches.cs`:

```csharp
namespace HexaSketch;

public interface Sketches
{
}
```

### Exports (driven)

`Ports/Exports.cs`:

```csharp
namespace HexaSketch;

public interface Exports
{
}
```

Also add one stub class, `HexaSketchApplication` in `HexaSketch.UseCases` (namespace `HexaSketch`), implementing the driving interfaces (`UserTasks`, `UserQueries`). It is the application itself; its constructor will later take the driven ports.

## 6. Adapters

Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally.

| Port | Adapter | Kind | Technology | Project | Class | Source |
|---|---|---|---|---|---|---|
| User | Developer | Real | WPF | `HexaSketch.App` | - | Context diagram: Developer |
| User | Specs | Fake | - | `HexaSketch.Specs` | - | Added by the plan: every port gets a fake |
| Context Diagrams | Context Diagrams | Real | File System (.contextsketch.json) | `HexaSketch.ContextDiagrams` | `FileSystemContextDiagrams` | Context diagram: Context Diagrams |
| Context Diagrams | Fake | Fake | - | `HexaSketch.Fakes` (`ContextDiagrams/`) | `FakeContextDiagrams` | Added in the sketch |
| Sketches | Sketches | Real | File System (JSON) | `HexaSketch.Sketches` | `FileSystemSketches` | Context diagram: Sketches |
| Sketches | Fake | Fake | - | `HexaSketch.Fakes` (`Sketches/`) | `FakeSketches` | Added in the sketch |
| Exports | Exports | Real | File System (SVG / Markdown) | `HexaSketch.Exports` | `FileSystemExports` | Context diagram: Exports |
| Exports | Fake | Fake | - | `HexaSketch.Fakes` (`Exports/`) | `FakeExports` | Added in the sketch |

For each driven adapter, add the stub class named above implementing its port's interface, in its project's namespace and in the folder shown. For example:

```csharp
namespace HexaSketch.Adapters;

public sealed class FileSystemContextDiagrams : ContextDiagrams
{
}
```

Driving adapters (the app among them) keep their project template's default files. Fakes on driving ports are the specs (section 7).

Adapter notes:

- **Developer** (`HexaSketch.App`): Imports a context diagram, sketches ports and adapters, exports diagram and plan
- **Specs** (`HexaSketch.Specs`): Specs that drive the application through the User port, using the fake driven adapters.
- **Context Diagrams** (`HexaSketch.ContextDiagrams`): Context diagrams exported from Context Sketch; the starting point for a sketch
- **Sketches** (`HexaSketch.Sketches`): Saved ports-and-adapters sketches
- **Exports** (`HexaSketch.Exports`): Exported diagram and solution plan

## 7. Fakes and specs

- `HexaSketch.Fakes` (`Checks/HexaSketch.Fakes/`): every fake driven adapter, with a folder per port (`ContextDiagrams/`, `Sketches/`, `Exports/`). The namespace is flat: `HexaSketch.Fakes`.
- `HexaSketch.Specs` (`Checks/HexaSketch.Specs/`): xUnit. Packages: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`, `AwesomeAssertions`. Keep the template's packages and add `AwesomeAssertions`.
  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the fake adapters up by hand; there is no composition root yet.
- Use the latest stable package versions. Use AwesomeAssertions (`.Should()` style) in specs.

## 8. Not placed

Every element of the context diagram is on the sketch.

## 9. Build this

Instructions for the coding agent:

1. Create `HexaSketch.sln` (classic `.sln` format: `dotnet new sln --format sln`).
2. Create every project in section 2 with its template, in its folder, and add it to the solution in the matching solution folder (`dotnet sln add <project> --solution-folder <folder>`, or `--in-root` for top-level projects). Set each project's `RootNamespace`.
3. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`), except `Class1.cs` in `HexaSketch.Domain`: keep it, with its namespace changed to `HexaSketch`, to show the namespace choice.
4. Add exactly the project references in section 3, and the packages in section 7.
5. Add a root `.editorconfig` with a naming rule that says interfaces are plain PascalCase, so the IDE doesn't ask for an `I` prefix:

   ```ini
   root = true

   [*.cs]
   dotnet_naming_rule.interfaces_no_prefix.symbols = interfaces
   dotnet_naming_rule.interfaces_no_prefix.style = pascal_case
   dotnet_naming_rule.interfaces_no_prefix.severity = warning

   dotnet_naming_symbols.interfaces.applicable_kinds = interface
   dotnet_naming_symbols.interfaces.applicable_accessibilities = *

   dotnet_naming_style.pascal_case.capitalization = pascal_case
   ```

6. Add the port interfaces and the application stub from section 5, and the adapter stub classes from section 6.
7. Add the placeholder spec from section 7.
8. Check that `dotnet build` and `dotnet test` both succeed.

Ground rules:

- **Stubs only.** No functionality, no interface members, no wiring.
- **No composition root** (no host project, no dependency injection setup) in this version.
- **No `I` prefix** on interfaces. Implementations carry the meaningful extra (`FileSystemStorage`), never `Impl`.
- Don't add projects, references or packages the plan doesn't list.
