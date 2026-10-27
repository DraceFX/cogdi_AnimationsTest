public struct HeadsetStatus
{
    public int BatteryLevel;         // -1 = не удалось получить
    public long TotalStorageBytes;   // 0 = не удалось получить
    public long FreeStorageBytes;

    public bool HasBattery => BatteryLevel >= 0;
    public bool HasStorage => TotalStorageBytes > 0;
}