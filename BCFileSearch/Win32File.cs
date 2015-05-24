//using System.Windows.Forms;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Microsoft.Win32.SafeHandles;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace BASeCamp.Search
{


    public static class Win32File
    {
        private static String[] stdbyteprefixes =
        {
            " Bytes",
            "KB",
            "MB",
            "GB",
            "TB"
        };

        private static int getbyteprefixindex(long bytevalue)
        {
            int currindex = 0;
            long reduceit = bytevalue;
            while (reduceit > 1024)
            {
                reduceit /= 1024;
                currindex++;
            }
            return currindex;
        }

        public static String FormatSize(long amount)
        {
            int gotindex = getbyteprefixindex(amount);
            double calcamount = amount;
            for (int i = 0; i < gotindex; i++)
            {
                calcamount /= 1024;
            }
            return calcamount.ToString("F2", CultureInfo.InvariantCulture) + " " + stdbyteprefixes[gotindex];
        }
    }

    //Private Type IO_STATUS_BLOCK
//IoStatus                As Long
//Information             As Long
//End Type

    //Private Declare Function NtQueryInformationFile Lib "NTDLL.DLL" (ByVal FileHandle As Long, IoStatusBlock_Out As IO_STATUS_BLOCK, lpFileInformation_Out As Long, ByVal length As Long, ByVal FileInformationClass As Long) As Long

    public static class Win32Find
    {
        public enum FILE_INFORMATION_CLASS
        {
            FileDirectoryInformation = 1, // 1
            FileFullDirectoryInformation, // 2
            FileBothDirectoryInformation, // 3
            FileBasicInformation, // 4
            FileStandardInformation, // 5
            FileInternalInformation, // 6
            FileEaInformation, // 7
            FileAccessInformation, // 8
            FileNameInformation, // 9
            FileRenameInformation, // 10
            FileLinkInformation, // 11
            FileNamesInformation, // 12
            FileDispositionInformation, // 13
            FilePositionInformation, // 14
            FileFullEaInformation, // 15
            FileModeInformation = 16, // 16
            FileAlignmentInformation, // 17
            FileAllInformation, // 18
            FileAllocationInformation, // 19
            FileEndOfFileInformation, // 20
            FileAlternateNameInformation, // 21
            FileStreamInformation, // 22
            FilePipeInformation, // 23
            FilePipeLocalInformation, // 24
            FilePipeRemoteInformation, // 25
            FileMailslotQueryInformation, // 26
            FileMailslotSetInformation, // 27
            FileCompressionInformation, // 28
            FileObjectIdInformation, // 29
            FileCompletionInformation, // 30
            FileMoveClusterInformation, // 31
            FileQuotaInformation, // 32
            FileReparsePointInformation, // 33
            FileNetworkOpenInformation, // 34
            FileAttributeTagInformation, // 35
            FileTrackingInformation, // 36
            FileIdBothDirectoryInformation, // 37
            FileIdFullDirectoryInformation, // 38
            FileValidDataLengthInformation, // 39
            FileShortNameInformation, // 40
            FileHardLinkInformation = 46 // 46    
        }

        /*Private Type FILE_STREAM_INFORMATION
        NextEntryOffset         As Long
        StreamNameLength        As Long
        StreamSize              As Long
        StreamSizeHi            As Long
        StreamAllocationSize    As Long
        StreamAllocationSizeHi  As Long
        StreamName(259)         As Byte
    End Type
       
         */


        private static Dictionary<string, Icon> Extensionicons = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        public static int FileStreamInformation = 22;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr NtQueryInformationFile(IntPtr fileHandle, ref IO_STATUS_BLOCK IoStatusBlock, IntPtr pInfoBlock, uint length,
            FILE_INFORMATION_CLASS fileInformation);

        [DllImport("user32.dll")]
        private static extern int DestroyIcon(IntPtr hIcon);

        public static void PurgeIconCache()
        {
            Extensionicons.Clear();
        }

        /// <summary>
        /// Get the associated Icon for a file or application, this method always returns
        /// an icon.  If the strPath is invalid or there is no idonc the default icon is returned
        /// </summary>
        /// <param name="strPath">full path to the file</param>
        /// <param name="bSmall">if true, the 16x16 icon is returned otherwise the 32x32</param>
        /// <returns></returns>
        public static Icon GetIcon(string strPath, bool bSmall)
        {
            String gotext = Path.GetExtension(strPath);
            if (!Extensionicons.ContainsKey(gotext))
            {
                SHFILEINFO info = new SHFILEINFO(true);
                int cbFileInfo = Marshal.SizeOf(info);
                SHGFI flags;
                if (bSmall)
                    flags = SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes;
                else
                    flags = SHGFI.Icon | SHGFI.LargeIcon | SHGFI.UseFileAttributes;

                SHGetFileInfo(strPath, 256, out info, (uint) cbFileInfo, (uint) flags);
                Icon returnthis = Icon.FromHandle(info.hIcon);
                //DestroyIcon(info.hIcon);
                Extensionicons.Add(gotext, returnthis);
            }
            return Extensionicons[gotext];
        }
        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto,SetLastError=true)]
        public static extern IntPtr CreateFile(String lpFileName, FileAccess dwDesiredAccess, FileShare shareMode, IntPtr securityAttributes, FileMode dwCreationDispostion, EFileAttributes dwFlagsAndAttributes,
            int hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        //Private Declare Function CreateFileW Lib "kernel32" (ByVal lpFileName As Long, ByVal dwDesiredAccess As Long, ByVal dwShareMode As Long, lpSecurityAttributes As SECURITY_ATTRIBUTES, ByVal dwCreationDisposition As Long, ByVal dwFlagsAndAttributes As Long, ByVal hTemplateFile As Long) As Long

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        public static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        public static IEnumerable<WIN32_FIND_DATA> EnumerateFindData(String directory)
        {
            WIN32_FIND_DATA w32find;
            Trace.WriteLine("enumerating Directory " + directory);
            IntPtr findhandle = FindFirstFile(@"\\?\" + directory + @"\*", out w32find);
            //if no items found, break the enumeration.
            if (findhandle == INVALID_HANDLE_VALUE)
            {
                int lasterror = GetLastError();
                Debug.Print("LastError is " + lasterror);
                yield break;
            }
            do
            {
                if (!(w32find.cFileName == "." || w32find.cFileName == ".."))
                    yield return w32find;
            } while (FindNextFile(findhandle, out w32find));
            FindClose(findhandle);
        }
        

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Auto,Pack=4)]
        public struct FILE_STREAM_INFORMATION
        {
            public uint NextEntryOffset;
            public uint StreamAllocationSize;
            public UInt64 StreamNameLength;
            public UInt64 StreamSize;
        }

        public struct IO_STATUS_BLOCK
        {
            private ulong information;
            private uint status;
        }

        private struct SHFILEINFO
        {
            /// <summary>Maximal Length of unmanaged Windows-Path-strings</summary>
            private const int MAX_PATH = 260;

            /// <summary>Maximal Length of unmanaged Typename</summary>
            private const int MAX_TYPE = 80;

            public uint dwAttributes;

            public IntPtr hIcon;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_TYPE)] public string szTypeName;

            public SHFILEINFO(bool b)
            {
                hIcon = IntPtr.Zero;
                iIcon = 0;
                dwAttributes = 0;
                szDisplayName = "";
                szTypeName = "";
            }
        };

        [Flags]
        private enum SHGFI
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,

            /// <summary>get display name</summary>
            DisplayName = 0x000000200,

            /// <summary>get type name</summary>
            TypeName = 0x000000400,

            /// <summary>get attributes</summary>
            Attributes = 0x000000800,

            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,

            /// <summary>return exe type</summary>
            ExeType = 0x000002000,

            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,

            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,

            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,

            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,

            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,

            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,

            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,

            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,

            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,

            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,

            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,

            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;

            public ulong FileSize()
            {
                return nFileSizeLow + (ulong) nFileSizeHigh*4294967296;
            }

            public string FileName()
            {
                return cFileName.Replace('\0', ' ').Trim();
            }
        }
    }
}