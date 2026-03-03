using System.Collections.Generic;
using LibraryDomain.Model;

namespace LibraryInfrastructure.Models;

public class FanficDetailsViewModel
{
    public Fanfic Fanfic { get; set; } = null!;

    public IReadOnlyList<Chapter> Chapters { get; set; } = new List<Chapter>();

    public IReadOnlyList<Comment> Comments { get; set; } = new List<Comment>();

    public int? FirstChapterId { get; set; }

    public int LikesCount { get; set; }

    public int BookmarksCount { get; set; }

    public bool IsLikedByCurrentUser { get; set; }

    public bool IsBookmarkedByCurrentUser { get; set; }

    public bool CanManageChapters { get; set; }
}
