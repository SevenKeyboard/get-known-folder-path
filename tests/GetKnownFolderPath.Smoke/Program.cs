using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using SevenKeyboard;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

internal static class Program
{
    private static readonly Guid LocalAppDataLow = new("A520A1A4-1780-4FF6-BD18-167343C5AF16");
    private static readonly Guid Profile = new("5E6C858F-0E22-4760-9AFE-EA3317B67173");
    private static readonly Guid SystemFolder = new("1AC14E77-02E7-4E5D-B744-2EB1AE5198B7");

    private static int Main()
    {
        try
        {
            Console.WriteLine($"Process: {RuntimeInformation.ProcessArchitecture}");
            CheckNative("LocalAppDataLow", LocalAppDataLow);
            CheckExpected("Profile", Profile, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            CheckExpected("System", SystemFolder, Environment.GetFolderPath(Environment.SpecialFolder.System));

            // The Program Files (x86) GUID is the original AHK usage example.
            CheckExpected("AHK ProgramFilesX86 example",
                new Guid("7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E"),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));

            CheckFailure("Empty GUID", Guid.Empty);
            CheckFailure("Unknown GUID", new Guid("D5B654BB-1E16-48FB-8B02-C66F55CA0A78"));
            CheckFailure("Virtual Computer folder", new Guid("0AC0837C-BBF8-452A-850D-79D08E667CA7"));

            // No CREATE, INIT, writes, or Default User (-1) token requests.
            CheckNative("DONT_VERIFY", LocalAppDataLow, 0x00004000);
            CheckNative("DEFAULT_PATH | DONT_VERIFY", LocalAppDataLow, 0x00004400);
            CheckNative("NOT_PARENT_RELATIVE | DEFAULT_PATH | DONT_VERIFY", LocalAppDataLow, 0x00004600);

            // Let Windows decide whether undefined/invalid flags fail or are ignored.
            CheckNative("Undefined flag bit", Profile, 0x00000002);
            CheckNative("NOT_PARENT_RELATIVE without DEFAULT_PATH", LocalAppDataLow, 0x00000200);

            using WindowsIdentity identity = WindowsIdentity.GetCurrent(
                TokenAccessLevels.Query | TokenAccessLevels.Impersonate);
            nint token = identity.AccessToken.DangerousGetHandle();
            CheckNative("Borrowed current-user token", Profile, token: token);
            Require(GetKnownFolderPath.TryGet(Profile, out string tokenPath, token: token),
                "Borrowed-token profile query failed.");
            Require(tokenPath == Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Borrowed-token profile differs from .NET.");
            CheckTokenOpen(token);
            CheckNative("Unknown GUID with borrowed token", Guid.Empty, token: token);
            CheckTokenOpen(token);

            CheckHandles(token);
            CheckTokenOpen(token);
            Console.WriteLine("PASS: all read-only smoke checks.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception}");
            return 1;
        }
    }

    private static void CheckExpected(string name, Guid id, string expected)
    {
        Require(expected != "", $"{name}: .NET did not return an existing path.");
        Require(GetKnownFolderPath.TryGet(id, out string path), $"{name}: query failed.");
        Require(path == expected, $"{name}: '{path}' differs from .NET '{expected}'.");
        Console.WriteLine($"PASS: {name} = {path}");
    }

    private static void CheckFailure(string name, Guid id)
    {
        Require(!GetKnownFolderPath.TryGet(id, out string path), $"{name}: expected failure.");
        Require(path == "", $"{name}: failure did not clear the output.");
        CheckNative(name, id);
    }

    private static void CheckNative(string name, Guid id, uint flags = 0, nint token = 0)
    {
        // Independent runtime marshalling supplies/frees the native string.
        // The library uses source-generated interop and explicit pointer ownership.
        int result = SHGetKnownFolderPath(in id, flags, token, out string? expected);
        bool success = GetKnownFolderPath.TryGet(id, out string actual, flags, token);
        Require(success == (result == 0), $"{name}: success differs from native HRESULT 0x{result:X8}.");
        Require(actual == (result == 0 ? expected : ""), $"{name}: output differs from native.");
        Console.WriteLine($"PASS: {name}; native HRESULT=0x{result:X8}; path={actual}");
    }

    private static void CheckTokenOpen(nint token)
    {
        if (!GetHandleInformation(token, out _))
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Borrowed token was closed.");
    }

    private static void CheckHandles(nint token)
    {
        // Warm native caches and JIT before measuring successive equal-sized batches.
        RunBatch(token, 1000);
        Console.WriteLine("Handle-count warmup complete.");
        uint baseline = HandleCount();
        uint minimum = baseline;
        uint maximum = baseline;
        Console.Write($"Handle counts after warmup: {baseline}");
        for (int batch = 0; batch < 5; batch++)
        {
            RunBatch(token, 2000);
            uint count = HandleCount();
            minimum = Math.Min(minimum, count);
            maximum = Math.Max(maximum, count);
            Console.Write($", {count}");
        }
        Console.WriteLine();
        // Allow small runtime bookkeeping changes, not per-call handle growth.
        Require(maximum - minimum <= 8, "Handle count grew across repeated success/failure calls.");
        Console.WriteLine("PASS: 30,000 measured calls; handle-count range <= 8.");
    }

    private static void RunBatch(nint token, int repetitions)
    {
        for (int i = 0; i < repetitions; i++)
        {
            Require(GetKnownFolderPath.TryGet(Profile, out string profile, token: token) && profile != "",
                "Repeated profile query failed.");
            Require(!GetKnownFolderPath.TryGet(Guid.Empty, out string missing) && missing == "",
                "Repeated unknown-folder query changed failure semantics.");
            // DONT_VERIFY avoids assuming LocalLow already exists on a fresh profile.
            Require(GetKnownFolderPath.TryGet(LocalAppDataLow, out string low, 0x00004000) && low != "",
                "Repeated LocalAppDataLow query failed.");
        }
    }

    private static uint HandleCount()
    {
        if (!GetProcessHandleCount(new nint(-1), out uint count))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        return count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHGetKnownFolderPath(in Guid id, uint flags, nint token,
        [MarshalAs(UnmanagedType.LPWStr)] out string? path);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetHandleInformation(nint handle, out uint flags);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessHandleCount(nint process, out uint count);
}
