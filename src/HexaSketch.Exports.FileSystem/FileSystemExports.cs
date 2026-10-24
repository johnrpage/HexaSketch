using System.Text;
using HexaSketch.Application;

namespace HexaSketch.Exports.FileSystem;

/// <summary>Exports adapter. Writes the SVG diagram and the Markdown plan as UTF-8 files.</summary>
public sealed class FileSystemExports : HexaSketch.Application.Exports
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public void WriteDiagram(string location, string svg) => File.WriteAllText(location, svg, Utf8);

    public void WritePlan(string location, string markdown) => File.WriteAllText(location, markdown, Utf8);
}
