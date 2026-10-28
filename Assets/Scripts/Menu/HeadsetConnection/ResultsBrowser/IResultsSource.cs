using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IResultsSource
{
    public Task<IReadOnlyList<string>> GetUsersAsync(CancellationToken ct);
    public Task<IReadOnlyList<string>> GetScenariosAsync(string user, CancellationToken ct);
    public Task<string> ReadCsvAsync(string user, string scenario, CancellationToken ct);
}