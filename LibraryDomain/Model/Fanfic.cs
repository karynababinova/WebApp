using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class Fanfic : Entity
{

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int StatusId { get; set; }

    public int? ContentRatingId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ContentRating? ContentRating { get; set; }

    public virtual ICollection<FanficTag> FanficTags { get; set; } = new List<FanficTag>();

    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    public virtual FanficStatus Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
