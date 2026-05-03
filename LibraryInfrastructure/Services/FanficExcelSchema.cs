namespace LibraryInfrastructure.Services;

public static class FanficExcelSchema
{
    public const string WorksWorksheetName = "Твори";
    public const string ReportWorksheetName = "Звіт";

    public const int TitleColumn = 1;
    public const int DescriptionColumn = 2;
    public const int RatingColumn = 3;
    public const int TagsColumn = 4;
    public const int ChapterTitleColumn = 5;
    public const int ChapterNumberColumn = 6;
    public const int ChapterContentColumn = 7;

    public static readonly IReadOnlyList<string> ImportHeaders =
    [
        "Назва твору",
        "Опис",
        "Рейтинг",
        "Теги",
        "Назва розділу",
        "Номер розділу",
        "Текст розділу"
    ];

    public static readonly IReadOnlyList<string> ReportHeaders =
    [
        "Назва твору",
        "Автор",
        "Рейтинг",
        "Кількість тегів",
        "Кількість розділів",
        "Коментарі до твору",
        "Лайки",
        "Закладки",
        "Оновлено"
    ];
}
