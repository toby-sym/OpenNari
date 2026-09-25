using HidSharp;

namespace OpenNari.Core;

public sealed class NariDevice : IDisposable
{
    private const int RazerVendorId = 0x1532;
    private const int ReceiverProductId = 0x051A;
    private readonly HidStream stream;

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
        if (intensity is < 20 or > 100)
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

    public byte[] ReadFeatureReport()
    {
        var report = new byte[64];
        report[0] = 0xFF;
        stream.GetFeature(report);
        return report;
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
        stream.SetFeature(report);
        return report;
    }

    public void Dispose() => stream.Dispose();
}
