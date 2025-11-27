using NUnit.Framework;
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

    private static string[] ToArraySync(IAsyncEnumerable<string> gen) =>
        ToArray(gen).Result;

    [TestCase("", new string[] { }, '\n', true)]
    [TestCase("", new[] { "" }, '\n', false)]
    [TestCase("\n\n", new string[] { }, '\n', true)]
    [TestCase("\n\n", new[] { "", "", "" }, '\n', false)]
    [TestCase("foo\0bar", new[] { "foo\0bar" })]
    [TestCase("foo\0bar", new[] { "foo", "bar" }, '\0')]
    public void BasicTests(string payload, string[] result, char delim = '\n', bool skip = true)
    {
        var input = Encoding.UTF8.GetBytes(payload);

        using var lr1 = new LineReader(new MemoryStream(input), delim, skip);
        var syncresult = lr1.ReadLines().ToArray();
        Assert.That(syncresult, Is.EqualTo(result));

        // Test async version
        using var lr2 = new LineReader(new MemoryStream(input), delim, skip);
        var asyncresult = ToArraySync(lr2.ReadLines(new CancellationToken()));
        Assert.That(asyncresult, Is.EqualTo(result));

        // Test unbuffered too
        using var lr3 = new LineReader(new MemoryStream(input), delim,
            skip, false);
        syncresult = lr3.ReadLines().ToArray();
        Assert.That(syncresult, Is.EqualTo(result));
    }

    [TestCase("foo\n\nbar", new[] { "foo", "", "bar" }, false)]
    [TestCase("foo\n\nbar", new[] { "foo", "bar" }, true)]
    [TestCase("foo\r\n\r\nbar", new[] { "foo", "bar" }, true, true)]
    public void SkipBlanks(string payload, string[] result, bool suppressBlanks, bool crlf = false, char sep = '\n')
    {
        using var lr = new LineReader(new MemoryStream(Encoding.UTF8.GetBytes(payload)), sep, suppressBlanks, crlf: crlf);
        var syncresult = lr.ReadLines().ToArray();
        Assert.That(syncresult, Is.EqualTo(result));
    }

    [Test]
    public void PreCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using var lr = new LineReader(new MemoryStream("foo"u8.ToArray()));
        Assert.ThrowsAsync<TaskCanceledException>(async () => await ToArray(lr.ReadLines(cts.Token)));
    }
}
