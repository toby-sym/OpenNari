# OpenNari

OpenNari is an open-source Windows app for the Razer Nari Ultimate. It shows headset battery level and charging status, controls HyperSense strength from 0 to 100%, switches the earcup lighting, and applies a custom static color through the headset's USB receiver. The commands were tested on a connected Nari Ultimate.

Battery status refreshes every 15 seconds while the receiver is connected. The approximate percentage uses receiver voltage while discharging and the headset's reported level while charging. Changes are limited to 5% steps to avoid sudden firmware jumps. The charge state is reported directly by the receiver; unavailable readings are shown as such. The status report format and initial voltage range are based on [NariMeter's receiver protocol research](https://github.com/indina853/NariMeter#how-it-works--reverse-engineering-the-protocol).

## Build

Install the .NET 10 SDK on Windows, then run:

```powershell
dotnet build OpenNari.slnx
dotnet run --project src/OpenNari
```

The app does not replace an audio driver. Sound and microphone continue to use the normal Windows devices.

## Portable Windows app

The Actions workflow builds a single self-contained Windows x64 executable

## References

- [OpenRGB Nari Ultimate device report and USB capture](https://gitlab.com/CalcProgrammer1/OpenRGB/-/issues/2114)
- [Razer Nari pairing protocol research](https://github.com/juanjodarko/razer-nari-pairing)
- [Earlier Linux Nari driver research](https://github.com/felixZmn/razer-nari-driver)
- [HidSharp](https://github.com/IntergatedCircuits/HidSharp) for Windows HID access

OpenNari is an independent project and is not affiliated with Razer.
