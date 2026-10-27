// AdbDataService.cs
using System.Threading;
using System.Threading.Tasks;

public class AdbDataService
{
    private readonly AdbExecutor _adb;

    public AdbDataService(AdbExecutor adb) => _adb = adb;

    public async Task<HeadsetStatus> GetStatusAsync(HeadsetDevice device, CancellationToken token)
    {
        var status = new HeadsetStatus();

        var batteryOutput = await _adb.ExecuteAsync($"-s {device.Serial} shell dumpsys battery", token);
        status.BatteryLevel = AdbOutputParsers.ParseBatteryLevel(batteryOutput);

        var storage = await GetStorageAsync(device.Serial, token);
        status.TotalStorageBytes = storage.total;
        status.FreeStorageBytes = storage.free;

        return status;
    }

    private async Task<(long total, long free)> GetStorageAsync(string serial, CancellationToken token)
    {
        //df -k /data
        var df = await _adb.ExecuteAsync($"-s {serial} shell df -k /data", token);
        var result = AdbOutputParsers.ParseDf(df);
        if (result.total > 0) return result;

        //stat -f /data
        var stat = await _adb.ExecuteAsync($"-s {serial} shell stat -f /data", token);
        result = AdbOutputParsers.ParseStatFs(stat);
        if (result.total > 0) return result;

        //df -k /storage/emulated/0
        var userDf = await _adb.ExecuteAsync($"-s {serial} shell df -k /storage/emulated/0", token);
        return AdbOutputParsers.ParseDf(userDf);
    }
}