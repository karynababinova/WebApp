namespace LibraryInfrastructure.Services;

public interface IFanficExcelTemplateService
{
    Task WriteTemplateAsync(Stream stream, CancellationToken cancellationToken);
}
