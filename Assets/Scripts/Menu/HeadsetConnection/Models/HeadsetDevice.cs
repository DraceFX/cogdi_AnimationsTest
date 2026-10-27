public class HeadsetDevice
{
    public string Serial { get; set; }
    public string Model { get; set; }         // внутреннее имя, e.g. "A9210"
    public string DisplayModel { get; set; }  // e.g. "Pico 4 Ultra"

    public string HardwareSerial { get; set; }

    public bool IsWireless => !string.IsNullOrEmpty(Serial) && Serial.Contains(":");

    public string EffectiveSerial => string.IsNullOrEmpty(HardwareSerial) ? Serial : HardwareSerial;
}
