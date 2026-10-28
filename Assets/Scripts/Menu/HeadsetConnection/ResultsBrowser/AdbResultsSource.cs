using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AdbResultsSource : IResultsSource
{
    private readonly AdbExecutor _adb;
    private readonly string _serial;
    private readonly string _remoteAccountsPath;

    public AdbResultsSource(AdbExecutor adb, string deviceSerial, string remoteAccountsPath)
    {
        _adb = adb;
        _serial = deviceSerial;
        _remoteAccountsPath = remoteAccountsPath.TrimEnd('/');
    }

    public async Task<IReadOnlyList<string>> GetUsersAsync(CancellationToken ct)
    {
        var output = await _adb.ExecuteAsync($"-s {_serial} shell ls -1 -p \"{_remoteAccountsPath}\"", ct, timeoutMs: 10000);

        return ParseEntries(output, mustBeDirectory: true);
    }

    public async Task<IReadOnlyList<string>> GetScenariosAsync(string user, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user)) return Array.Empty<string>();

        var path = $"{_remoteAccountsPath}/{user}";
        var output = await _adb.ExecuteAsync($"-s {_serial} shell ls -1 -p \"{path}\"", ct, timeoutMs: 10000);

        // Сценарии — это *.csv-файлы (не папки)
        return ParseEntries(output, mustBeDirectory: false).Where(n => n.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)).Select(n => n.Substring(0, n.Length - 4)).ToList();
    }

    public async Task<string> ReadCsvAsync(string user, string scenario, CancellationToken ct)
    {
        var path = $"{_remoteAccountsPath}/{user}/{scenario}.csv";
        var output = await _adb.ExecuteAsync($"-s {_serial} shell cat \"{path}\"", ct, timeoutMs: 15000);

        return string.IsNullOrEmpty(output) ? null : output;
    }

    private static IReadOnlyList<string> ParseEntries(string output, bool mustBeDirectory)
    {
        if (string.IsNullOrWhiteSpace(output)) return Array.Empty<string>();

        var result = new List<string>();
        foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            // ls -p добавляет "/" к папкам
            bool isDir = line.EndsWith("/");
            if (mustBeDirectory && !isDir) continue;
            if (!mustBeDirectory && isDir) continue;

            var name = isDir ? line.Substring(0, line.Length - 1) : line;
            if (name.Length > 0)
            {
                result.Add(name);
            }
        }
        return result;
    }
}
