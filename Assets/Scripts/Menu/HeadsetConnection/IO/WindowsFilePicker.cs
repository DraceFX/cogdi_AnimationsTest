using System;
using System.Runtime.InteropServices;

public static class WindowsFilePicker
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private class OpenFileName
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public string file = null;
        public int maxFile = 0;
        public string fileTitle = null;
        public int maxFileTitle = 0;
        public string initialDir = null;
        public string title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public string defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public string templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    public static string PickApk()
    {
        var ofn = new OpenFileName();
        ofn.structSize = Marshal.SizeOf(ofn);
        // Фильтр: "APK (*.apk)\0*.apk\0All Files (*.*)\0*.*\0\0"
        ofn.filter = "Android Package (*.apk)\0*.apk\0All Files (*.*)\0*.*\0\0";
        ofn.file = new string(new char[2048]);
        ofn.maxFile = ofn.file.Length;
        ofn.title = "Выберите APK для установки";
        ofn.dlgOwner = GetActiveWindow();

        // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY | OFN_NOCHANGEDIR
        ofn.flags = 0x00080000 | 0x00001000 | 0x00000004 | 0x00000008;

        if (GetOpenFileName(ofn) && !string.IsNullOrEmpty(ofn.file))
            return ofn.file.TrimEnd('\0');

        return null; // отмена или ошибка
    }
#else
    public static string PickApk()
    {
        Debug.LogWarning("Файловый диалог не поддерживается на этой платформе.");
        return null;
    }
#endif
}