using System;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

public class AdbDeviceService
{
    private readonly AdbExecutor _adb;

    public AdbDeviceService(AdbExecutor adb) => _adb = adb;

    public async Task<HeadsetDevice> FindHeadsetAsync(CancellationToken token)
    {
        var output = await _adb.ExecuteAsync("devices -l", token);
        var devices = AdbOutputParsers.ParseDevicesList(output);

        if (devices.Count == 0) return null;
        if (devices.Count == 1) return devices[0];

        foreach (var d in devices)
        {
            token.ThrowIfCancellationRequested();
            if (await IsHeadsetAsync(d.Serial, token)) return d;
        }
        return null;
    }

    public async Task EnableTcpipOnceAsync(string serial, int port, CancellationToken token)
    {
        var result = await _adb.ExecuteAsync($"-s {serial} tcpip {port}", token);
        Debug.Log($"adb tcpip {port} -> {result.Trim()}");
        await Task.Delay(1000, token);
        await _adb.ExecuteAsync("reconnect offline", token);
    }

    public Task<string> ConnectWirelessAsync(string ip, int port, CancellationToken token) => _adb.ExecuteAsync($"connect {ip}:{port}", token);

    private async Task<bool> IsHeadsetAsync(string serial, CancellationToken token)
    {
        try
        {
            var manufacturer = (await _adb.ExecuteAsync($"-s {serial} shell getprop ro.product.manufacturer", token)).Trim();
            var brand = (await _adb.ExecuteAsync($"-s {serial} shell getprop ro.product.brand", token)).Trim();
            var model = (await _adb.ExecuteAsync($"-s {serial} shell getprop ro.product.model", token)).Trim();

            var combined = $"{manufacturer} {brand} {model}";
            return combined.IndexOf("Pico", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("Meta", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("Oculus", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось получить свойства {serial}: {e.Message}");
            return false;
        }
    }

    public async Task<string> GetHardwareSerialAsync(string serial, CancellationToken token)
    {
        try
        {
            var result = await _adb.ExecuteAsync($"-s {serial} shell getprop ro.serialno", token);
            var value = result.Trim();

            // Пустая строка или "unknown" — значит, производитель не выставил свойство
            if (string.IsNullOrEmpty(value) || value.Equals("unknown", System.StringComparison.OrdinalIgnoreCase)) return null;

            return value;
        }
        catch (System.OperationCanceledException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"Не удалось получить ro.serialno для {serial}: {e.Message}");
            return null;
        }
    }
}