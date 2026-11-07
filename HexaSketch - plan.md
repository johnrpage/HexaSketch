# HexaSketch: solution plan

*Exported by HexaSketch from the sketch "HexaSketch - Context Diagram". Stubs only: no functionality.*

## 1. Overview

- **System**: HexaSketch (C# .NET Desktop App)
- **Purpose**: Sketch a ports-and-adapters diagram from a context diagram, then export the diagram and a solution plan
- **Solution**: `HexaSketch.sln`, targeting `net10.0` (`net10.0-windows` for WPF projects)
- **Architecture**: ports and adapters. The application core (Domain and Application) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.
- **Ports**: 1 driving, 3 driven

## 2. Solution and project layout

```
HexaSketch.sln
  src/HexaSketch.Domain/
  src/HexaSketch.Application/
  src/HexaSketch.Developer/
  src/HexaSketch.ContextDiagrams.ContextDiagrams/
  src/HexaSketch.ContextDiagrams.Fake/
  src/HexaSketch.Sketches.Sketches/
  src/HexaSketch.Sketches.Fake/
  src/HexaSketch.Exports.Exports/
  src/HexaSketch.Exports.Fake/
  tests/HexaSketch.Specs/
```

| Project | Template | Role |
|---|---|---|
| `HexaSketch.Domain` | `classlib` | Domain-specific business rules |
| `HexaSketch.Application` | `classlib` | Application rules (use cases) and all port interfaces |
| `HexaSketch.Developer` | `wpf` | Driving adapter |
| `HexaSketch.ContextDiagrams.ContextDiagrams` | `classlib` | Driven adapter |
| `HexaSketch.ContextDiagrams.Fake` | `classlib` | Driven adapter |
| `HexaSketch.Sketches.Sketches` | `classlib` | Driven adapter |
| `HexaSketch.Sketches.Fake` | `classlib` | Driven adapter |
| `HexaSketch.Exports.Exports` | `classlib` | Driven adapter |
| `HexaSketch.Exports.Fake` | `classlib` | Driven adapter |
| `HexaSketch.Specs` | `xunit` | Specs |

## 3. Project references (dependency rules)

| Project | References |
|---|---|
| `HexaSketch.Domain` | nothing |
| `HexaSketch.Application` | `HexaSketch.Domain` |
| `HexaSketch.Developer` | `HexaSketch.Application` |
| `HexaSketch.ContextDiagrams.ContextDiagrams` | `HexaSketch.Application` |
| `HexaSketch.ContextDiagrams.Fake` | `HexaSketch.Application` |
| `HexaSketch.Sketches.Sketches` | `HexaSketch.Application` |
| `HexaSketch.Sketches.Fake` | `HexaSketch.Application` |
| `HexaSketch.Exports.Exports` | `HexaSketch.Application` |
| `HexaSketch.Exports.Fake` | `HexaSketch.Application` |
| `HexaSketch.Specs` | `HexaSketch.Application`, `HexaSketch.ContextDiagrams.Fake`, `HexaSketch.Sketches.Fake`, `HexaSketch.Exports.Fake` |

Rules:

- `HexaSketch.Domain` references nothing.
- `HexaSketch.Application` references `HexaSketch.Domain` only.
- Every adapter project references `HexaSketch.Application` only. **Adapters never reference each other.**
- Spec projects may also reference the fake driven adapters they wire up by hand.
- These references are the dependency rule. Don't add others.

## 4. Ports

All port interfaces live in `HexaSketch.Application`, namespace `HexaSketch.Application`. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.

### User (driving)

The User port: how people (and anything acting for them) use the application. It has two halves, commands and queries.

```csharp
namespace HexaSketch.Application;

public interface UserTasks
{
}

public interface UserQueries
{
}
```

### Context Diagrams (driven)

```csharp
namespace HexaSketch.Application;

public interface ContextDiagrams
{
}
```

### Sketches (driven)

```csharp
namespace HexaSketch.Application;

public interface Sketches
{
}
```

### Exports (driven)

```csharp
namespace HexaSketch.Application;

public interface Exports
{
}
```

Also add one stub class, `HexaSketchApplication` in `HexaSketch.Application`, implementing the driving interfaces (`UserTasks`, `UserQueries`). It is the application itself; its constructor will later take the driven ports.

## 5. Adapters

Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally.

| Port | Adapter | Kind | Technology | Project | Class | Source |
|---|---|---|---|---|---|---|
| User | Developer | Real | WPF | `HexaSketch.Developer` | - | Context diagram: Developer |
| User | Specs | Fake | - | `HexaSketch.Specs` | - | Added by the plan: every port gets a fake |
| Context Diagrams | Context Diagrams | Real | File System (.contextsketch.json) | `HexaSketch.ContextDiagrams.ContextDiagrams` | `ContextDiagrams` | Context diagram: Context Diagrams |
| Context Diagrams | Fake | Fake | - | `HexaSketch.ContextDiagrams.Fake` | `FakeContextDiagrams` | Added in the sketch |
| Sketches | Sketches | Real | File System (JSON) | `HexaSketch.Sketches.Sketches` | `Sketches` | Context diagram: Sketches |
| Sketches | Fake | Fake | - | `HexaSketch.Sketches.Fake` | `FakeSketches` | Added in the sketch |
| Exports | Exports | Real | File System (SVG / Markdown) | `HexaSketch.Exports.Exports` | `Exports` | Context diagram: Exports |
| Exports | Fake | Fake | - | `HexaSketch.Exports.Fake` | `FakeExports` | Added in the sketch |

For each driven adapter, add the stub class named above implementing its port's interface, in the namespace matching its project. For example:

```csharp
namespace HexaSketch.ContextDiagrams.ContextDiagrams;

using HexaSketch.Application;

public sealed class ContextDiagrams : ContextDiagrams
{
}
```

Driving adapters keep their project template's default files. Fakes on driving ports are spec projects (next section).

Adapter notes:

- **Developer** (`HexaSketch.Developer`): Imports a context diagram, sketches ports and adapters, exports diagram and plan
- **Specs** (`HexaSketch.Specs`): Specs that drive the application through the User port, using the fake driven adapters.
- **Context Diagrams** (`HexaSketch.ContextDiagrams.ContextDiagrams`): Context diagrams exported from Context Sketch; the starting point for a sketch
- **Sketches** (`HexaSketch.Sketches.Sketches`): Saved ports-and-adapters sketches
- **Exports** (`HexaSketch.Exports.Exports`): Exported diagram and solution plan

## 6. Test projects and packages

- `HexaSketch.Specs` (`tests/HexaSketch.Specs`): xUnit. Packages: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Shouldly`.
  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the fake adapters up by hand; there is no composition root yet.
- Use the latest stable package versions. Prefer Shouldly assertions in specs.

## 7. Not placed

Every element of the context diagram is on the sketch.

## 8. Build this

Instructions for the coding agent:

1. Create `HexaSketch.sln` (classic `.sln` format: `dotnet new sln --format sln`).
2. Create every project in section 2 with its template and folder, and add it to the solution. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`).
3. Add exactly the project references in section 3, and the packages in section 6.
4. Add the port interfaces and the application stub from section 4, and the adapter stub classes from section 5.
5. Add the placeholder specs from section 6.
6. Check that `dotnet build` and `dotnet test` both succeed.

Ground rules:

- **Stubs only.** No functionality, no interface members, no wiring.
- **No composition root** (no host project, no dependency injection setup) in this version.
- **No `I` prefix** on interfaces. Implementations carry the meaningful extra (`FileSystemStorage`), never `Impl`.
- Don't add projects, references or packages the plan doesn't list.
