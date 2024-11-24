using System.Text;

namespace LineReader.Tests;

public sealed class LineReaderTests
{
    [SetUp]
    public void Setup()
    {
    }

    private static async Task<string[]> ToArray(IAsyncEnumerable<string> gen)
    {
        List<string> r = [];
        await foreach (var s in gen)
            r.Add(s);
        return [.. r];
    }

    private static string[] ToArraySync(IAsyncEnumerable<string> gen)
    {
        return Task.Run(async () => await ToArray(gen)).Result;
    }

    [TestCase("", new string[] { }, '\n', true)]
    [TestCase("", new[] { "" }, '\n', false)]
    [TestCase("\n\n", new string[] { }, '\n', true)]
    [TestCase("\n\n", new[] { "", "", "" }, '\n', false)]
    [TestCase("foo\0bar", new[] { "foo\0bar" })]
    [TestCase("foo\0bar", new[] { "foo", "bar" }, '\0')]
    public void BasicTests(string payload, string[] result, char delim = '\n', bool skip = true)
    {
        var lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)), delim, skip);
        var syncresult = lr.ReadLines().ToArray();
        Assert.That(syncresult, Is.EqualTo(result));

        // Test async version
        lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)), delim, skip);
        var asyncresult = ToArraySync(lr.ReadLines(new CancellationToken()));
        Assert.That(asyncresult, Is.EqualTo(result));

        // Test unbuffered too
        lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)), delim,
            skip, false);
        syncresult = lr.ReadLines().ToArray();
        Assert.That(syncresult, Is.EqualTo(result));
    }

    [TestCase("foo\n\nbar", new[] { "foo", "", "bar" }, false)]
    [TestCase("foo\n\nbar", new[] { "foo", "bar" }, true)]
    [TestCase("foo\r\n\r\nbar", new[] { "foo", "bar" }, true, true)]
    public void SkipBlanks(string payload, string[] result, bool suppressBlanks, bool crlf = false, char sep = '\n')
    {
        var lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)), sep, suppressBlanks, crlf: crlf);
        var syncresult = lr.ReadLines().ToArray();
        Assert.That(syncresult, Is.EqualTo(result));
    }

    [Test]
    public void PreCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var lr = new LineReader(new MemoryStream("foo"u8.ToArray()));
        Assert.ThrowsAsync<TaskCanceledException>(async () => await ToArray(lr.ReadLines(cts.Token)));
    }
}
