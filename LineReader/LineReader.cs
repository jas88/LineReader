using System.Runtime.CompilerServices;
using System.Text;

namespace LineReader;
public class LineReader
{
    private readonly bool _suppressBlanks;
    private readonly char _sep;
    private readonly StreamReader _src;

    /// <summary>
    /// Construct a new LineReader
    /// </summary>
    /// <param name="src">The source Stream to read from</param>
    /// <param name="sep">The separator character, default '\n'</param>
    /// <param name="suppressBlanks">Skip over blank lines (including just \r) instead of returning them</param>
    /// <param name="buffer">Whether to wrap the provided Stream in a buffer</param>
    public LineReader(Stream src,char sep='\n',bool suppressBlanks=true,bool buffer=true)
    {
        _suppressBlanks = suppressBlanks;
        _sep = sep;
        _src = new StreamReader(buffer ? new BufferedStream(src) : src);
    }

    private bool Skip(StringBuilder sb)
    {
        return _suppressBlanks && (sb.Length == 0 || (sb.Length == 1 && sb[0] == '\r'));
    }

    /// <summary>
    /// Iterate through the lines/substrings in this Stream
    /// </summary>
    /// <returns>Enumerating each line/substring</returns>
    public IEnumerable<string> ReadLines()
    {
        StringBuilder sb = new();
        while (true)
        {
            var c = _src.Read();
            if (c == -1)
            {
                if (!Skip(sb))
                    yield return sb.ToString();
                yield break;
            }
            else if (c == _sep)
            {
                if (!Skip(sb))
                    yield return sb.ToString();
                sb.Clear();
            }
            else sb.Append((char)c);
        }
    }

#if NET
    /// <summary>
    /// Iterate through the lines/substrings in this Stream asynchronously
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Enumerating each line/substring</returns>
    public async IAsyncEnumerable<string> ReadLines([EnumeratorCancellation] CancellationToken ct)
    {
        StringBuilder sb = new(); 
        var buff = new char[]{'\uffff'};
        while (true)
        {
            var r = await _src.ReadAsync(buff, ct);
            if (r == 0)
            {
                if (!Skip(sb))
                    yield return sb.ToString();
                yield break;
            }
            if (buff[0] == _sep)
            {
                if (!Skip(sb))
                    yield return sb.ToString();
                sb.Clear();
            }
            else sb.Append(buff[0]);
        }
    }
#endif
}
