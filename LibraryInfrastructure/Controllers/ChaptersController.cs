using System;
using System.Linq;
using System.Threading.Tasks;
using LibraryDomain.Model;
using LibraryInfrastructure.Models;
using LibraryInfrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Controllers;

public class ChaptersController : Controller
{
    private readonly DbLibraryContext _context;

    public ChaptersController(DbLibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var chapters = await _context.Chapters
            .AsNoTracking()
            .Include(c => c.Fanfic)
            .OrderBy(c => c.FanficId)
            .ThenBy(c => c.ChapterNumber)
            .ToListAsync();

        return View(chapters);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var chapter = await _context.Chapters
            .AsNoTracking()
            .Include(c => c.Fanfic)
            .FirstOrDefaultAsync(c => c.Id == id.Value);

        if (chapter == null)
        {
            return NotFound();
        }

        var previousChapterId = await _context.Chapters
            .AsNoTracking()
            .Where(c => c.FanficId == chapter.FanficId && c.ChapterNumber < chapter.ChapterNumber)
            .OrderByDescending(c => c.ChapterNumber)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        var nextChapterId = await _context.Chapters
            .AsNoTracking()
            .Where(c => c.FanficId == chapter.FanficId && c.ChapterNumber > chapter.ChapterNumber)
            .OrderBy(c => c.ChapterNumber)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        var comments = await _context.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.ChapterId == chapter.Id && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var viewModel = new ChapterReadingViewModel
        {
            Chapter = chapter,
            Comments = comments,
            PreviousChapterId = previousChapterId,
            NextChapterId = nextChapterId,
            CanManage = CanManage(chapter.Fanfic.UserId)
        };

        return View(viewModel);
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    public async Task<IActionResult> Create(int? fanficId)
    {
        if (!fanficId.HasValue)
        {
            return BadRequest("fanficId is required.");
        }

        var fanfic = await _context.Fanfics.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fanficId.Value);
        if (fanfic == null)
        {
            return NotFound();
        }

        if (!CanManage(fanfic.UserId))
        {
            return Forbid();
        }

        ViewData["FanficId"] = fanfic.Id;
        ViewData["FanficTitle"] = fanfic.Title;
        return View(new Chapter { FanficId = fanfic.Id });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int fanficId, [Bind("Title,Content")] Chapter chapter)
    {
        // Fanfic is a navigation property and is not posted from the form.
        // We set FanficId explicitly below, so skip validating the navigation object.
        ModelState.Remove(nameof(Chapter.Fanfic));
        ModelState.Remove(nameof(Chapter.ChapterNumber));

        var fanfic = await _context.Fanfics.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fanficId);
        if (fanfic == null)
        {
            return NotFound();
        }

        if (!CanManage(fanfic.UserId))
        {
            return Forbid();
        }

        var nextChapterNumber = await _context.Chapters
            .AsNoTracking()
            .Where(c => c.FanficId == fanficId)
            .MaxAsync(c => (int?)c.ChapterNumber) ?? 0;

        chapter.FanficId = fanficId;
        chapter.ChapterNumber = nextChapterNumber + 1;
        
        if (!ModelState.IsValid)
        {
            ViewData["FanficId"] = fanfic.Id;
            ViewData["FanficTitle"] = fanfic.Title;
            return View(chapter);
        }

        var now = DateTime.Now;
        chapter.CreatedAt = now;
        chapter.UpdatedAt = now;

        _context.Chapters.Add(chapter);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Fanfics", new { id = fanficId });
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var chapter = await _context.Chapters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id.Value);
        if (chapter == null)
        {
            return NotFound();
        }

        var fanfic = await _context.Fanfics.AsNoTracking().FirstOrDefaultAsync(f => f.Id == chapter.FanficId);
        if (fanfic == null)
        {
            return NotFound();
        }

        if (!CanManage(fanfic.UserId))
        {
            return Forbid();
        }

        ViewData["FanficTitle"] = fanfic.Title;
        return View(chapter);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Title,Content,Id")] Chapter chapter)
    {
        // Fanfic is a navigation property and is not posted from the form.
        ModelState.Remove(nameof(Chapter.Fanfic));
        // ChapterNumber is not posted from the form anymore; keep existing value.
        ModelState.Remove(nameof(Chapter.ChapterNumber));

        if (id != chapter.Id)
        {
            return NotFound();
        }

        var existing = await _context.Chapters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        var fanfic = await _context.Fanfics.AsNoTracking().FirstOrDefaultAsync(f => f.Id == existing.FanficId);
        if (fanfic == null)
        {
            return NotFound();
        }

        if (!CanManage(fanfic.UserId))
        {
            return Forbid();
        }

        chapter.FanficId = existing.FanficId;
        chapter.ChapterNumber = existing.ChapterNumber;

        if (!ModelState.IsValid)
        {
            ViewData["FanficTitle"] = fanfic.Title;
            return View(chapter);
        }

        chapter.CreatedAt = existing.CreatedAt;
        chapter.UpdatedAt = DateTime.Now;

        _context.Chapters.Update(chapter);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Fanfics", new { id = chapter.FanficId });
    }

    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var chapter = await _context.Chapters
            .AsNoTracking()
            .Include(c => c.Fanfic)
            .FirstOrDefaultAsync(c => c.Id == id.Value);

        if (chapter == null)
        {
            return NotFound();
        }

        if (!CanManage(chapter.Fanfic.UserId))
        {
            return Forbid();
        }

        return View(chapter);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var chapter = await _context.Chapters.FindAsync(id);
        if (chapter == null)
        {
            return NotFound();
        }

        var fanfic = await _context.Fanfics.AsNoTracking().FirstOrDefaultAsync(f => f.Id == chapter.FanficId);
        if (fanfic == null)
        {
            return NotFound();
        }

        if (!CanManage(fanfic.UserId))
        {
            return Forbid();
        }

        _context.Chapters.Remove(chapter);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Fanfics", new { id = chapter.FanficId });
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
}
