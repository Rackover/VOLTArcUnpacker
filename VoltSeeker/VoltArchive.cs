using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VoltSeeker
{
    internal class VoltArchive : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct VoltEntry
        {
            public uint Unk1;
            public uint Unk2;
            public uint Offset;

            public override string ToString()
            {
                return $"Entry {Unk1} {Unk2} at offset {Offset}";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VoltFile
        {
            public ulong FileOffset;
            public ulong FileSize;

            [MarshalAs(UnmanagedType.LPStr)] // Ansi, null terminated
            public string FileName;

            public override string ToString()
            {
                return $"{FileName,-36} | {FormatSize(FileSize),8} | off {FileOffset:X8}";
            }
        }

        public readonly ulong DiskSize;
        public readonly string FileName;
        public readonly int Version;
        public readonly int FileCount;
        public readonly int FileListLength;

        private VoltEntry[] entries;
        private VoltFile[] files;

        private FileStream fs;

        public VoltArchive(string fileName)
        {
            fs = File.OpenRead(fileName);

            using (BinaryReader br = new BinaryReader(fs, leaveOpen:true, encoding:Encoding.ASCII)) {
                if (System.Text.Encoding.ASCII.GetString(br.ReadBytes(4)) != "VOLT") throw new Exception("Invalid magic VOLT");

                FileName = Path.GetFileName(fileName);
                Version = br.ReadInt32();
                FileCount = br.ReadInt32();
                FileListLength = br.ReadInt32(); // unused

                entries = new VoltEntry[FileCount];
                for (int i = 0; i < FileCount; i++) {
                    var rawEntry = br.ReadBytes(12);

                    int size = Marshal.SizeOf(typeof(VoltEntry));
                    IntPtr ptr = Marshal.AllocHGlobal(size);

                    Marshal.Copy(rawEntry, 0, ptr, size);

                    entries[i] = (VoltEntry)Marshal.PtrToStructure(ptr, typeof(VoltEntry));
                    Marshal.FreeHGlobal(ptr);
                }

                files = new VoltFile[FileCount];
                for (int i = 0; i < FileCount; i++) {
                    var file = new VoltFile() {
                        FileOffset = br.ReadUInt64(),
                        FileSize = br.ReadUInt64(),
                        FileName = ReadCString(br)
                    };

                    files[i] = file;
                }
            }


            DiskSize = (ulong)new FileInfo(fileName).Length;
        }

        public void SaveToDirectory(string directory)
        {
            var info = new DirectoryInfo(directory);
            if (!info.Exists) {
                info = Directory.CreateDirectory(directory);
            }

            for (int i = 0; i < files.Length; i++) {

                using (var output = File.OpenWrite(Path.Combine(info.FullName, files[i].FileName))) {
                    using (var bw = new BinaryWriter(output)) {
                        fs.Seek((long)files[i].FileOffset, SeekOrigin.Begin);
                        ulong remainingToRead = files[i].FileSize;

                        while (remainingToRead > 0) {
                            byte[] buff = new byte[1024];
                            var bytesRead = fs.Read(buff, 0, (int)Math.Min((ulong)buff.Length, remainingToRead));

                            remainingToRead -= (ulong)bytesRead;

                            bw.Write(buff, 0, bytesRead);
                        }
                    }
                }
            }
        }

        private string ReadCString(BinaryReader br)
        {
            StringBuilder sb = new StringBuilder();
            byte b;
            while (true) {
                b = br.ReadByte();

                if (b == 0)
                    break;
                else
                    sb.Append((char)b);
            }

            return sb.ToString();
        }
        private static string FormatSize(ulong byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            ulong bytes = (ulong)Math.Abs((decimal)byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign((decimal)byteCount) * num).ToString() + suf[place];
        }

        public override string ToString()
        {
            return $"{FileName,-10} ({FormatSize(DiskSize)} on disk) / {FileCount,3} files\n    - {string.Join("\n    - ", entries.Select((e, i) => $"{files[i]}"))}";
        }

        public void Dispose()
        {
            ((IDisposable)fs).Dispose();
        }
    }
}
