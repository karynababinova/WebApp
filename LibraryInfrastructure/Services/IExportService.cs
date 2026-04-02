using LibraryDomain.Model;

namespace LibraryInfrastructure.Services;

public interface IExportService<TEntity>
    where TEntity : Entity
{
    Task WriteToAsync(Stream stream, CancellationToken cancellationToken);
}
