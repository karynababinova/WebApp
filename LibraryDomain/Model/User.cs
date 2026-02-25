using System;
using System.Collections.Generic;

namespace LibraryDomain.Model;

public partial class User : Entity
{
    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Fanfic> Fanfics { get; set; } = new List<Fanfic>();

    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
}
