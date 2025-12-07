using System.IO;
using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class AiFileAttachmentTests
{
    private class NonSeekStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void Length_UsesBytes_WhenProvided()
    {
        var a = new AiFileAttachment { Bytes = new byte[10] };
        a.Length.Should().Be(10);
    }

    [Fact]
    public void Length_UsesProvidedLength_WhenNoBytes()
    {
        var a = new AiFileAttachment { ProvidedLength = 42 };
        a.Length.Should().Be(42);
    }

    [Fact]
    public void Length_UsesStreamLength_WhenSeekable()
    {
        using var ms = new MemoryStream(new byte[7]);
        var a = new AiFileAttachment { Stream = ms };
        a.Length.Should().Be(7);
    }

    [Fact]
    public void Length_ReturnsZero_WhenStreamNotSeekable()
    {
        using var s = new NonSeekStream();
        var a = new AiFileAttachment { Stream = s };
        a.Length.Should().Be(0);
    }
}