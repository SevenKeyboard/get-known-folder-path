# GetKnownFolderPath

A small C# wrapper for Windows `SHGetKnownFolderPath`. Version 2.0.0 targets
.NET 10 (`net10.0-windows`), Windows 10+, and NativeAOT-compatible consumers.

Reference `src/SevenKeyboard.GetKnownFolderPath/SevenKeyboard.GetKnownFolderPath.csproj`:

```csharp
using SevenKeyboard;

var localAppDataLow = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
if (GetKnownFolderPath.TryGet(localAppDataLow, out string path))
    Console.WriteLine(path);
```

`TryGet(Guid knownFolderId, out string path, uint flags = 0, nint token = 0)`
returns true only for `S_OK`; otherwise it returns false and an empty path.
Flags and the borrowed token pass through unchanged; the caller retains token
ownership. The native path buffer is always freed. Native DLL lookup defaults
to System32. GUID parsing belongs to the caller.

Build and run the read-only smoke checks (no additional test packages):

```powershell
dotnet build src/SevenKeyboard.GetKnownFolderPath/SevenKeyboard.GetKnownFolderPath.csproj -c Release
dotnet run --project tests/GetKnownFolderPath.Smoke/GetKnownFolderPath.Smoke.csproj -c Release
```

The original source header specifies The Unlicense.
