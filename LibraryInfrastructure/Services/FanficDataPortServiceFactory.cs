using LibraryDomain.Model;

namespace LibraryInfrastructure.Services;

public class FanficDataPortServiceFactory : IDataPortServiceFactory<Fanfic>
{
    private readonly DbLibraryContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FanficDataPortServiceFactory(DbLibraryContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public IImportService<Fanfic> GetImportService(string contentType)
    {
        if (contentType == ExcelContentTypes.Xlsx)
        {
            return new FanficExcelImportService(_context, _httpContextAccessor);
        }

        throw new NotSupportedException($"Імпорт творів для типу {contentType} не підтримується.");
    }

    public IExportService<Fanfic> GetExportService(string contentType)
    {
        if (contentType == ExcelContentTypes.Xlsx)
        {
            return new FanficExcelExportService(_context);
        }

        throw new NotSupportedException($"Експорт творів для типу {contentType} не підтримується.");
    }
}
