# OpenNari

OpenNari is an open-source Windows app for the Razer Nari Ultimate. The first milestone is a clean desktop interface and a reliable way to identify the headset's USB HID interface. HyperSense and Chroma controls will be enabled only when their commands have been confirmed on hardware.

## Build

Install the .NET 10 SDK on Windows, then run:

```powershell
dotnet build OpenNari.slnx
dotnet run --project src/OpenNari
```

The app does not replace an audio driver. Sound and microphone continue to use the normal Windows devices.

## Status

This is an early hardware research project. The connected Nari Ultimate has USB IDs `1532:051A` for the receiver and `1532:051B` for the headset when attached by cable. Device detection and settings controls are being developed in small, tested steps. The interface will distinguish verified controls from commands still under investigation.

## References

- [OpenRGB Nari Ultimate device report and USB capture](https://gitlab.com/CalcProgrammer1/OpenRGB/-/issues/2114)
- [Razer Nari pairing protocol research](https://github.com/juanjodarko/razer-nari-pairing)
- [Earlier Linux Nari driver research](https://github.com/felixZmn/razer-nari-driver)

OpenNari is an independent community project and is not affiliated with Razer.
