//#define LOCAL_DEBUG
using MemoryPack;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;


namespace NeeLaboratory.IO.Search.Files
{
    public static class FileForestCacheTest
    {
        public static void Test(FileForest forest)
        {
            var trees = forest.Trees;
            foreach (var tree in trees)
            {
                var count = trees.Sum(e => e.WalkAll().Count());
                Debug.WriteLine($"Node: Count={count}");
                var memento = forest.CreateMemento();

                // for MemoryPack
                try
                {
                    var sw = Stopwatch.StartNew();
                    // NOTE: UTF8だと不正なUnicodeテキストがあるとエラーになるのでUTF16そのままで
                    var bin = MemoryPackSerializer.Serialize(memento, MemoryPackSerializerOptions.Utf16);
                    var comp = Compress(bin);
                    Debug.WriteLine($"MemoryPack.Serialize: {sw.ElapsedMilliseconds}ms, Size={bin.Length:#,0}, Comp={comp.Length:#,0}");

                    sw.Restart();
                    bin = Decompress(comp);
                    var val = MemoryPackSerializer.Deserialize<FileForestMemento>(bin);
                    Debug.WriteLine($"MemoryPack.Deserialize: {sw.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                // for System.Text.Json
                try
                {
                    var sw = Stopwatch.StartNew();
                    var bin = JsonSerializer.SerializeToUtf8Bytes(memento);
                    var comp = Compress(bin);
                    Debug.WriteLine($"JsonSerializer.Serialize: {sw.ElapsedMilliseconds}ms, Size={bin.Length:#,0}, Comp={comp.Length:#,0}");

                    sw.Restart();
                    bin = Decompress(comp);
                    var val = JsonSerializer.Deserialize<FileForestMemento>(bin);
                    Debug.WriteLine($"JsonSerializer.Deserialize: {sw.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                // MemoryPack, MessagePack のパフォーマンス差は意識するほどのものはない。System.Text.Json はこれらに比べると数倍遅い
                // テキスト情報が多いので圧縮がよく効く
                // [NVRS0001] <= HEADER
                // 実データそのままで。

#if false  // RestoreMemento が Trunk を生成するようになったので以下の処理はできないぞ。Memento の Restore の正当性テストは別で行おう。
                var newTrees = RestoreMemento(memento);
                Debug.Assert(newTrees is not null);
                //newTree.Dump();
                var count2 = newTrees.Sum(e => e.WalkAll().Count());
                Debug.Assert(count == count2);
#endif
            }
        }

        public static byte[] Compress(byte[] src)
        {
            using (var ms = new MemoryStream())
            {
                using (var ds = new DeflateStream(ms, CompressionMode.Compress, true/*msは*/))
                {
                    ds.Write(src, 0, src.Length);
                }

                // 圧縮した内容を byte 配列にして取り出す
                ms.Position = 0;
                byte[] comp = new byte[ms.Length];
                ms.Read(comp, 0, comp.Length);
                return comp;
            }
        }

        public static byte[] Decompress(byte[] src)
        {
            using (var ms = new MemoryStream(src))
            using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
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
