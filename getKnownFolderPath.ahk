#Requires AutoHotkey v2.0.0+
;==============================================================
; GetKnownFolderPath — Wrapper for SHGetKnownFolderPath
;
; GitHub: https://github.com/SevenKeyboard/get-known-folder-path
; Author: SevenKeyboard Ltd. (2025)
; License: The Unlicense
;
; Documentation / References:
;   SHGetKnownFolderPath():
;     https://www.autohotkey.com/boards/viewtopic.php?t=75602
;   KNOWNFOLDERID:
;     https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid
;==============================================================

/*
Example Usage:
    msgBox getKnownFolderPath("{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}") ;  C:\Program Files (x86)
*/

getKnownFolderPath(knownFolderId, dwFlags:=0x00000000, hToken:=0)    {
    static S_OK:=0
    knownFolderPath:=""
    loop 1    {
        GUID:=buffer(16,0)
        if (dllCall("Ole32.dll\IIDFromString", "Str",knownFolderId, "Ptr",GUID.ptr)!=S_OK)
            break
        if (dllCall("Shell32.dll\SHGetKnownFolderPath", "Ptr",GUID.ptr, "UInt",dwFlags, "Ptr",hToken, "Ptr*",&ppszPath:=0)!=S_OK)
            break
        knownFolderPath:=strGet(ppszPath,"UTF-16")
        dllCall("Ole32.dll\CoTaskMemFree", "Ptr",ppszPath)
    }
    return knownFolderPath
}