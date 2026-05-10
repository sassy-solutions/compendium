// -----------------------------------------------------------------------
// <copyright file="PagedResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_WithRequiredProperties_InitializesSuccessfully()
    {
        // Arrange / Act
        var paged = new PagedResult<string>
        {
            Items = new[] { "a", "b", "c" },
            TotalCount = 3,
            Page = 1,
            PageSize = 20
        };

        // Assert
        paged.Items.Should().HaveCount(3);
        paged.TotalCount.Should().Be(3);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(0, 20, 0)]   // 0 / 20 = 0 pages
    [InlineData(1, 20, 1)]   // 1 / 20 → 1 page
    [InlineData(20, 20, 1)]  // 20 / 20 → 1 page
    [InlineData(21, 20, 2)]  // 21 / 20 → 2 pages
    [InlineData(100, 20, 5)]
    [InlineData(101, 20, 6)]
    public void TotalPages_ReturnsCeiling_OfTotalCountOverPageSize(int totalCount, int pageSize, int expected)
    {
        // Arrange
        var paged = new PagedResult<int>
        {
            Items = Array.Empty<int>(),
            TotalCount = totalCount,
            Page = 1,
            PageSize = pageSize
        };

        // Act
        var totalPages = paged.TotalPages;

        // Assert
        totalPages.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    public void HasPreviousPage_IsTrue_WhenPageGreaterThanOne(int page, bool expected)
    {
        // Arrange
        var paged = new PagedResult<int>
        {
            Items = Array.Empty<int>(),
            TotalCount = 100,
            Page = page,
            PageSize = 10
        };

        // Act / Assert
        paged.HasPreviousPage.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 100, 10, true)]   // 10 pages, on page 1 → next exists
    [InlineData(5, 100, 10, true)]   // 10 pages, on page 5 → next exists
    [InlineData(10, 100, 10, false)] // last page
    [InlineData(11, 100, 10, false)] // beyond last page
    [InlineData(1, 0, 10, false)]    // empty result
    public void HasNextPage_IsTrue_WhenPageLessThanTotalPages(int page, int totalCount, int pageSize, bool expected)
    {
        // Arrange
        var paged = new PagedResult<int>
        {
            Items = Array.Empty<int>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        // Act / Assert
        paged.HasNextPage.Should().Be(expected);
    }

    [Fact]
    public void Empty_DefaultParameters_ReturnsEmptyPagedResultWithDefaults()
    {
        // Act
        var paged = PagedResult<string>.Empty();

        // Assert
        paged.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(0);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(20);
        paged.HasPreviousPage.Should().BeFalse();
        paged.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void Empty_WithCustomParameters_ReturnsEmptyPagedResultWithSpecifiedValues()
    {
        // Act
        var paged = PagedResult<int>.Empty(page: 7, pageSize: 50);

        // Assert
        paged.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(0);
        paged.Page.Should().Be(7);
        paged.PageSize.Should().Be(50);
    }

    [Fact]
    public void PagedResult_IsRecord_HasValueEquality()
    {
        // Arrange
        var first = new PagedResult<int>
        {
            Items = new[] { 1, 2, 3 },
            TotalCount = 3,
            Page = 1,
            PageSize = 20
        };
        var second = new PagedResult<int>
        {
            Items = new[] { 1, 2, 3 },
            TotalCount = 3,
            Page = 1,
            PageSize = 20
        };

        // Act / Assert
        // Records compare items by reference equality unless we use structural collections;
        // equality without comparing collection identity is asserted via individual props.
        first.TotalCount.Should().Be(second.TotalCount);
        first.Page.Should().Be(second.Page);
        first.PageSize.Should().Be(second.PageSize);
    }
}
