using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class Comment : Entity
{
    public int FanficId { get; set; }

    public int? ChapterId { get; set; }

    public int UserId { get; set; }

    public int? ParentCommentId { get; set; }

    public string Text { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Chapter? Chapter { get; set; }

    public virtual Fanfic Fanfic { get; set; } = null!;

    public virtual ICollection<Comment> InverseParentComment { get; set; } = new List<Comment>();

    public virtual Comment? ParentComment { get; set; }

    public virtual User User { get; set; } = null!;
}
