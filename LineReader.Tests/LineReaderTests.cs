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

    private static string[] ToArraySync(IAsyncEnumerable<string> gen)
    {
        return Task.Run(async () => await ToArray(gen)).Result;
    }

    [TestCase("",new string[]{},'\n',true)]
    [TestCase("",new string[]{""},'\n',false)]
    [TestCase("\n\n",new string[]{},'\n',true)]
    [TestCase("\n\n",new string[]{"","",""},'\n',false)]
    [TestCase("foo\0bar",new string[]{"foo\0bar"})]
    [TestCase("foo\0bar",new string[]{"foo","bar"},'\0')]
    public void BasicTests(string payload,string [] result,char delim='\n',bool skip=true)
    {
        var lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)),delim,skip);
        var syncresult = lr.ReadLines().ToArray();
        Assert.That(syncresult,Is.EqualTo(result));

        // Test async version
        lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)),delim,skip);
        var asyncresult = ToArraySync(lr.ReadLines(new CancellationToken()));
        Assert.That(asyncresult,Is.EqualTo(result));

        // Test unbuffered too
        lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)),delim,skip,buffer:false);
        syncresult = lr.ReadLines().ToArray();
        Assert.That(syncresult,Is.EqualTo(result));
    }

    [TestCase("foo\n\nbar",new[]{"foo","","bar"},false)]
    [TestCase("foo\n\nbar",new[]{"foo","bar"},true)]
    [TestCase("foo\r\n\r\nbar",new[]{"foo","bar"},true,true)]
    public void SkipBlanks(string payload, string[] result,bool suppressBlanks,bool crlf=false,char sep='\n')
    {
        var lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)),sep:sep,suppressBlanks:suppressBlanks,crlf:crlf);
        var syncresult = lr.ReadLines().ToArray();
        Assert.That(syncresult,Is.EqualTo(result));
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