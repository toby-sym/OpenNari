using HidSharp;
using OpenNari.Core;

if (args.Length > 0)
{
    using var headset = NariDevice.Connect();
    if (headset is null)
    {
        Console.Error.WriteLine("The Nari Ultimate receiver settings interface is unavailable.");
        return 1;
    }

    byte[] report;
    switch (args[0].ToLowerInvariant())
    {
        case "read":
            report = headset.ReadFeatureReport();
            break;
        case "haptics" when args.Length == 3 &&
                            (args[1] is "on" or "off") &&
                            int.TryParse(args[2], out var intensity):
            report = headset.SetHaptics(args[1] == "on", intensity);
            break;
        case "light" when args.Length == 2 && args[1] is "on" or "off":
            report = headset.SetLightingEnabled(args[1] == "on");
            break;
        case "color" when args.Length == 2 && args[1].Length == 6:
            byte[] color;
            try { color = Convert.FromHexString(args[1]); }
            catch (FormatException)
            {
                Console.Error.WriteLine("Color must be six hexadecimal digits, such as FF0000.");
                return 2;
            }
            report = headset.SetLightingColor(color[0], color[1], color[2]);
            break;
        default:
            Console.Error.WriteLine("Use: read | haptics on|off 0-100 | light on|off | color RRGGBB");
            return 2;
    }

    Console.WriteLine(Convert.ToHexString(report.AsSpan(0, 16)));
    return 0;
}

foreach (var productId in new[] { 0x051A, 0x051B })
{
    Console.WriteLine($"Razer device 1532:{productId:X4}");
    foreach (var device in DeviceList.Local.GetHidDevices(0x1532, productId))
    {
        Console.WriteLine($"  {device.DevicePath}");
        Console.WriteLine($"  Feature report: {device.GetMaxFeatureReportLength()} bytes; input: {device.GetMaxInputReportLength()} bytes; output: {device.GetMaxOutputReportLength()} bytes");
        Console.WriteLine($"  Product: {device.GetProductName()}");
        Console.WriteLine($"  Can open: {device.TryOpen(out var stream)}");
        stream?.Dispose();
    }
}

return 0;
