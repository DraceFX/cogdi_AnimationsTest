using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AdbExecutor
{
    private const int DefaultTimeoutMs = 15000;
    private readonly string _adbPath;

    public AdbExecutor(string adbPath)
    {
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            throw new ArgumentException("adb path is empty", nameof(adbPath));
        }

        _adbPath = adbPath;
    }

    public Task<string> ExecuteAsync(string arguments, CancellationToken token = default, int timeoutMs = DefaultTimeoutMs, bool combineStderr = false) => ExecuteProcessAsync(_adbPath, arguments, timeoutMs, token, combineStderr);

    public async Task<bool> StartServerAsync(CancellationToken token = default)
    {
        try
        {
            await ExecuteProcessAsync(_adbPath, "kill-server", 5000, token, false);
            await ExecuteProcessAsync(_adbPath, "start-server", DefaultTimeoutMs, token, false);

            var version = await ExecuteProcessAsync(_adbPath, "version", 5000, token, false);
            if (string.IsNullOrWhiteSpace(version) || !version.Contains("Android Debug Bridge"))
            {
                Debug.LogError($"ADB не отвечает. Вывод: '{version}'");
                return false;
            }

            Debug.Log($"ADB готов. {version.Split('\n')[0].Trim()}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка инициализации ADB: {e.GetType().Name}: {e.Message}");
            return false;
        }
    }

    private static async Task<string> ExecuteProcessAsync(string exe, string arguments, int timeoutMs, CancellationToken token, bool combineStderr)
    {
        return await Task.Run(async () =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var p = new Process { StartInfo = psi };
            p.Start();

            var stdoutTask = p.StandardOutput.ReadToEndAsync();
            var stderrTask = p.StandardError.ReadToEndAsync();

            var waitTask = Task.Run(() => p.WaitForExit());
            var completed = await Task.WhenAny(waitTask, Task.Delay(timeoutMs, token));

            if (completed != waitTask)
            {
                try
                {
                    p.Kill();
                }
                catch { }

                Debug.LogWarning($"Процесс '{exe} {arguments}' не завершился за {timeoutMs} мс.");
                return string.Empty;
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (!string.IsNullOrWhiteSpace(stderr) && !combineStderr)
            {
                Debug.LogWarning($"ADB stderr ({arguments}): {stderr.Trim()}");
            }

            return combineStderr && !string.IsNullOrWhiteSpace(stderr) ? stdout + "\n" + stderr : stdout;
        }, token);
    }
}