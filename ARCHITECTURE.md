# HexaSketch architecture

HexaSketch is built the way it tells you to build things: ports and adapters.

```
src/HexaSketch.Domain                    sketch model and its rules; no references
src/HexaSketch.Application               -> Domain. The User port (UserTasks, UserQueries),
                                         the driven ports (ContextDiagrams, Sketches, Exports),
                                         SketchSession, layout, SVG and plan rendering
src/HexaSketch.<Port>.FileSystem         real driven adapters      -> Application only
src/HexaSketch.<Port>.Fake               in-memory driven adapters -> Application only
src/HexaSketch.Wpf                       WPF UI, the real adapter on the User port, and the
                                         composition root (App.xaml.cs)
tests/HexaSketch.Specs                   xUnit; drives the User port with the Fake adapters
tests/HexaSketch.FileSystem.Specs        xUnit; checks the FileSystem adapters against real files
```

## Dependency rules

- Domain references nothing.
- Application references Domain only.
- Every adapter references Application only. Adapters never reference each other.
- Only the composition root (in `HexaSketch.Wpf`) knows which adapters are used.

## Naming

- Interfaces take the plain name of the port, with no `I` prefix: `Sketches`, `Exports`.
- Implementations carry the meaningful extra: `FileSystemSketches`, `FakeExports`, `SketchSession`.
