// -----------------------------------------------------------------------
// <copyright file="MailingListTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Tests.Models;

public class MailingListTests
{
    [Fact]
    public void MailingList_WithRequiredProperties_CreatesInstanceWithDefaults()
    {
        // Arrange / Act
        var list = new MailingList
        {
            Id = "list-1",
            Slug = "general-news",
            Name = "General News",
        };

        // Assert
        list.Id.Should().Be("list-1");
        list.Slug.Should().Be("general-news");
        list.Name.Should().Be("General News");
        list.Description.Should().BeNull();
        list.IsPublic.Should().BeTrue();
        list.IsSingleOptIn.Should().BeFalse();
        list.SubscriberCount.Should().Be(0);
        list.CreatedAt.Should().Be(default);
        list.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void MailingList_WithAllProperties_PreservesValues()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var list = new MailingList
        {
            Id = "list-2",
            Slug = "exclusive",
            Name = "Exclusive Updates",
            Description = "Insider news",
            IsPublic = false,
            IsSingleOptIn = true,
            SubscriberCount = 1234,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };

        // Assert
        list.Id.Should().Be("list-2");
        list.Slug.Should().Be("exclusive");
        list.Name.Should().Be("Exclusive Updates");
        list.Description.Should().Be("Insider news");
        list.IsPublic.Should().BeFalse();
        list.IsSingleOptIn.Should().BeTrue();
        list.SubscriberCount.Should().Be(1234);
        list.CreatedAt.Should().Be(createdAt);
        list.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void MailingList_RecordEquality_IsValueBasedForScalarProperties()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var first = new MailingList
        {
            Id = "list-1",
            Slug = "news",
            Name = "News",
            CreatedAt = createdAt,
        };

        var second = new MailingList
        {
            Id = "list-1",
            Slug = "news",
            Name = "News",
            CreatedAt = createdAt,
        };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void MailingList_With_ReturnsCloneWithUpdatedField()
    {
        // Arrange
        var original = new MailingList { Id = "list-1", Slug = "news", Name = "News" };

        // Act
        var updated = original with { SubscriberCount = 100 };

        // Assert
        updated.SubscriberCount.Should().Be(100);
        original.SubscriberCount.Should().Be(0);
        updated.Id.Should().Be(original.Id);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void MailingList_PublicAndOptInFlags_AreIndependent(bool isPublic, bool isSingleOptIn)
    {
        // Arrange / Act
        var list = new MailingList
        {
            Id = "id",
            Slug = "slug",
            Name = "name",
            IsPublic = isPublic,
            IsSingleOptIn = isSingleOptIn,
        };

        // Assert
        list.IsPublic.Should().Be(isPublic);
        list.IsSingleOptIn.Should().Be(isSingleOptIn);
    }
}
