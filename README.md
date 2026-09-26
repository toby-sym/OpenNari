# OpenNari

OpenNari is an open-source Windows app for the Razer Nari Ultimate. It controls HyperSense strength from 0 to 100%, switches the earcup lighting, and applies a custom static color through the headset's USB receiver. The commands were tested on a connected Nari Ultimate.

## Build

Install the .NET 10 SDK on Windows, then run:

```powershell
dotnet build OpenNari.slnx
dotnet run --project src/OpenNari
```

The app does not replace an audio driver. Sound and microphone continue to use the normal Windows devices.

## Portable Windows app

The Actions workflow builds a single self-contained Windows x64 executable for pull requests, pushes to `main`, and manual runs. Open **Actions**, choose **Build portable Windows app**, then select **Run workflow**:

- Choose `dev` for a development build. It has no version stamp.
- Choose `release` and enter a version in `X.Y.Z` form, such as `1.2.3`. The app shows that version, and the workflow creates the `v1.2.3` tag and GitHub release with the executable only.

## References

- [OpenRGB Nari Ultimate device report and USB capture](https://gitlab.com/CalcProgrammer1/OpenRGB/-/issues/2114)
- [Razer Nari pairing protocol research](https://github.com/juanjodarko/razer-nari-pairing)
- [Earlier Linux Nari driver research](https://github.com/felixZmn/razer-nari-driver)
- [HidSharp](https://github.com/IntergatedCircuits/HidSharp) for Windows HID access

OpenNari is an independent project and is not affiliated with Razer.
