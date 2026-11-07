# HexaSketch

A small Windows desktop tool (WPF, .NET) that turns a context diagram from [Context Sketch](https://github.com/johnrpage/context-tool) into a ports-and-adapters sketch. It exports the sketch as an SVG diagram, plus a Markdown solution plan a coding agent can build stubs from.

> **Superseded by [Motif Sketch](https://github.com/johnrpage/Motif.Sketch).** Motif Sketch combines this tool and Context Sketch into one browser app. This repo is kept as reference material, and its diagram, sketching behaviour and plan opinions carry over to Motif Sketch, with the changes listed in Motif Sketch's SPEC.md.

## Running it

```
dotnet run --project src/HexaSketch.Wpf
dotnet run --project src/HexaSketch.Wpf -- "HexaSketch - Context Diagram.contextsketch.json"
dotnet test
```

## Docs

- [DESIGN.md](DESIGN.md): what v1 does and why, including the plan's opinions
- [ARCHITECTURE.md](ARCHITECTURE.md): how the code is organised (ports and adapters)
- [HexaSketch.hexasketch.json](HexaSketch.hexasketch.json): HexaSketch's own sketch

## License

MIT, see [LICENSE](LICENSE).
