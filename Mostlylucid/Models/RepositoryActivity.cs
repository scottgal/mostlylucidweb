namespace Mostlylucid.Models;

public record RepositoryActivity(string Name, string Description, DateTimeOffset PushedAt, bool HasRelease);

public record RepositoryActivityViewModel(IReadOnlyList<RepositoryActivity> Repositories, bool IsStale = false);
