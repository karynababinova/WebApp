using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class Tag : Entity
{

    public string Name { get; set; } = null!;

    public virtual ICollection<FanficTag> FanficTags { get; set; } = new List<FanficTag>();
}
