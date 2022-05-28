using System.Runtime.CompilerServices;
using System.Text;

namespace LineReader;
public class LineReader
{
    private readonly bool _suppressBlanks,_crlf;
    private readonly char _sep;
    private readonly StreamReader _src;

    /// <summary>
    /// Construct a new LineReader
    /// </summary>
    /// <param name="src">The source Stream to read from</param>
    /// <param name="sep">The separator character, default '\n'</param>
    /// <param name="suppressBlanks">Skip over blank lines (including just \r) instead of returning them</param>
    /// <param name="buffer">Whether to wrap the provided Stream in a buffer</param>
    /// <param name="crlf">Hack to regard CR and LF as interchangeable for legacy DOS files</param>
    public LineReader(Stream src,char sep='\n',bool suppressBlanks=true,bool buffer=true,bool crlf=false)
    {
        _suppressBlanks = suppressBlanks;
        _crlf = crlf;
        _sep = sep;
        _src = new StreamReader(buffer ? new BufferedStream(src) : src);
    }

    private bool Skip(StringBuilder sb)
    {
        return _suppressBlanks && sb.Length == 0;
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
            if (c == _sep)
            {
                if (!Skip(sb))
                    yield return sb.ToString();
                sb.Clear();
            }
            else sb.Append((_crlf && c=='\r')?'\n':(char)c);
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
            else sb.Append((_crlf && buff[0]=='\r')?'\n':buff[0]);
        }
    }
#endif
}
