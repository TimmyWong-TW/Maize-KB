using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Serialization;

namespace YouTube;

public sealed class Script
{
    [XmlAttribute("id")]
    public string? Id { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public sealed class Text
{
    [XmlAttribute("start")]
    public float Start { get; set; }

    [XmlAttribute("dur")]
    public float Duration { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

[XmlRoot("transcript")]
public sealed class Transcript
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Referenced directly.")]
    public static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(Transcript));

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Referenced directly.")]
    public static Transcript? Deserialize(Stream stream) => Serializer.Deserialize(stream) as Transcript;

    [XmlElement("script")]
    public Script[]? Scripts { get; set; }

    [XmlElement("text")]
    public Text[]? Texts { get; set; }

    public string ToTSV()
    {
        StringBuilder sb = new("start\tend\ttext");
        if (Texts is { Length: > 0 } lines)
        {
            foreach (var t in lines)
            {
                sb.Append($"\n{t.Start:F3}\t{t.Start + t.Duration:F3}\t{t.Value?.ReplaceLineEndings(" ")}");
            }
        }
        return sb.ToString();
    }
}
