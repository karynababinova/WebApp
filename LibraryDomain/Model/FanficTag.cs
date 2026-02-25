using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class FanficTag : Entity
{

    public int FanficId { get; set; }

    public int TagId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Fanfic Fanfic { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}
