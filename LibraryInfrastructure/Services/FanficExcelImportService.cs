using System.Globalization;
using System.Security.Claims;
using ClosedXML.Excel;
using LibraryDomain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Services;

public class FanficExcelImportService : IImportService<Fanfic>
{
    private readonly DbLibraryContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<string, ContentRating?> _ratings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Tag> _tags = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Fanfic> _works = new(StringComparer.OrdinalIgnoreCase);

    public FanficExcelImportService(DbLibraryContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanRead)
        {
            throw new ArgumentException("Дані Excel не можуть бути прочитані.", nameof(stream));
        }

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(ws =>
                string.Equals(ws.Name, FanficExcelSchema.WorksWorksheetName, StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            throw new ExcelDataPortException("У файлі не знайдено жодного аркуша для імпорту.");
        }

        var rows = worksheet.RowsUsed().Skip(1).ToList();
        if (rows.Count == 0)
        {
            throw new ExcelDataPortException("Excel-файл не містить рядків з даними.");
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        var draftStatus = await GetDraftStatusAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (IsRowEmpty(row))
            {
                continue;
            }

            await ImportRowAsync(row, user, draftStatus, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportRowAsync(
        IXLRow row,
        User user,
        FanficStatus draftStatus,
        CancellationToken cancellationToken)
    {
        var title = GetRequiredString(row, FanficExcelSchema.TitleColumn, "назву твору");
        var description = GetOptionalString(row, FanficExcelSchema.DescriptionColumn);
        var ratingName = GetOptionalString(row, FanficExcelSchema.RatingColumn);
        var tagList = GetOptionalString(row, FanficExcelSchema.TagsColumn);
        var chapterTitle = GetRequiredString(row, FanficExcelSchema.ChapterTitleColumn, "назву розділу");
        var chapterContent = GetRequiredString(row, FanficExcelSchema.ChapterContentColumn, "текст розділу");
        var chapterNumber = GetOptionalInt(row, FanficExcelSchema.ChapterNumberColumn);

        var fanfic = await GetOrCreateFanficAsync(title, description, ratingName, user, draftStatus, cancellationToken);
        await AttachTagsAsync(fanfic, tagList, cancellationToken);
        AddOrUpdateChapter(fanfic, chapterTitle, chapterContent, chapterNumber);
    }

    private async Task<Fanfic> GetOrCreateFanficAsync(
        string title,
        string? description,
        string? ratingName,
        User user,
        FanficStatus draftStatus,
        CancellationToken cancellationToken)
    {
        var key = $"{user.Id}:{title.Trim().ToLowerInvariant()}";
        if (!_works.TryGetValue(key, out var fanfic))
        {
            fanfic = await _context.Fanfics
                .Include(f => f.FanficTags)
                    .ThenInclude(ft => ft.Tag)
                .Include(f => f.Chapters)
                .FirstOrDefaultAsync(
                    f => f.UserId == user.Id && f.Title.ToLower() == title.Trim().ToLower(),
                    cancellationToken);

            if (fanfic == null)
            {
                fanfic = new Fanfic
                {
                    Title = title.Trim(),
                    Description = NormalizeOptional(description),
                    User = user,
                    Status = draftStatus,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                var rating = await ResolveRatingAsync(ratingName, cancellationToken);
                if (rating != null)
                {
                    fanfic.ContentRating = rating;
                }

                _context.Fanfics.Add(fanfic);
            }

            _works[key] = fanfic;
        }

        fanfic.Description = NormalizeOptional(description) ?? fanfic.Description;
        fanfic.ContentRating = await ResolveRatingAsync(ratingName, cancellationToken) ?? fanfic.ContentRating;
        fanfic.UpdatedAt = DateTime.Now;

        return fanfic;
    }

    private async Task AttachTagsAsync(Fanfic fanfic, string? rawTags, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawTags))
        {
            return;
        }

        foreach (var tagName in rawTags
                     .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var tag = await GetOrCreateTagAsync(tagName, cancellationToken);
            var alreadyAttached = fanfic.FanficTags.Any(ft =>
                string.Equals(ft.Tag?.Name, tag.Name, StringComparison.OrdinalIgnoreCase) ||
                (tag.Id != 0 && ft.TagId == tag.Id));

            if (!alreadyAttached)
            {
                fanfic.FanficTags.Add(new FanficTag
                {
                    Fanfic = fanfic,
                    Tag = tag,
                    CreatedAt = DateTime.Now
                });
            }
        }
    }

    private void AddOrUpdateChapter(Fanfic fanfic, string title, string content, int? chapterNumber)
    {
        var normalizedTitle = title.Trim();
        var targetNumber = chapterNumber.GetValueOrDefault();
        Chapter? chapter = null;

        if (targetNumber > 0)
        {
            chapter = fanfic.Chapters.FirstOrDefault(c => c.ChapterNumber == targetNumber);
        }

        chapter ??= fanfic.Chapters.FirstOrDefault(c =>
            string.Equals(c.Title, normalizedTitle, StringComparison.OrdinalIgnoreCase));

        if (chapter == null)
        {
            var nextNumber = targetNumber > 0
                ? targetNumber
                : (fanfic.Chapters.Count == 0 ? 1 : fanfic.Chapters.Max(c => c.ChapterNumber) + 1);

            chapter = new Chapter
            {
                Fanfic = fanfic,
                ChapterNumber = nextNumber,
                Title = normalizedTitle,
                Content = content.Trim(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            fanfic.Chapters.Add(chapter);
            return;
        }

        chapter.Title = normalizedTitle;
        chapter.Content = content.Trim();
        chapter.UpdatedAt = DateTime.Now;
    }

    private async Task<ContentRating?> ResolveRatingAsync(string? ratingName, CancellationToken cancellationToken)
    {
        var normalized = NormalizeOptional(ratingName);
        if (normalized == null)
        {
            return null;
        }

        if (_ratings.TryGetValue(normalized, out var cached))
        {
            return cached;
        }

        var rating = await _context.ContentRatings.FirstOrDefaultAsync(
            r => r.Name.ToLower() == normalized.ToLower(),
            cancellationToken);

        if (rating == null)
        {
            rating = new ContentRating
            {
                Name = TrimToLength(normalized, 32)
            };
            _context.ContentRatings.Add(rating);
        }

        _ratings[normalized] = rating;
        return rating;
    }

    private async Task<Tag> GetOrCreateTagAsync(string tagName, CancellationToken cancellationToken)
    {
        var normalized = TrimToLength(tagName.Trim(), 64);
        if (_tags.TryGetValue(normalized, out var cached))
        {
            return cached;
        }

        var tag = await _context.Tags.FirstOrDefaultAsync(
            t => t.Name.ToLower() == normalized.ToLower(),
            cancellationToken);

        if (tag == null)
        {
            tag = new Tag
            {
                Name = normalized
            };
            _context.Tags.Add(tag);
        }

        _tags[normalized] = tag;
        return tag;
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, CultureInfo.InvariantCulture, out var userId))
        {
            throw new ExcelDataPortException("Не вдалося визначити користувача для імпорту.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            throw new ExcelDataPortException("Користувача для імпорту не знайдено у базі даних.");
        }

        return user;
    }

    private async Task<FanficStatus> GetDraftStatusAsync(CancellationToken cancellationToken)
    {
        var status = await _context.FanficStatuses.FirstOrDefaultAsync(
            s => s.Name.ToLower() == "draft",
            cancellationToken);

        status ??= await _context.FanficStatuses.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);

        if (status == null)
        {
            throw new ExcelDataPortException("У базі даних не знайдено жодного статусу твору.");
        }

        return status;
    }

    private static string GetRequiredString(IXLRow row, int columnIndex, string fieldName)
    {
        var value = row.Cell(columnIndex).GetString().Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ExcelDataPortException($"У рядку {row.RowNumber()} відсутнє значення для поля \"{fieldName}\".");
        }

        return value;
    }

    private static string? GetOptionalString(IXLRow row, int columnIndex)
    {
        return NormalizeOptional(row.Cell(columnIndex).GetString());
    }

    private static int? GetOptionalInt(IXLRow row, int columnIndex)
    {
        var text = row.Cell(columnIndex).GetString().Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return int.TryParse(text, out var value) && value > 0 ? value : null;
    }

    private static bool IsRowEmpty(IXLRow row)
    {
        return row.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetString()));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string TrimToLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
