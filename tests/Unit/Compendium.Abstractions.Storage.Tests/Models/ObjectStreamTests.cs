// -----------------------------------------------------------------------
// <copyright file="ObjectStreamTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Tests.Models;

public class ObjectStreamTests
{
    private static ObjectInfo SampleInfo() => new(
        "k",
        4,
        "etag",
        "text/plain",
        new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ObjectStream_Constructor_StoresContentAndInfo()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var info = SampleInfo();

        // Act
        using var stream = new ObjectStream(content, info);

        // Assert
        stream.Content.Should().BeSameAs(content);
        stream.Info.Should().BeSameAs(info);
    }

    [Fact]
    public void ObjectStream_Constructor_NullContent_Throws()
    {
        // Arrange
        var info = SampleInfo();

        // Act
        Action act = () => _ = new ObjectStream(null!, info);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("content");
    }

    [Fact]
    public void ObjectStream_Constructor_NullInfo_Throws()
    {
        // Arrange
        using var content = new MemoryStream();

        // Act
        Action act = () => _ = new ObjectStream(content, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("info");
    }

    [Fact]
    public void ObjectStream_Dispose_DisposesUnderlyingStream()
    {
        // Arrange
        var content = new TrackingStream();
        var stream = new ObjectStream(content, SampleInfo());

        // Act
        stream.Dispose();

        // Assert
        content.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void ObjectStream_Dispose_CalledTwice_OnlyDisposesOnce()
    {
        // Arrange
        var content = new TrackingStream();
        var stream = new ObjectStream(content, SampleInfo());

        // Act
        stream.Dispose();
        stream.Dispose();

        // Assert
        content.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void ObjectStream_Content_AfterDispose_Throws()
    {
        // Arrange
        var stream = new ObjectStream(new MemoryStream(), SampleInfo());
        stream.Dispose();

        // Act
        Action act = () => _ = stream.Content;

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task ObjectStream_DisposeAsync_DisposesUnderlyingStream()
    {
        // Arrange
        var content = new TrackingStream();
        var stream = new ObjectStream(content, SampleInfo());

        // Act
        await stream.DisposeAsync();

        // Assert
        content.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task ObjectStream_DisposeAsync_CalledTwice_OnlyDisposesOnce()
    {
        // Arrange
        var content = new TrackingStream();
        var stream = new ObjectStream(content, SampleInfo());

        // Act
        await stream.DisposeAsync();
        await stream.DisposeAsync();

        // Assert
        content.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void ObjectStream_Info_RemainsAccessibleAfterDispose()
    {
        // Arrange
        var info = SampleInfo();
        var stream = new ObjectStream(new MemoryStream(), info);

        // Act
        stream.Dispose();

        // Assert
        stream.Info.Should().BeSameAs(info);
    }

    private sealed class TrackingStream : MemoryStream
    {
        public int DisposeCount { get; private set; }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
