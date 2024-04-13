using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace SemanticChunker;

internal sealed record WhisperXWord(string Word)
{
    public float? Start { get; init; }
    public float? End { get; init; }
    public float? Score { get; init; }
}

internal sealed record WhisperXSegment(float Start, float End, string Text, IList<WhisperXWord> Words);

internal sealed record WhisperXJson(IList<WhisperXSegment> Segments)
{
    private static readonly SearchValues<char> endOfSentence = SearchValues.Create("。！？!?");

    public void PatchSegments()
    {
        for (var i = 0; i < Segments.Count; ++i)
        {
            var current = Segments[i];
            var count = current.Words.Count;
            if (current.Words is [{ Word: { } startWord } start, ..])
            {
                if (i > 0 && !current.Text.StartsWith(startWord))
                {
                    var previous = Segments[i - 1];
                    if (previous.Text.EndsWith(startWord))
                    {
                        current.Words.RemoveAt(0);
                        previous.Words.Add(start);
                    }
                    else if (previous.Words is [.., { } previousEnd] and { Count: { } previousCount } && current.Text.StartsWith(previousEnd.Word))
                    {
                        previous.Words.RemoveAt(previousCount - 1);
                        current.Words.Insert(0, previousEnd);
                    }
                }
            }
            if (current.Words is [.., { Word: { } endWord } end])
            {
                if (i + 1 < Segments.Count && !current.Text.EndsWith(endWord))
                {
                    var next = Segments[i + 1];
                    if (next.Text.StartsWith(endWord))
                    {
                        current.Words.RemoveAt(count - 1);
                        next.Words.Insert(0, end);
                    }
                    else if (next.Words is [{ } nextStart, ..] && current.Text.EndsWith(nextStart.Word))
                    {
                        next.Words.RemoveAt(0);
                        current.Words.Add(nextStart);
                    }
                }
            }
        }
    }

    public IList<WhisperXSegment> ResegmentChineseSentences()
    {
        var segments = Segments;
        var words = segments.SelectMany((s, i) => s.Words.Select(w => (i, w))).ToList();
        WhisperXSegment ResegmentCore(int previous, int next) // re-segment while retaining where to have spaces between words
        {
            var startWord = words[previous];
            var endWord = words[next];
            var startSegmentIndex = startWord.i;
            var endSegmentIndex = endWord.i;
            var startSegment = segments[startSegmentIndex];
            var endSegment = segments[endSegmentIndex];
            var startText = startSegment.Text;
            var endText = endSegment.Text;
            int trimStart = 0, trimEnd = endText.Length;
            float start, end;
            {
                var sw = startSegment.Words;
                var si = -1;
                for (var i = 0; i < sw.Count; ++i)
                {
                    if (ReferenceEquals(sw[i], startWord.w))
                    {
                        si = i;
                        break;
                    }
                }
                Debug.Assert(si >= 0);
                if (si == 0)
                {
                    start = startSegment.Start;
                }
                else
                {
                    // Trims preceeding words and spaces
                    for (var i = 0; i < si; ++i)
                    {
                        var w = startSegment.Words[i].Word;
                        trimStart = startText.IndexOf(w, trimStart, StringComparison.Ordinal) + w.Length;
                        Debug.Assert(trimStart <= startText.Length);
                        if (trimStart < startText.Length && startText[trimStart] == ' ')
                        {
                            ++trimStart;
                        }
                    }
                    if (startWord.w.Start is { } s)
                    {
                        start = s;
                    }
                    else
                    {
                        for (; ; )
                        {
                            if (--si <= 0)
                            {
                                start = startSegment.Start;
                                break;
                            }
                            if (sw[si].End is { } e)
                            {
                                start = e;
                                break;
                            }
                        }
                    }
                }
            }
            {
                var ew = endSegment.Words;
                var ei = -1;
                for (var i = ew.Count; --i >= 0;)
                {
                    if (ReferenceEquals(ew[i], endWord.w))
                    {
                        ei = i;
                        break;
                    }
                }
                Debug.Assert(ei >= 0);
                var last = ew.Count - 1;
                if (ei == last)
                {
                    end = endSegment.End;
                }
                else
                {
                    // Trims succeeding spaces and words
                    for (var i = endSegment.Words.Count - 1; i > ei; --i)
                    {
                        trimEnd = endText.LastIndexOf(endSegment.Words[i].Word, trimEnd - 1, StringComparison.Ordinal);
                        Debug.Assert(trimEnd >= 0);
                        if (trimEnd > 0 && endText[trimEnd - 1] == ' ')
                        {
                            --trimEnd;
                        }
                    }
                    if (endWord.w.End is { } e)
                    {
                        end = e;
                    }
                    else
                    {
                        for (; ; )
                        {
                            if (++ei >= last)
                            {
                                end = endSegment.End;
                                break;
                            }
                            if (ew[ei].Start is { } es)
                            {
                                end = es;
                                break;
                            }
                        }
                    }
                }
            }
            string text;
            if (startSegmentIndex == endSegmentIndex)
            {
                Debug.Assert(trimEnd - trimStart > 0);
                text = startText[trimStart..trimEnd];
            }
            else
            {
                var length = startText.Length - trimStart;
                var capacity = length + endSegmentIndex - startSegmentIndex + trimEnd;
                for (var i = startSegmentIndex + 1; i < endSegmentIndex; ++i)
                {
                    capacity += segments[i].Text.Length;
                }
                StringBuilder sb = new(startText, trimStart, length, capacity);
                for (var i = startSegmentIndex + 1; i < endSegmentIndex; ++i)
                {
                    sb.Append(' ');
                    sb.Append(segments[i].Text);
                }
                sb.Append(' ');
                sb.Append(endText, 0, trimEnd);
                text = sb.ToString();
            }
            return new(start, end, text,
                words.Skip(previous).Take(next - previous + 1).Select(p => p.w).ToList());
        }
        var last = words.Count - 1;
        List<WhisperXSegment> sentences = new(segments.Count);
        for (var previous = 0; previous <= last;)
        {
            var next = words.FindIndex(previous, p => p.w.Word is [{ } c] && endOfSentence.Contains(c));
            if (next == -1)
            {
                if (previous < last)
                {
                    sentences.Add(ResegmentCore(previous, last));
                }
                break;
            }
            else
            {
                if (previous == next)
                {
                    sentences[^1].Words.Add(words[next].w);
                }
                else
                {
                    sentences.Add(ResegmentCore(previous, next));
                }
                previous = next + 1;
            }
        }
        return sentences;
    }

    public string ToTSV()
    {
        StringBuilder sb = new("start\tend\ttext\n");
        foreach (var s in Segments)
        {
            sb.Append($"{s.Start:F3}\t{s.End:F3}\t{s.Text}\n");
        }
        sb.Remove(sb.Length - 1, 1);
        return sb.ToString();
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(WhisperXJson))]
internal sealed partial class WhisperXJsonContext : JsonSerializerContext { }