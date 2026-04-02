using ClosedXML.Excel;
using LibraryDomain.Model;
using Microsoft.EntityFrameworkCore;

namespace LibraryInfrastructure.Services;

public class FanficExcelExportService : IExportService<Fanfic>
{
    private readonly DbLibraryContext _context;

    public FanficExcelExportService(DbLibraryContext context)
    {
        _context = context;
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanWrite)
        {
            throw new ArgumentException("Потік для експорту Excel недоступний для запису.", nameof(stream));
        }

        var works = await _context.Fanfics
            .AsNoTracking()
            .Include(f => f.User)
            .Include(f => f.ContentRating)
            .Include(f => f.FanficTags)
                .ThenInclude(ft => ft.Tag)
            .Include(f => f.Chapters)
            .Include(f => f.Comments)
            .Include(f => f.Likes)
            .Include(f => f.Bookmarks)
            .OrderBy(f => f.Title)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        WriteWorksWorksheet(workbook.Worksheets.Add(FanficExcelSchema.WorksWorksheetName), works);
        WriteReportWorksheet(workbook.Worksheets.Add(FanficExcelSchema.ReportWorksheetName), works);
        workbook.SaveAs(stream);
    }

    private static void WriteWorksWorksheet(IXLWorksheet worksheet, IReadOnlyCollection<Fanfic> works)
    {
        WriteHeader(worksheet, FanficExcelSchema.ImportHeaders);

        var rowIndex = 2;
        foreach (var work in works)
        {
            var tagString = string.Join("; ", work.FanficTags.Select(ft => ft.Tag.Name).OrderBy(x => x));
            var chapters = work.Chapters.OrderBy(c => c.ChapterNumber).ToList();

            if (chapters.Count == 0)
            {
                WriteWorkRow(worksheet, rowIndex++, work, tagString, null);
                continue;
            }

            foreach (var chapter in chapters)
            {
                WriteWorkRow(worksheet, rowIndex++, work, tagString, chapter);
            }
        }

        worksheet.Columns().AdjustToContents();
    }

    private static void WriteReportWorksheet(IXLWorksheet worksheet, IReadOnlyCollection<Fanfic> works)
    {
        WriteHeader(worksheet, FanficExcelSchema.ReportHeaders);

        var rowIndex = 2;
        foreach (var work in works.OrderByDescending(w => w.UpdatedAt))
        {
            worksheet.Cell(rowIndex, 1).Value = work.Title;
            worksheet.Cell(rowIndex, 2).Value = work.User.Username;
            worksheet.Cell(rowIndex, 3).Value = work.ContentRating?.Name ?? "Не вказано";
            worksheet.Cell(rowIndex, 4).Value = work.FanficTags.Count;
            worksheet.Cell(rowIndex, 5).Value = work.Chapters.Count;
            worksheet.Cell(rowIndex, 6).Value = work.Comments.Count(c => !c.IsDeleted && c.ChapterId == null);
            worksheet.Cell(rowIndex, 7).Value = work.Likes.Count;
            worksheet.Cell(rowIndex, 8).Value = work.Bookmarks.Count;
            worksheet.Cell(rowIndex, 9).Value = work.UpdatedAt.ToString("dd.MM.yyyy HH:mm");
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();
    }

    private static void WriteWorkRow(IXLWorksheet worksheet, int rowIndex, Fanfic work, string tagString, Chapter? chapter)
    {
        worksheet.Cell(rowIndex, FanficExcelSchema.TitleColumn).Value = work.Title;
        worksheet.Cell(rowIndex, FanficExcelSchema.DescriptionColumn).Value = work.Description ?? string.Empty;
        worksheet.Cell(rowIndex, FanficExcelSchema.RatingColumn).Value = work.ContentRating?.Name ?? string.Empty;
        worksheet.Cell(rowIndex, FanficExcelSchema.TagsColumn).Value = tagString;
        worksheet.Cell(rowIndex, FanficExcelSchema.ChapterTitleColumn).Value = chapter?.Title ?? string.Empty;
        worksheet.Cell(rowIndex, FanficExcelSchema.ChapterNumberColumn).Value = chapter?.ChapterNumber ?? 0;
        worksheet.Cell(rowIndex, FanficExcelSchema.ChapterContentColumn).Value = chapter?.Content ?? string.Empty;
    }

    private static void WriteHeader(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
    }
}
