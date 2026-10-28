using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class LocalResultsSource : IResultsSource
{
    private readonly ResultBrowser _browser;

    public LocalResultsSource(string rootPath)
    {
        _browser = new ResultBrowser(rootPath);
    }

    public Task<IReadOnlyList<string>> GetUsersAsync(CancellationToken ct) => Task.Run(() => _browser.GetUsers(), ct);
    public Task<IReadOnlyList<string>> GetScenariosAsync(string user, CancellationToken ct) => Task.Run(() => _browser.GetScenarios(user), ct);
    public Task<string> ReadCsvAsync(string user, string scenario, CancellationToken ct) => Task.Run(() => _browser.ReadCsv(user, scenario), ct);
}
