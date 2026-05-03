using LibraryDomain.Model;

namespace LibraryInfrastructure.Services;

public interface IImportService<TEntity>
    where TEntity : Entity
{
    Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken);
}
