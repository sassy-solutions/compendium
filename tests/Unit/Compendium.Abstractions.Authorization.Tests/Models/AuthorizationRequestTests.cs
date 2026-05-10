// -----------------------------------------------------------------------
// <copyright file="AuthorizationRequestTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Authorization.Tests.Models;

public class AuthorizationRequestTests
{
    [Fact]
    public void AuthorizationRequest_WithRequiredOnly_LeavesContextualTuplesNull()
    {
        // Arrange / Act
        var request = new AuthorizationRequest("user:1", "viewer", "doc:1", "tenant-a");

        // Assert
        request.Subject.Should().Be("user:1");
        request.Relation.Should().Be("viewer");
        request.Object.Should().Be("doc:1");
        request.TenantId.Should().Be("tenant-a");
        request.ContextualTuples.Should().BeNull();
    }

    [Fact]
    public void AuthorizationRequest_WithContextualTuples_PreservesList()
    {
        // Arrange
        var ctx = new[]
        {
            new RelationTuple("user:1", "member", "group:eng"),
            new RelationTuple("group:eng", "viewer", "doc:1"),
        };

        // Act
        var request = new AuthorizationRequest("user:1", "viewer", "doc:1", "tenant-a", ctx);

        // Assert
        request.ContextualTuples.Should().NotBeNull();
        request.ContextualTuples!.Should().HaveCount(2);
        request.ContextualTuples!.Should().ContainInOrder(ctx);
    }

    [Theory]
    [InlineData("user:1", "viewer", "doc:1", "tenant-a")]
    [InlineData("group:eng#member", "editor", "folder:specs", "tenant-b")]
    [InlineData("user:*", "viewer", "doc:public", "tenant-c")]
    public void AuthorizationRequest_AcrossShapes_PreservesAllFields(string subject, string relation, string @object, string tenantId)
    {
        // Arrange / Act
        var request = new AuthorizationRequest(subject, relation, @object, tenantId);

        // Assert
        request.Subject.Should().Be(subject);
        request.Relation.Should().Be(relation);
        request.Object.Should().Be(@object);
        request.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void AuthorizationRequest_RecordEquality_TwoIdenticalRequests_AreEqual()
    {
        // Arrange
        var ctx = new[] { new RelationTuple("user:1", "member", "group:eng") };
        var first = new AuthorizationRequest("user:1", "viewer", "doc:1", "tenant-a", ctx);
        var second = first with { };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void AuthorizationRequest_RecordEquality_DifferingTenant_AreNotEqual()
    {
        // Arrange
        var first = new AuthorizationRequest("user:1", "viewer", "doc:1", "tenant-a");
        var second = new AuthorizationRequest("user:1", "viewer", "doc:1", "tenant-b");

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void AuthorizationRequest_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new AuthorizationRequest("user:1", "viewer", "doc:1", "tenant-a");

        // Act
        var updated = original with { Relation = "editor" };

        // Assert
        updated.Relation.Should().Be("editor");
        original.Relation.Should().Be("viewer");
    }

    [Fact]
    public void AuthorizationRequest_Deconstruction_YieldsAllPositionalFields()
    {
        // Arrange
        var ctx = new[] { new RelationTuple("user:1", "member", "group:eng") };
        var request = new AuthorizationRequest("user:1", "viewer", "doc:1", "tenant-a", ctx);

        // Act
        var (subject, relation, @object, tenantId, contextual) = request;

        // Assert
        subject.Should().Be("user:1");
        relation.Should().Be("viewer");
        @object.Should().Be("doc:1");
        tenantId.Should().Be("tenant-a");
        contextual.Should().BeEquivalentTo(ctx);
    }
}
