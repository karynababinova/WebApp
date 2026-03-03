using System;
using System.Linq;
using System.Threading.Tasks;
using LibraryDomain.Model;
using LibraryInfrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Controllers;

public class CommentsController : Controller
{
    private readonly DbLibraryContext _context;

    public CommentsController(DbLibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var comments = await _context.Comments
            .AsNoTracking()
            .Include(c => c.Chapter)
            .Include(c => c.Fanfic)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(comments);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var comment = await _context.Comments
            .AsNoTracking()
            .Include(c => c.Chapter)
            .Include(c => c.Fanfic)
            .FirstOrDefaultAsync(c => c.Id == id.Value);

        if (comment == null)
        {
            return NotFound();
        }

        return View(comment);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateForChapter(int chapterId, string text)
    {
        var currentUserId = User.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        var chapter = await _context.Chapters
            .AsNoTracking()
            .Where(c => c.Id == chapterId)
            .Select(c => new { c.Id, c.FanficId })
            .FirstOrDefaultAsync();

        if (chapter == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            TempData["CommentError"] = "Текст коментаря не може бути порожнім.";
            return RedirectToAction("Details", "Chapters", new { id = chapterId });
        }

        _context.Comments.Add(new Comment
        {
            Text = text.Trim(),
            FanficId = chapter.FanficId,
            ChapterId = chapter.Id,
            UserId = currentUserId.Value,
            ParentCommentId = null,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Chapters", new { id = chapterId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateForFanfic(int fanficId, string text)
    {
        var currentUserId = User.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        var fanficExists = await _context.Fanfics.AnyAsync(f => f.Id == fanficId);
        if (!fanficExists)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            TempData["FanficCommentError"] = "Текст коментаря не може бути порожнім.";
            return RedirectToAction("Details", "Fanfics", new { id = fanficId });
        }

        _context.Comments.Add(new Comment
        {
            Text = text.Trim(),
            FanficId = fanficId,
            ChapterId = null,
            UserId = currentUserId.Value,
            ParentCommentId = null,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Fanfics", new { id = fanficId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMine(int id, int? fanficId, int? chapterId)
    {
        var currentUserId = User.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment == null)
        {
            return NotFound();
        }

        var canDelete = User.IsInRole(AppRoles.Admin) || comment.UserId == currentUserId.Value;
        if (!canDelete)
        {
            return Forbid();
        }

        comment.IsDeleted = true;
        await _context.SaveChangesAsync();

        if (chapterId.HasValue)
        {
            return RedirectToAction("Details", "Chapters", new { id = chapterId.Value });
        }

        if (fanficId.HasValue)
        {
            return RedirectToAction("Details", "Fanfics", new { id = fanficId.Value });
        }

        return RedirectToAction("Index", "Fanfics");
    }
}
