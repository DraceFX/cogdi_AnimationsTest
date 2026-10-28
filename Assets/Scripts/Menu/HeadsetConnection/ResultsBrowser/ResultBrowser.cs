using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ResultBrowser
{
    private readonly string _rootPath;

    public ResultBrowser(string rootPath)
    {
        _rootPath = rootPath;
    }

    public string RootPath => _rootPath;

    public IReadOnlyList<string> GetUsers()
    {
        if (!Directory.Exists(_rootPath))
        {
            return Array.Empty<string>();
        }

        try
        {
            return Directory.GetDirectories(_rootPath).Select(Path.GetFileName).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (System.Exception)
        {
            return Array.Empty<string>();
        }
    }

    public IReadOnlyList<string> GetScenarios(string user)
    {
        if (string.IsNullOrEmpty(user)) return Array.Empty<string>();

        var userPath = Path.Combine(_rootPath, user);
        if (!Directory.Exists(userPath)) return Array.Empty<string>();

        try
        {
            return Directory.GetFiles(userPath, "*.csv").Select(Path.GetFileNameWithoutExtension).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (System.Exception)
        {
            return Array.Empty<string>();
        }
    }

    public string ReadCsv(string user, string scenario)
    {
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(scenario)) return null;

        var path = Path.Combine(_rootPath, user, scenario + ".csv");
        if (!File.Exists(path)) return null;

        try
        {
            return File.ReadAllText(path);
        }
        catch (System.Exception)
        {
            return null;
        }
    }
}
