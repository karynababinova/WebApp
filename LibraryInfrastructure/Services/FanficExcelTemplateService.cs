using ClosedXML.Excel;

namespace LibraryInfrastructure.Services;

public class FanficExcelTemplateService : IFanficExcelTemplateService
{
    public Task WriteTemplateAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanWrite)
        {
            throw new ArgumentException("Потік для шаблону Excel недоступний для запису.", nameof(stream));
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(FanficExcelSchema.WorksWorksheetName);

        for (var i = 0; i < FanficExcelSchema.ImportHeaders.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = FanficExcelSchema.ImportHeaders[i];
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Value = "Хроніки зоряної бібліотеки";
        worksheet.Cell(2, 2).Value = "Космічний твір про екіпаж, що шукає загублений архів.";
        worksheet.Cell(2, 3).Value = "Teen";
        worksheet.Cell(2, 4).Value = "science fiction; adventure";
        worksheet.Cell(2, 5).Value = "Пробудження";
        worksheet.Cell(2, 6).Value = 1;
        worksheet.Cell(2, 7).Value = "Перший розділ твору.";

        worksheet.Cell(3, 1).Value = "Хроніки зоряної бібліотеки";
        worksheet.Cell(3, 2).Value = "Космічний твір про екіпаж, що шукає загублений архів.";
        worksheet.Cell(3, 3).Value = "Teen";
        worksheet.Cell(3, 4).Value = "science fiction; adventure";
        worksheet.Cell(3, 5).Value = "Сигнал з темряви";
        worksheet.Cell(3, 6).Value = 2;
        worksheet.Cell(3, 7).Value = "Другий розділ твору.";

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(stream);

        return Task.CompletedTask;
    }
}
