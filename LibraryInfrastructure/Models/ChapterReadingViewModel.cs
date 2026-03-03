using System.Collections.Generic;
using LibraryDomain.Model;

namespace LibraryInfrastructure.Models;

public class ChapterReadingViewModel
{
    public Chapter Chapter { get; set; } = null!;

    public IReadOnlyList<Comment> Comments { get; set; } = new List<Comment>();

    public int? PreviousChapterId { get; set; }

    public int? NextChapterId { get; set; }

    public bool CanManage { get; set; }
}
