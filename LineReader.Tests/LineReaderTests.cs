using System.Text;

namespace LineReader.Tests;

public class LineReaderTests
{
    private const string withNulls="foo\bar";

    [SetUp]
    public void Setup()
    {
    }

    private static async Task<string[]> ToArray(IAsyncEnumerable<string> gen)
    {
        List<string> r = new();
        await foreach (var s in gen)
        {
            r.Add(s);
        }
        return r.ToArray();
    }

    [TestCase("",new string[]{})]
    [TestCase("foo\0bar",new string[]{"foo\0bar"})]
    [TestCase("foo\0bar",new string[]{"foo","bar"},'\0')]
    public void Test1(string payload,string [] result,char delim='\n')
    {
        var lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)),delim);
        Assert.That(lr.ReadLines().ToArray(),Is.EqualTo(result));
    }

    [Test]
    public void PreCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes("foo")));
        Assert.ThrowsAsync<TaskCanceledException>(async () => await ToArray(lr.ReadLines(cts.Token)));
    }
}