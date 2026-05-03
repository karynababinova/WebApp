using System.Diagnostics;
using LibraryInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace LibraryInfrastructure.Controllers;

public class HomeController : Controller
{
    private readonly DbLibraryContext _context;

    public HomeController(DbLibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeDashboardViewModel
        {
            TotalWorks = await _context.Fanfics.CountAsync(),
            TotalChapters = await _context.Chapters.CountAsync(),
            TotalComments = await _context.Comments.CountAsync(c => !c.IsDeleted),
            TotalTags = await _context.Tags.CountAsync(),
            WorksByRating = await _context.ContentRatings
                .AsNoTracking()
                .Select(r => new ChartItemViewModel
                {
                    Label = r.Name,
                    Value = r.Fanfics.Count
                })
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Label)
                .ToListAsync(),
            TopTags = await _context.Tags
                .AsNoTracking()
                .Select(t => new ChartItemViewModel
                {
                    Label = t.Name,
                    Value = t.FanficTags.Count
                })
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Label)
                .Take(6)
                .ToListAsync(),
            WorkEngagement = await _context.Fanfics
                .AsNoTracking()
                .Select(f => new WorkEngagementViewModel
                {
                    FanficId = f.Id,
                    Title = f.Title,
                    ChaptersCount = f.Chapters.Count,
                    CommentsCount = f.Comments.Count(c => !c.IsDeleted),
                    LikesCount = f.Likes.Count,
                    BookmarksCount = f.Bookmarks.Count
                })
                .ToListAsync(),
            RecentWorks = await _context.Fanfics
                .AsNoTracking()
                .Include(f => f.ContentRating)
                .OrderByDescending(f => f.UpdatedAt)
                .Take(4)
                .Select(f => new RecentWorkViewModel
                {
                    Id = f.Id,
                    Title = f.Title,
                    Description = f.Description,
                    RatingName = f.ContentRating != null ? f.ContentRating.Name : "Без рейтингу"
                })
                .ToListAsync()
        };

        model.WorkEngagement = model.WorkEngagement
            .OrderByDescending(x => x.TotalEngagement)
            .ThenBy(x => x.Title)
            .Take(5)
            .ToList();

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
