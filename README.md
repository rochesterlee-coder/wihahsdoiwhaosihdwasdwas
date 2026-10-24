# InstaOrg

A Windows desktop app that copies files from a folder into `Organized Files`, grouped by type and listed alphabetically by filename in each group.

## Safety notes
- InstaOrg copies files; it does not move or delete originals.
- You choose the source and destination folders.
- Duplicate filenames are renamed instead of overwritten.
- Start by testing on a small sample folder.
- Some protected or inaccessible folders may not be readable.

## Build locally
Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet publish InstaOrg.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The app will be in the `publish` folder.

## Build with GitHub
Push this project to a GitHub repository. The workflow in `.github/workflows/build.yml` runs on pushes to `main` or manually from **Actions → Build InstaOrg → Run workflow**. Download the `InstaOrg-Windows-x64` artifact from the completed workflow.
