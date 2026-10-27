using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class HeadsetMonitor : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private HeadsetMonitorUI ui;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 30f;

    [Header("ADB")]
    [Tooltip("Если пусто — StreamingAssets/platform-tools/")]
    [SerializeField] private string adbExecutablePathOverride = "";

    [Header("Wireless Connection")]
    [SerializeField] private int adbPort = 5555;
    [SerializeField] private bool autoScanNetwork = true;
    [SerializeField] private int scanTimeoutMs = 300;
    [SerializeField] private int scanMaxConcurrency = 64;
    [SerializeField] private bool autoEnableTcpip = true;
    [SerializeField] private List<string> extraHeadsetIPs = new List<string>();

    private AdbExecutor _adb;
    private AdbDeviceService _deviceService;
    private AdbDataService _dataService;
    private AdbApkInstaller _apkInstaller;
    private NetworkScanner _scanner;
    private CancellationTokenSource _cts;
    private readonly HashSet<string> _tcpipEnabledSerials = new HashSet<string>();

    public HeadsetDevice ConnectedDevice { get; private set; }

    private async void Start()
    {
        _cts = new CancellationTokenSource();

        var path = AdbPathResolver.Resolve(adbExecutablePathOverride);
        Debug.Log($"Используется adb: {path}");

        if (!File.Exists(path) && !IsExecutableInPath(path))
        {
            Debug.LogError($"adb не найден по пути: {path}");
            return;
        }

        _adb = new AdbExecutor(path);
        if (!await _adb.StartServerAsync(_cts.Token))
        {
            Debug.LogError("Инициализация ADB не удалась — мониторинг не запущен.");
            return;
        }

        _deviceService = new AdbDeviceService(_adb);
        _dataService = new AdbDataService(_adb);
        _scanner = new NetworkScanner();
        _apkInstaller = new AdbApkInstaller(_adb);

        if (ui != null)
        {
            ui.InstallApkClicked += HandleInstallApkClicked;
            ui.SetInstallStatus(string.Empty);
            ui.UpdateModel(null);
            ui.UpdateSerial(null);
        }
        _ = MonitorLoopAsync(_cts.Token);
    }

    private void OnDestroy()
    {
        try
        {
            _cts?.Cancel();
        }
        catch { }
        _cts?.Dispose();

        if (ui != null)
        {
            ui.InstallApkClicked -= HandleInstallApkClicked;
        }
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                ConnectedDevice = await ConnectToHeadsetAsync(token);

                if (ConnectedDevice != null)
                {
                    ui?.UpdateModel(ConnectedDevice.DisplayModel);
                    ui?.UpdateSerial(ConnectedDevice.EffectiveSerial);
                    var status = await _dataService.GetStatusAsync(ConnectedDevice, token);

                    await Task.Yield(); // главный поток Unity
                    ui?.UpdateStatus(status);
                }
                else
                {
                    ui?.UpdateModel(null);
                    ui?.UpdateSerial(null);
                    Debug.Log("Headset not connected");
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"Monitor loop error: {e.GetType().Name}: {e.Message}");
                ConnectedDevice = null;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(updateInterval), token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task<HeadsetDevice> ConnectToHeadsetAsync(CancellationToken token)
    {
        var device = await _deviceService.FindHeadsetAsync(token);
        if (device != null)
        {
            if (!device.IsWireless && autoEnableTcpip && _tcpipEnabledSerials.Add(device.Serial))
            {
                try
                {
                    await _deviceService.EnableTcpipOnceAsync(device.Serial, adbPort, token);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"adb tcpip: {e.Message}");
                    _tcpipEnabledSerials.Remove(device.Serial);
                }
            }

            if (device != null && device.IsWireless && string.IsNullOrEmpty(device.HardwareSerial))
            {
                device.HardwareSerial = await _deviceService.GetHardwareSerialAsync(device.Serial, token);
            }
            return device;
        }

        if (!autoScanNetwork) return null;

        var foundIps = await _scanner.ScanForPortAsync(adbPort, scanTimeoutMs, scanMaxConcurrency, token);

        foreach (var extra in extraHeadsetIPs)
        {
            if (!string.IsNullOrWhiteSpace(extra) && !foundIps.Contains(extra))
            {
                foundIps.Add(extra);
            }
        }

        if (foundIps.Count == 0)
        {
            Debug.Log("Шлемы в локальной сети не найдены.");
            return null;
        }

        foreach (var ip in foundIps)
        {
            token.ThrowIfCancellationRequested();
            var result = await _deviceService.ConnectWirelessAsync(ip, adbPort, token);
            Debug.Log($"adb connect {ip}:{adbPort} -> {result.Trim()}");
        }

        return await _deviceService.FindHeadsetAsync(token);
    }

    private static bool IsExecutableInPath(string path)
    {
        //Если нет слеша — считаем, что ищем в PATH
        return path.IndexOf(Path.DirectorySeparatorChar) < 0 && path.IndexOf(Path.AltDirectorySeparatorChar) < 0;
    }

    private async void HandleInstallApkClicked()
    {
        if (ConnectedDevice == null)
        {
            ui?.SetInstallStatus("Шлем не подключён", Color.red);
            return;
        }

        var path = WindowsFilePicker.PickApk();
        if (string.IsNullOrEmpty(path)) return; // пользователь отменил диалог

        var fileName = System.IO.Path.GetFileName(path);

        ui?.SetInstallButtonInteractable(false);
        ui?.SetInstallStatus($"Установка: {fileName}...", new Color(1f, 0.8f, 0.2f));

        try
        {
            var ok = await _apkInstaller.InstallAsync(path, ConnectedDevice.Serial, _cts.Token);

            if (ok)
            {
                ui?.SetInstallStatus($"Установлено: {fileName}", new Color(0.3f, 0.9f, 0.3f));
            }
            else
            {
                ui?.SetInstallStatus("Ошибка установки (см. Console)", Color.red);
            }
        }
        catch (OperationCanceledException)
        {
            //Приложение закрывается
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка установки APK: {e.GetType().Name}: {e.Message}");
            ui?.SetInstallStatus($"Ошибка: {e.Message}", Color.red);
        }
        finally
        {
            ui?.SetInstallButtonInteractable(true);
        }
    }
}