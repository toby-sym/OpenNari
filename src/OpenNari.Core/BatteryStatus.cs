namespace OpenNari.Core;

public enum BatteryChargeState
{
    Discharging,
    Charging,
    FullyCharged
}

public readonly record struct BatteryStatus(int FirmwarePercent, int Millivolts, BatteryChargeState ChargeState);
