//#define LOCAL_DEBUG
using MemoryPack;
using System.Diagnostics;
using System.IO.Compression;


namespace NeeLaboratory.IO.Search.Files
{
    public static class FileForestCache
    {
        public const string CacheFileName = "RealtimeSearch.cache";
        public static FileHeader DefaultFileHeader { get; } = new FileHeader("RSNC"u8, 1); // RealtimeSearch Node Cache, Version 1

        public static void Save(string path, FileForestMemento memento)
        {
            // NOTE: UTF8だと不正なUnicodeファイル名があるとエラーになるのでUTF16そのままで
            var bin = MemoryPackSerializer.Serialize(memento, MemoryPackSerializerOptions.Utf16);

            using var stream = File.OpenWrite(path);
            FileHeader.Write(stream, DefaultFileHeader);
            Compress(stream, bin);
        }

        public static void Remove(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static FileForestMemento? Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                using var stream = File.OpenRead(path);

                var header = FileHeader.Read(stream);
                if (header != DefaultFileHeader) return null;

                var bin = Decompress(stream);
                return MemoryPackSerializer.Deserialize<FileForestMemento>(bin);
            }
            catch (Exception ex)
            {
                // すべての例外を読み込み失敗とみなす
                Debug.WriteLine(ex);
                return null;
            }
        }

        private static void Compress(Stream stream, byte[] src)
        {
            using (var ds = new DeflateStream(stream, CompressionMode.Compress, true))
            {
                ds.Write(src, 0, src.Length);
            }
        }

        private static byte[] Decompress(Stream stream)
        {
            using (var ds = new DeflateStream(stream, CompressionMode.Decompress))
            {
                using (var dest = new MemoryStream())
                {
                    ds.CopyTo(dest);

                    dest.Position = 0;
                    byte[] decomp = new byte[dest.Length];
                    dest.Read(decomp, 0, decomp.Length);
                    return decomp;
                }
            }
        }
    }
}
