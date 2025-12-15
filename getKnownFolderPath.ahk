#Requires AutoHotkey v1.1.35+
;==============================================================
; GetKnownFolderPath — Wrapper for SHGetKnownFolderPath
;
; GitHub: https://github.com/SevenKeyboard/get-known-folder-path
; Author: SevenKeyboard Ltd. (2025)
; License: The Unlicense
;
; Documentation / References:
;   SHGetKnownFolderPath()
;     https://www.autohotkey.com/boards/viewtopic.php?t=75602
;   KNOWNFOLDERID
;     https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid
;==============================================================

/*
Example Usage:
    msgBox % getKnownFolderPath("{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}") ;  C:\Program Files (x86)
*/

class VersionManager_getKnownFolderPath
{
    static _ := VersionManager_getKnownFolderPath._init()
    _init()    {
        global
        GETKNOWNFOLDERPATH_VERSION := "1.0.0"
    }
}
getKnownFolderPath(knownFolderId, dwFlags:=0x00000000, hToken:=0)    {
    static S_OK:=0
    knownFolderPath:=""
    loop 1    {
        varSetCapacity(GUID,16,0)
        if (dllCall("Ole32.dll\IIDFromString", "Str",knownFolderId, "Ptr",&GUID, "Int")!==S_OK)
            break
        if (dllCall("Shell32.dll\SHGetKnownFolderPath", "Ptr",&GUID, "UInt",dwFlags, "Ptr",hToken, "Ptr*",ppszPath, "Int")!==S_OK)
            break
        knownFolderPath:=strGet(ppszPath,"UTF-16")
        dllCall("Ole32.dll\CoTaskMemFree", "Ptr",ppszPath)
    }
    return knownFolderPath
}