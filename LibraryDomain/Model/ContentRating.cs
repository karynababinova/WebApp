using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class ContentRating : Entity
{

    public string Name { get; set; } = null!;

    public virtual ICollection<Fanfic> Fanfics { get; set; } = new List<Fanfic>();
}
