namespace OpenNari.Core;

public readonly record struct BatteryEstimate(int Percent, BatteryChargeState ChargeState);

public sealed class BatteryLevelEstimator
{
    // Observed Nari voltage range from NariMeter's receiver research. Voltage
    // is useful while discharging; the charging circuit raises it on cable.
    private const int EmptyMillivolts = 3296;
    private const int FullMillivolts = 4128;
    private static readonly TimeSpan StateChangeHold = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StepInterval = TimeSpan.FromSeconds(30);

    private int? displayedPercent;
    private BatteryChargeState previousChargeState;
    private DateTimeOffset holdUntil;
    private DateTimeOffset lastStepAt;
    private int pendingDirection;
    private int pendingCount;

    public BatteryEstimate Update(BatteryStatus reading, DateTimeOffset now)
    {
        if (reading.ChargeState == BatteryChargeState.FullyCharged)
        {
            displayedPercent = 100;
            previousChargeState = BatteryChargeState.FullyCharged;
            lastStepAt = now;
            pendingCount = 0;
            return new BatteryEstimate(100, BatteryChargeState.FullyCharged);
        }

        var voltagePercent = VoltagePercent(reading.Millivolts);
        var target = reading.ChargeState == BatteryChargeState.Charging
            ? reading.FirmwarePercent
            : Math.Abs(voltagePercent - reading.FirmwarePercent) > 40
                ? reading.FirmwarePercent
                : voltagePercent;
        target = Math.Clamp((int)Math.Round(target / 5.0) * 5, 0, 100);

        if (displayedPercent is null)
        {
            displayedPercent = target;
            previousChargeState = reading.ChargeState;
            lastStepAt = now;
            return new BatteryEstimate(target, reading.ChargeState);
        }

        if (reading.ChargeState != previousChargeState)
        {
            previousChargeState = reading.ChargeState;
            holdUntil = now + StateChangeHold;
            pendingCount = 0;
        }

        var current = displayedPercent.Value;
        if (reading.ChargeState == BatteryChargeState.Charging)
        {
            target = Math.Max(target, current);
        }
        else if (target > current && target - current < 10)
        {
            // Small voltage recovery under changing load is not charge gained.
            target = current;
        }

        var direction = Math.Sign(target - current);
        if (direction == 0 || now < holdUntil)
        {
            pendingCount = 0;
            return new BatteryEstimate(current, reading.ChargeState);
        }

        if (direction != pendingDirection)
        {
            pendingDirection = direction;
            pendingCount = 1;
        }
        else
        {
            pendingCount++;
        }

        if (pendingCount >= 2 && now - lastStepAt >= StepInterval)
        {
            displayedPercent = Math.Clamp(current + direction * 5, 0, 100);
            lastStepAt = now;
            pendingCount = 0;
        }

        return new BatteryEstimate(displayedPercent.Value, reading.ChargeState);
    }

    private static int VoltagePercent(int millivolts) =>
        (int)Math.Round(Math.Clamp((millivolts - EmptyMillivolts) * 100.0 /
            (FullMillivolts - EmptyMillivolts), 0, 100));
}
