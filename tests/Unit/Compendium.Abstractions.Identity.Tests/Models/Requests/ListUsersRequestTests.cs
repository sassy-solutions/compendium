// -----------------------------------------------------------------------
// <copyright file="ListUsersRequestTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models.Requests;

public class ListUsersRequestTests
{
    [Fact]
    public void ListUsersRequest_Defaults_HaveExpectedValues()
    {
        // Arrange / Act
        var request = new ListUsersRequest();

        // Assert
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.SearchQuery.Should().BeNull();
        request.OrganizationId.Should().BeNull();
        request.Role.Should().BeNull();
        request.IsActive.Should().BeNull();
        request.SortBy.Should().Be("email");
        request.SortDescending.Should().BeFalse();
    }

    [Fact]
    public void ListUsersRequest_FullyPopulated_RoundTripsAllProperties()
    {
        // Arrange / Act
        var request = new ListUsersRequest
        {
            Page = 5,
            PageSize = 100,
            SearchQuery = "alice",
            OrganizationId = "org-1",
            Role = "admin",
            IsActive = true,
            SortBy = "createdAt",
            SortDescending = true
        };

        // Assert
        request.Page.Should().Be(5);
        request.PageSize.Should().Be(100);
        request.SearchQuery.Should().Be("alice");
        request.OrganizationId.Should().Be("org-1");
        request.Role.Should().Be("admin");
        request.IsActive.Should().BeTrue();
        request.SortBy.Should().Be("createdAt");
        request.SortDescending.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ListUsersRequest_IsActive_AcceptsBothBooleanValues(bool isActive)
    {
        // Arrange / Act
        var request = new ListUsersRequest { IsActive = isActive };

        // Assert
        request.IsActive.Should().Be(isActive);
    }

    [Fact]
    public void ListUsersRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new ListUsersRequest { Page = 2, PageSize = 50 };
        var second = new ListUsersRequest { Page = 2, PageSize = 50 };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
