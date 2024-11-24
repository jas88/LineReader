using System.Runtime.CompilerServices;
using System.Text;

namespace LineReader;

/// <summary>
/// Construct a new LineReader
/// </summary>
/// <param name="src">The source Stream to read from</param>
/// <param name="sep">The separator character, default '\n'</param>
/// <param name="suppressBlanks">Skip over blank lines (including just \r) instead of returning them</param>
/// <param name="buffer">Whether to wrap the provided Stream in a buffer</param>
/// <param name="crlf">Hack to regard CR and LF as interchangeable for legacy DOS files</param>
public class LineReader(Stream src, char sep = '\n', bool suppressBlanks = true, bool buffer = true, bool crlf = false)
{
    private readonly StreamReader _src = new(buffer ? new BufferedStream(src) : src);

    private bool Skip(StringBuilder sb) => suppressBlanks && sb.Length == 0;

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

            if (crlf && c == '\r')
                c = sep;
            if (c == sep)
            {
                if (!Skip(sb))
                    yield return sb.ToString();

                sb.Clear();
            }
            else sb.Append(crlf && c == '\r' ? '\n' : (char)c);
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
        var buff = new[] { '\uffff' };
        while (true)
        {
            var r = await _src.ReadAsync(buff, ct);
            if (r == 0)
            {
                if (!Skip(sb))
                    yield return sb.ToString();

                yield break;
            }

            if (crlf && buff[0] == '\r')
                buff[0] = sep;
            if (buff[0] == sep)
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
