using System.ComponentModel.DataAnnotations;
using LibraryDomain.Model;

namespace LibraryInfrastructure.Models;

public class ProfileViewModel
{
    [Required]
    [StringLength(50)]
    [Display(Name = "Ім'я користувача")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Про себе")]
    public string? Bio { get; set; }

    public string Role { get; set; } = string.Empty;

    public string RoleDisplayName { get; set; } = string.Empty;

    public IReadOnlyList<Fanfic> MyWorks { get; set; } = new List<Fanfic>();
}
