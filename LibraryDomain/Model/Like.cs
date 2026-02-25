using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class Like : Entity
{

    public int UserId { get; set; }

    public int FanficId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Fanfic Fanfic { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
