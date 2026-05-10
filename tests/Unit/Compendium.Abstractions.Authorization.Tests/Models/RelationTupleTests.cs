// -----------------------------------------------------------------------
// <copyright file="RelationTupleTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Authorization.Tests.Models;

public class RelationTupleTests
{
    [Fact]
    public void RelationTuple_WithAllFields_PreservesValues()
    {
        // Arrange / Act
        var tuple = new RelationTuple("user:alice", "viewer", "doc:readme");

        // Assert
        tuple.Subject.Should().Be("user:alice");
        tuple.Relation.Should().Be("viewer");
        tuple.Object.Should().Be("doc:readme");
    }

    [Theory]
    [InlineData("user:alice", "viewer", "doc:readme")]
    [InlineData("group:eng#member", "editor", "folder:specs")]
    [InlineData("user:*", "viewer", "doc:public")]
    public void RelationTuple_WithVariousSubjectShapes_PreservesValues(string subject, string relation, string @object)
    {
        // Arrange / Act
        var tuple = new RelationTuple(subject, relation, @object);

        // Assert
        tuple.Subject.Should().Be(subject);
        tuple.Relation.Should().Be(relation);
        tuple.Object.Should().Be(@object);
    }

    [Fact]
    public void RelationTuple_RecordEquality_TwoIdenticalTuples_AreEqual()
    {
        // Arrange
        var first = new RelationTuple("user:1", "owner", "doc:1");
        var second = new RelationTuple("user:1", "owner", "doc:1");

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void RelationTuple_RecordEquality_DifferingRelation_AreNotEqual()
    {
        // Arrange
        var first = new RelationTuple("user:1", "viewer", "doc:1");
        var second = new RelationTuple("user:1", "editor", "doc:1");

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void RelationTuple_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new RelationTuple("user:1", "viewer", "doc:1");

        // Act
        var updated = original with { Relation = "editor" };

        // Assert
        updated.Relation.Should().Be("editor");
        original.Relation.Should().Be("viewer");
    }

    [Fact]
    public void RelationTuple_Deconstruction_YieldsAllPositionalFields()
    {
        // Arrange
        var tuple = new RelationTuple("user:1", "viewer", "doc:1");

        // Act
        var (subject, relation, @object) = tuple;

        // Assert
        subject.Should().Be("user:1");
        relation.Should().Be("viewer");
        @object.Should().Be("doc:1");
    }
}
