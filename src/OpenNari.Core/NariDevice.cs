using HidSharp;

namespace OpenNari.Core;

public sealed class NariDevice : IDisposable
{
    private const int RazerVendorId = 0x1532;
    private const int ReceiverProductId = 0x051A;
    private readonly HidStream stream;
    private readonly object streamLock = new();

    private NariDevice(HidDevice device, HidStream stream)
    {
        DevicePath = device.DevicePath;
        this.stream = stream;
    }

    public string DevicePath { get; }

    public static NariDevice? Connect()
    {
        // The receiver has three HID collections. Only collection 3 carries
        // the 64-byte feature reports used for headset settings.
        var devices = DeviceList.Local.GetHidDevices(RazerVendorId, ReceiverProductId);
        foreach (var device in devices)
        {
            if (device.GetMaxFeatureReportLength() != 64 ||
                !device.DevicePath.Contains("mi_05&col03", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (device.TryOpen(out var stream))
            {
                return new NariDevice(device, stream);
            }
        }

        return null;
    }

    public byte[] SetHaptics(bool enabled, int intensity)
    {
        if (intensity is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(intensity));
        }

        // Captured Synapse reports use 0x20 for HyperSense. The following two
        // bytes are the on/off flag and the intensity in percent.
        return SendReport(0x02, 0xF1, 0x06, 0x20, enabled ? (byte)1 : (byte)0, (byte)intensity);
    }

    public byte[] SetLightingEnabled(bool enabled)
    {
        // Captures of the static lighting switch differ only in the last byte.
        return SendReport(0x12, 0xF1, 0x03, 0x71, enabled ? (byte)0xFF : (byte)0);
    }

    public byte[] SetLightingColor(byte red, byte green, byte blue)
    {
        // The OpenRGB issue capture streams changing RGB triples with this
        // command. A single report may be enough for a steady color.
        return SendReport(0x12, 0xF1, 0x05, 0x72, red, green, blue);
    }

    public byte[] ReadFeatureReport()
    {
        var report = new byte[64];
        report[0] = 0xFF;
        lock (streamLock)
        {
            stream.GetFeature(report);
        }
        return report;
    }

    public BatteryStatus? ReadBatteryStatus()
    {
        // Nari receiver status query captured from Synapse. The response holds
        // charging state at byte 9 and firmware battery percentage at byte 14.
        // See https://github.com/indina853/NariMeter#how-it-works--reverse-engineering-the-protocol
        var request = new byte[64];
        request[0] = 0xFF;
        request[1] = 0x0A;
        request[3] = 0xFD;
        request[4] = 0x04;
        request[5] = 0x12;
        request[6] = 0xF1;
        request[7] = 0x02;
        request[8] = 0x05;

        var response = new byte[64];
        response[0] = 0xFF;
        lock (streamLock)
        {
            stream.SetFeature(request);
            // The receiver needs a short pause to prepare the requested report.
            Thread.Sleep(100);
            stream.GetFeature(response);
        }

        if (response[0] != 0xFF ||
            (response[1] == 0x01 && response[2] == 0x00) ||
            response[9] is not (0x03 or 0x05 or 0x06) ||
            response[14] > 100)
        {
            return null;
        }

        var chargeState = response[9] switch
        {
            0x05 => BatteryChargeState.Charging,
            0x06 => BatteryChargeState.FullyCharged,
            _ => BatteryChargeState.Discharging
        };

        var millivolts = (response[12] << 8) | response[13];
        if (millivolts is < 3000 or > 4300)
        {
            return null;
        }

        return new BatteryStatus(response[14], millivolts, chargeState);
    }

    private byte[] SendReport(params byte[] command)
    {
        var report = new byte[64];
        report[0] = 0xFF;
        report[1] = 0x0A;
        report[2] = 0x00;
        report[3] = 0xFF;
        report[4] = 0x04;
        command.CopyTo(report, 5);
        lock (streamLock)
        {
            stream.SetFeature(report);
        }
        return report;
    }

    public void Dispose()
    {
        lock (streamLock)
        {
            stream.Dispose();
        }
    }
}
