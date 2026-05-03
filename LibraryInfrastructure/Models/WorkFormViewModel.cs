using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LibraryInfrastructure.Models;

public class WorkFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(255)]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Display(Name = "Рейтинг")]
    public int? ContentRatingId { get; set; }

    [Display(Name = "Теги")]
    public List<int> SelectedTagIds { get; set; } = new();

    [Display(Name = "Назва першого розділу")]
    public string? FirstChapterTitle { get; set; }

    [Display(Name = "Текст першого розділу")]
    public string? FirstChapterContent { get; set; }
}
