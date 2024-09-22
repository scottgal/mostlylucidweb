namespace Mostlylucid.Services.Interfaces;

public interface IBlogPopulator
{
    Task Populate(CancellationToken cancellationToken);


}