# Contributing to OpenNari

## Branch names

- `story/` for a user story that spans a few related changes.
- `feature/` for one new capability.
- `defect/` for a bug fix.

Use a short, plain English description after the slash, such as `feature/lighting-color-picker`.

## Changes and pull requests

Keep each pull request focused on a change that can be reviewed and tested. Open pull requests against `main` and describe the user-visible change and how you checked it. Use plain English commit messages and pull request titles.

When changing headset commands, include the capture or hardware evidence in `docs/protocol.md`. Do not describe a command as verified unless it has been checked against a capture or a connected headset.

## Build on Windows

Install the .NET 10 SDK, then run:

```powershell
dotnet build OpenNari.slnx --configuration Release
dotnet run --project src/OpenNari
```

The GitHub Actions workflow builds pull requests and pushes to `main`. To make a versioned portable release, open **Actions**, choose **Build portable Windows app**, select **Run workflow**, enter the app version, and run it. The run stores the portable executable as an artifact and creates a GitHub release with the executable and its SHA-256 checksum.
