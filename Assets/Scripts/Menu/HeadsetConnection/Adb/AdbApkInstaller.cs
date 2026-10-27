using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AdbApkInstaller
{
    private readonly AdbExecutor _adb;

    // 5 минут на установку — с запасом для больших APK по Wi-Fi
    private const int InstallTimeoutMs = 5 * 60 * 1000;

    public AdbApkInstaller(AdbExecutor adb) => _adb = adb;

    public async Task<bool> InstallAsync(string apkPath, string serial, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(apkPath) || !System.IO.File.Exists(apkPath))
        {
            Debug.LogError($"APK не найден: {apkPath}");
            return false;
        }

        // -r = переустановить (сохраняет данные), -d = разрешить downgrade
        var args = $"-s {serial} install -r \"{apkPath}\"";
        Debug.Log($"Запуск: adb {args}");

        var output = await _adb.ExecuteAsync(args, token, timeoutMs: InstallTimeoutMs, combineStderr: true);
        Debug.Log($"adb install output:\n{output}");

        if (output.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0) return true;

        // Извлекаем строку с Failure [...], если есть — для читаемой ошибки
        var failureMsg = ExtractFailureMessage(output);
        Debug.LogError($"Установка не удалась: {failureMsg}");
        return false;
    }

    public async Task<bool> UninstallAsync(string packageName, string serial, CancellationToken token)
    {
        var output = await _adb.ExecuteAsync($"-s {serial} uninstall {packageName}", token, timeoutMs: 30000, combineStderr: true);
        return output.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ExtractFailureMessage(string output)
    {
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.IndexOf("Failure [", StringComparison.Ordinal) >= 0) return line.Trim();
            if (line.StartsWith("adb: failed", StringComparison.OrdinalIgnoreCase)) return line.Trim();
        }
        return output.Trim();
    }
}