using System.Collections.Generic;

namespace LibraryInfrastructure.Models;

public class HomeDashboardViewModel
{
    public int TotalWorks { get; set; }

    public int TotalChapters { get; set; }

    public int TotalComments { get; set; }

    public int TotalTags { get; set; }

    public List<ChartItemViewModel> WorksByRating { get; set; } = new();

    public List<ChartItemViewModel> TopTags { get; set; } = new();

    public List<WorkEngagementViewModel> WorkEngagement { get; set; } = new();

    public List<RecentWorkViewModel> RecentWorks { get; set; } = new();
}

public class ChartItemViewModel
{
    public string Label { get; set; } = string.Empty;

    public int Value { get; set; }
}

public class WorkEngagementViewModel
{
    public int FanficId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int ChaptersCount { get; set; }

    public int CommentsCount { get; set; }

    public int LikesCount { get; set; }

    public int BookmarksCount { get; set; }

    public int TotalEngagement => ChaptersCount + CommentsCount + LikesCount + BookmarksCount;
}

public class RecentWorkViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string RatingName { get; set; } = string.Empty;
}
