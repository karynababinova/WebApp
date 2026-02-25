using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class Chapter : Entity
{

    public int FanficId { get; set; }

    public string Title { get; set; } = null!;

    public int ChapterNumber { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual Fanfic Fanfic { get; set; } = null!;
}
