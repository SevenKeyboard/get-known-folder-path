//==============================================================
// GetKnownFolderPath — Wrapper for SHGetKnownFolderPath
//
// GitHub: https://github.com/SevenKeyboard/get-known-folder-path
// Author: SevenKeyboard Ltd. (2025-2026)
// License: The Unlicense
//
// Documentation / References:
//   SHGetKnownFolderPath()
//     https://www.autohotkey.com/boards/viewtopic.php?t=75602
//     https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shgetknownfolderpath
//   KNOWNFOLDERID
//     https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid
//==============================================================

using System;
using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace SevenKeyboard;

public static partial class GetKnownFolderPath
{
    public static bool TryGet(Guid knownFolderId, out string path, uint flags = 0, nint token = 0)
    {
        path = string.Empty;
        nint nativePath = 0;
        try
        {
            if (SHGetKnownFolderPath(in knownFolderId, flags, token, out nativePath) != 0)
                return false;

            path = Marshal.PtrToStringUni(nativePath)!;
            return true;
        }
        finally
        {
            // The API requires freeing the buffer even on failure; token is borrowed.
            CoTaskMemFree(nativePath);
        }
    }

    [LibraryImport("shell32.dll")]
    private static partial int SHGetKnownFolderPath(in Guid knownFolderId, uint flags, nint token, out nint path);

    [LibraryImport("ole32.dll")]
    private static partial void CoTaskMemFree(nint memory);
}
