//#define LOCAL_DEBUG
using MemoryPack;
using System.Diagnostics;
using System.IO.Compression;


namespace NeeLaboratory.IO.Search.Files
{
    public static class FileForestCache
    {
        public const string CacheFileName = "RealtimeSearch.cache";

        public static void Save(string path, FileForestMemento memento)
        {
            // TODO: キャッシュを使用しないのであればキャッシュファイルを削除する。これは上位処理だな

            // NOTE: UTF8だと不正なUnicodeファイル名があるとエラーになるのでUTF16そのままで
            var bin = MemoryPackSerializer.Serialize(memento, MemoryPackSerializerOptions.Utf16);

            using var stream = File.OpenWrite(path);

            var header = "NLRS0001"u8;
            stream.Write(header);
            Compress(stream, bin);
        }

        public static FileForestMemento? Load(string path)
        {
            // TODO: キャッシュを使用しないのであれば何もしない。これは上位処理だな。

            try
            {
                if (!File.Exists(path)) return null;

                using var stream = File.OpenRead(path);
                var header = new byte[8];
                int n = stream.Read(header);
                if (n != 8) return null;

                // check header
                var magic = header.AsSpan(0, 4);
                if (!magic.SequenceEqual("NLRS"u8)) return null;
                var version = int.Parse(header.AsSpan(4, 4));
                if (version != 1) return null; // バージョン違いは読み込み失敗

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
