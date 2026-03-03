using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LibraryDomain.Model;

public partial class Chapter : Entity
{

    public int FanficId { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Chapter number must be at least 1.")]
    public int ChapterNumber { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual Fanfic Fanfic { get; set; } = null!;
}
