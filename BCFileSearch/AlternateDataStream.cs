using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using BASeCamp.Search;
using Microsoft.Win32.SafeHandles;

namespace BCFileSearch
{
    public class AlternateDataStream : FileSystemInfo
    {

        private String SourceFile;
        
        private String _ADSName;
        public AlternateDataStream(String pSourceFile,String pADSName)
        {
            SourceFile = pSourceFile;
            ADSName = pADSName;
        }
        public override string FullName
        {
            get
            {
                return FullPath;
            }
        }
        public new String FullPath { get { return SourceFile + ":" + _ADSName; } set { throw new NotImplementedException(); } }
        public String ADSName { get { return _ADSName; } set { _ADSName = value; } }
        public override void Delete()
        {
            Win32ADS.DeleteFile(FullPath);
        }

        public override string Name
        {
            get { return FullPath; }
        }

        public override bool Exists
        {
            get {  //attempt to open the stream.
                IntPtr hFile = Win32Find.CreateFile(FullPath, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, FileMode.Open, Win32Find.EFileAttributes.Normal, 0);
                try
                {
                    if (hFile == new IntPtr(-1))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                finally
                {
                    if(hFile!= new IntPtr(-1)) Win32Find.CloseHandle(hFile);
                }
            }
        }
        public Stream OpenStream(FileAccess pFileAccess=FileAccess.Read,FileShare pShareMode=FileShare.ReadWrite,FileMode pFileMode=FileMode.OpenOrCreate)
        {
            IntPtr hFile = Win32Find.CreateFile(FullPath, pFileAccess, pShareMode, IntPtr.Zero, pFileMode, Win32Find.EFileAttributes.Normal, 0);
            if (hFile == new IntPtr(-1))
            {
                var lasterr = Marshal.GetLastWin32Error();
                throw new Win32Exception(lasterr);
            }
            else
            {
               
                    SafeFileHandle sfh = new SafeFileHandle(hFile,true);
                    return new FileStream(sfh,pFileAccess);
            }

        }
    }
    public static class FileInfoExtensions
    {
        public static IEnumerable<AlternateDataStream> AlternateStreams(this FileInfo SourceFile)
        {
            foreach( var iterate in Win32ADS.GetStreams(SourceFile.FullName))
            {
                yield return new AlternateDataStream(SourceFile.FullName,iterate);
            }
        }
    }
    public class Win32ADS
    {
        private const int ERROR_HANDLE_EOF = 38;
        public static int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        
        [DllImport("kernel32.dll", SetLastError = true,CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(string lpFileName);
        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFindHandle FindFirstStreamW(string lpFileName, StreamInfoLevels InfoLevel,
            [In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_STREAM_DATA lpFindStreamData, uint dwFlags);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FindNextStreamW(SafeFindHandle hndFindFile, [In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_STREAM_DATA lpFindStreamData);
        public static IEnumerable<String> GetStreams(String sFile)
        {
            //if Vista or later, we can use the Find Stream functions.
            if(Environment.OSVersion.Version.Major >= 6)
            {
                return GetStreams_Vista(sFile);
            }
            else
            {
                //otherwise, we're on XP... Or non-Windows. The caller can expect to handle THAT case, I think.
                return GetStreamsXP(sFile);
            }
        }
        public static IEnumerable<String> GetStreams(FileInfo Source)
        {
            return GetStreams(Source.FullName);
        } 
        private static IEnumerable<string> GetStreams_Vista(String sPath)
        {
            if (sPath == null) throw new ArgumentNullException("sPath");
            WIN32_FIND_STREAM_DATA findStreamData = new WIN32_FIND_STREAM_DATA();
            SafeFindHandle handle = FindFirstStreamW(sPath, StreamInfoLevels.FindStreamInfoStandard, findStreamData, 0);
            if (handle.IsInvalid) throw new Win32Exception();
            try
            {
                do
                {
                    yield return findStreamData.cStreamName;
                } while (FindNextStreamW(handle, findStreamData));
                int lastError = Marshal.GetLastWin32Error();
                if (lastError != ERROR_HANDLE_EOF) throw new Win32Exception(lastError);
            }
            finally
            {
                handle.Dispose();
            }
        }
        private static IEnumerable<string> GetStreamsXP(String file)
        {
            //on Windows XP, we can use NTQueryInformationFile.
            //first, open the file with Backup Semantics.
            IntPtr hFile = Win32Find.CreateFile(file, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, Win32Find.EFileAttributes.BackupSemantics, 0);
            //if the Open fails, we'll throw an exception.
            if (hFile == new IntPtr(-1))
            {
                var lasterr = Marshal.GetLastWin32Error();
                throw new Win32Exception(lasterr);
            }
            int lErr = 234;
            uint Chunksize = 4096;
            byte[] databuffer = new byte[Chunksize];
            MemoryStream StreamData = new MemoryStream();
            Win32Find.IO_STATUS_BLOCK IoBlock = new Win32Find.IO_STATUS_BLOCK();
            while (lErr == 234) //234 means more file information is available to read.
            {
                databuffer = new byte[Chunksize];

                IntPtr resultitem = Marshal.AllocHGlobal((int)Chunksize);

                IntPtr result = Win32Find.NtQueryInformationFile(hFile, ref IoBlock, resultitem, Chunksize, Win32Find.FILE_INFORMATION_CLASS.FileStreamInformation);
                lErr = result.ToInt32();
                Marshal.Copy(resultitem, databuffer, 0, (int)Chunksize);
                Marshal.FreeHGlobal(resultitem);
                StreamData.Write(databuffer, 0, (int)Chunksize);
            }

            StreamData.Seek(0, SeekOrigin.Begin);
            BinaryReader sr = new BinaryReader(StreamData);


            do
            {
                //now read in a FSInfo structure.
                Win32Find.FILE_STREAM_INFORMATION fsInfo = new Win32Find.FILE_STREAM_INFORMATION();
                //seek to the start of the Memory Buffer.

                int fssize = Marshal.SizeOf(typeof(Win32Find.FILE_STREAM_INFORMATION));
                byte[] fsdata = new byte[fssize];

                StreamData.Read(fsdata, 0, fssize);
                String readstring = Encoding.Unicode.GetString(fsdata);
                //marshal this byte into a structure.

                GCHandle fshandle = GCHandle.Alloc(fsdata, GCHandleType.Pinned);
                fsInfo = (Win32Find.FILE_STREAM_INFORMATION)Marshal.PtrToStructure(fshandle.AddrOfPinnedObject(), typeof(Win32Find.FILE_STREAM_INFORMATION));
                Byte[] grabbytes = sr.ReadBytes((int)fsInfo.StreamNameLength);
                String acquiredName = Encoding.Unicode.GetString(grabbytes);

                yield return acquiredName;

                fshandle.Free();
                if (fsInfo.NextEntryOffset == 0) break;
                sr.ReadBytes((int)fsInfo.NextEntryOffset - fssize - (int)fsInfo.StreamNameLength);
            } while (true);
            Win32Find.CloseHandle(hFile);
        }

        private enum StreamInfoLevels
        {
            FindStreamInfoStandard = 0
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WIN32_FIND_STREAM_DATA
        {
            public long StreamSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
            public string cStreamName;
        }
    }

    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeFindHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return FindClose(this.handle);
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FindClose(IntPtr handle);



    }
    
}
