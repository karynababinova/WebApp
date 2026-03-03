using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryDomain.Model;
using LibraryInfrastructure.Models;
using LibraryInfrastructure.Security;
using LibraryInfrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Controllers;

public class FanficsController : Controller
{
    private readonly DbLibraryContext _context;
    private readonly IDataPortServiceFactory<Fanfic> _fanficDataPortServiceFactory;
    private readonly IFanficExcelTemplateService _fanficExcelTemplateService;

    public FanficsController(
        DbLibraryContext context,
        IDataPortServiceFactory<Fanfic> fanficDataPortServiceFactory,
        IFanficExcelTemplateService fanficExcelTemplateService)
    {
        _context = context;
        _fanficDataPortServiceFactory = fanficDataPortServiceFactory;
        _fanficExcelTemplateService = fanficExcelTemplateService;
    }

    public async Task<IActionResult> Index(int? tagId, string? q)
    {
        var query = _context.Fanfics
            .AsNoTracking()
            .Include(f => f.ContentRating)
            .Include(f => f.FanficTags)
                .ThenInclude(ft => ft.Tag)
            .AsQueryable();

        if (tagId.HasValue)
        {
            query = query.Where(f => f.FanficTags.Any(ft => ft.TagId == tagId.Value));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(f => f.Title.ToLower().Contains(needle)
                || (f.Description != null && f.Description.ToLower().Contains(needle)));
        }

        var works = await query
            .OrderByDescending(f => f.UpdatedAt)
            .ToListAsync();

        ViewData["AllTags"] = await _context.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();
        ViewData["SelectedTagId"] = tagId;
        ViewData["Search"] = q;

        return View(works);
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [HttpGet]
    public IActionResult Import()
    {
        return View();
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? fileExcel, CancellationToken cancellationToken)
    {
        if (fileExcel == null || fileExcel.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Оберіть Excel-файл для імпорту.");
            return View();
        }

        try
        {
            await using var stream = fileExcel.OpenReadStream();
            var importService = _fanficDataPortServiceFactory.GetImportService(ResolveExcelContentType(fileExcel));
            await importService.ImportFromStreamAsync(stream, cancellationToken);
            TempData["FanficImportSuccess"] = "Excel-файл успішно імпортовано.";
            return RedirectToAction(nameof(Index));
        }
        catch (ExcelDataPortException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View();
        }
        catch (NotSupportedException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View();
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Не вдалося обробити Excel-файл. Перевірте структуру шаблону та повторіть спробу.");
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        [FromQuery] string contentType = ExcelContentTypes.Xlsx,
        CancellationToken cancellationToken = default)
    {
        var exportService = _fanficDataPortServiceFactory.GetExportService(contentType);
        await using var memoryStream = new MemoryStream();

        await exportService.WriteToAsync(memoryStream, cancellationToken);
        await memoryStream.FlushAsync(cancellationToken);
        memoryStream.Position = 0;

        return File(
            memoryStream.ToArray(),
            contentType,
            $"tvory_export_{DateTime.Now:yyyy-MM-dd}.xlsx");
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await _fanficExcelTemplateService.WriteTemplateAsync(memoryStream, cancellationToken);
        await memoryStream.FlushAsync(cancellationToken);
        memoryStream.Position = 0;

        return File(
            memoryStream.ToArray(),
            ExcelContentTypes.Xlsx,
            "tvory_import_shablon.xlsx");
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var fanfic = await _context.Fanfics
            .AsNoTracking()
            .Include(f => f.ContentRating)
            .Include(f => f.FanficTags)
                .ThenInclude(ft => ft.Tag)
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id.Value);

        if (fanfic == null)
        {
            return NotFound();
        }

        var chapters = await _context.Chapters
            .AsNoTracking()
            .Where(c => c.FanficId == fanfic.Id)
            .OrderBy(c => c.ChapterNumber)
            .ToListAsync();

        var comments = await _context.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.FanficId == fanfic.Id && c.ChapterId == null && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var currentUserId = User.GetCurrentUserId();
        var likesCount = await _context.Likes.CountAsync(l => l.FanficId == fanfic.Id);
        var bookmarksCount = await _context.Bookmarks.CountAsync(b => b.FanficId == fanfic.Id);

        var isLiked = currentUserId.HasValue
            && await _context.Likes.AnyAsync(l => l.FanficId == fanfic.Id && l.UserId == currentUserId.Value);
        var isBookmarked = currentUserId.HasValue
            && await _context.Bookmarks.AnyAsync(b => b.FanficId == fanfic.Id && b.UserId == currentUserId.Value);

        var viewModel = new FanficDetailsViewModel
        {
            Fanfic = fanfic,
            Chapters = chapters,
            Comments = comments,
            FirstChapterId = chapters.FirstOrDefault()?.Id,
            LikesCount = likesCount,
            BookmarksCount = bookmarksCount,
            IsLikedByCurrentUser = isLiked,
            IsBookmarkedByCurrentUser = isBookmarked,
            CanManageChapters = CanManage(fanfic.UserId)
        };

        return View(viewModel);
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    public async Task<IActionResult> Create()
    {
        await FillWorkFormLookupsAsync();
        return View(new WorkFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkFormViewModel model)
    {
        var currentUserId = User.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(model.FirstChapterTitle))
        {
            ModelState.AddModelError(nameof(model.FirstChapterTitle), "Вкажіть назву першого розділу.");
        }

        if (string.IsNullOrWhiteSpace(model.FirstChapterContent))
        {
            ModelState.AddModelError(nameof(model.FirstChapterContent), "Додайте текст першого розділу.");
        }

        if (!ModelState.IsValid)
        {
            await FillWorkFormLookupsAsync(model.ContentRatingId, model.SelectedTagIds);
            return View(model);
        }

        var draftStatus = await _context.FanficStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == "draft")
            ?? await _context.FanficStatuses.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();

        if (draftStatus == null)
        {
            ModelState.AddModelError(string.Empty, "Не знайдено жодного статусу твору.");
            await FillWorkFormLookupsAsync(model.ContentRatingId, model.SelectedTagIds);
            return View(model);
        }

        var now = DateTime.Now;
        var fanfic = new Fanfic
        {
            Title = model.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            ContentRatingId = model.ContentRatingId,
            UserId = currentUserId.Value,
            StatusId = draftStatus.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Fanfics.Add(fanfic);
        await _context.SaveChangesAsync();

        await SyncTagsAsync(fanfic.Id, model.SelectedTagIds);

        _context.Chapters.Add(new Chapter
        {
            FanficId = fanfic.Id,
            Title = model.FirstChapterTitle!.Trim(),
            ChapterNumber = 1,
            Content = model.FirstChapterContent!.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = fanfic.Id });
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var fanfic = await _context.Fanfics
            .AsNoTracking()
            .Include(f => f.FanficTags)
            .FirstOrDefaultAsync(f => f.Id == id.Value);

        if (fanfic == null)
        {
            return NotFound();
        }

        if (!CanManage(fanfic.UserId))
        {
            return Forbid();
        }

        var model = new WorkFormViewModel
        {
            Id = fanfic.Id,
            Title = fanfic.Title,
            Description = fanfic.Description,
            ContentRatingId = fanfic.ContentRatingId,
            SelectedTagIds = fanfic.FanficTags.Select(t => t.TagId).ToList()
        };

        await FillWorkFormLookupsAsync(model.ContentRatingId, model.SelectedTagIds);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkFormViewModel model)
    {
        if (model.Id != id)
        {
            return NotFound();
        }

        var existing = await _context.Fanfics.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        if (!CanManage(existing.UserId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await FillWorkFormLookupsAsync(model.ContentRatingId, model.SelectedTagIds);
            return View(model);
        }

        var fanfic = new Fanfic
        {
            Id = existing.Id,
            UserId = existing.UserId,
            StatusId = existing.StatusId,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.Now,
            Title = model.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            ContentRatingId = model.ContentRatingId
        };

        _context.Fanfics.Update(fanfic);
        await _context.SaveChangesAsync();

        await SyncTagsAsync(fanfic.Id, model.SelectedTagIds);

        return RedirectToAction(nameof(Details), new { id = fanfic.Id });
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var fanfic = await _context.Fanfics
            .AsNoTracking()
            .Include(f => f.ContentRating)
            .FirstOrDefaultAsync(f => f.Id == id.Value);

        if (fanfic == null)
        {
            return NotFound();
        }

        if (!CanManage(fanfic.UserId))
        {
            return Forbid();
        }

        return View(fanfic);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var fanfic = await _context.Fanfics.FindAsync(id);
        if (fanfic == null)
        {
            return NotFound();
        }

        if (!CanManage(fanfic.UserId))
        {
            return Forbid();
        }

        _context.Fanfics.Remove(fanfic);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var currentUserId = User.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        var fanficExists = await _context.Fanfics.AnyAsync(f => f.Id == id);
        if (!fanficExists)
        {
            return NotFound();
        }

        var existing = await _context.Likes.FirstOrDefaultAsync(l => l.UserId == currentUserId.Value && l.FanficId == id);
        if (existing == null)
        {
            _context.Likes.Add(new Like
            {
                UserId = currentUserId.Value,
                FanficId = id,
                CreatedAt = DateTime.Now
            });
        }
        else
        {
            _context.Likes.Remove(existing);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBookmark(int id)
    {
        var currentUserId = User.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        var fanficExists = await _context.Fanfics.AnyAsync(f => f.Id == id);
        if (!fanficExists)
        {
            return NotFound();
        }

        var existing = await _context.Bookmarks.FirstOrDefaultAsync(l => l.UserId == currentUserId.Value && l.FanficId == id);
        if (existing == null)
        {
            _context.Bookmarks.Add(new Bookmark
            {
                UserId = currentUserId.Value,
                FanficId = id,
                CreatedAt = DateTime.Now
            });
        }
        else
        {
            _context.Bookmarks.Remove(existing);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task FillWorkFormLookupsAsync(int? selectedRatingId = null, IEnumerable<int>? selectedTagIds = null)
    {
        ViewData["ContentRatingId"] = new SelectList(
            await _context.ContentRatings.AsNoTracking().OrderBy(r => r.Name).ToListAsync(),
            "Id",
            "Name",
            selectedRatingId);

        var selected = selectedTagIds?.ToHashSet() ?? new HashSet<int>();
        ViewData["Tags"] = await _context.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name,
                Selected = selected.Contains(t.Id)
            })
            .ToListAsync();
    }

    private async Task SyncTagsAsync(int fanficId, IEnumerable<int>? selectedTagIds)
    {
        var newIds = (selectedTagIds ?? Enumerable.Empty<int>()).Distinct().ToHashSet();
        var current = await _context.FanficTags.Where(ft => ft.FanficId == fanficId).ToListAsync();

        if (current.Count > 0)
        {
            _context.FanficTags.RemoveRange(current);
        }

        foreach (var tagId in newIds)
        {
            _context.FanficTags.Add(new FanficTag
            {
                FanficId = fanficId,
                TagId = tagId,
                CreatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
    }

    private bool CanManage(int ownerUserId)
    {
        if (User.IsAdmin())
        {
            return true;
        }

        var currentUserId = User.GetCurrentUserId();
        return currentUserId.HasValue && currentUserId.Value == ownerUserId;
    }

    private static string ResolveExcelContentType(IFormFile file)
    {
        if (string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return ExcelContentTypes.Xlsx;
        }

        return string.IsNullOrWhiteSpace(file.ContentType)
            ? ExcelContentTypes.Xlsx
            : file.ContentType;
    }
}
