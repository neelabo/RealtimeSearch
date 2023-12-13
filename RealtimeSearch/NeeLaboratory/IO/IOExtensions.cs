using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeeLaboratory.IO
{
    public static class IOExtensions
    {
        /// <summary>
        /// EnumerateFileSystemInfos with CancellationToken
        /// </summary>
        public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo directoryInfo, string searchPattern, EnumerationOptions options, CancellationToken token)
        {
            foreach (var item in directoryInfo.EnumerateFileSystemInfos(searchPattern, options))
            {
                token.ThrowIfCancellationRequested();
                yield return item;
            }
        }

        /// <summary>
        /// 一般的な？ EnumerationOption を作成する
        /// </summary>
        /// <param name="recursiveSubDirectories">サブフォルダーを含める</param>
        /// <param name="allowHidden">隠しファイルを含める</param>
        /// <returns></returns>
        public static EnumerationOptions CreateEnumerationOptions(bool recursiveSubDirectories, bool allowHidden)
        {
            return new EnumerationOptions()
            {
                AttributesToSkip = FileAttributes.System | FileAttributes.ReparsePoint | FileAttributes.Temporary | (allowHidden ? 0 : FileAttributes.Hidden),
                IgnoreInaccessible = true,
                RecurseSubdirectories = recursiveSubDirectories,
            };
        }

        /// <summary>
        /// EnumerationOption のクローン
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static EnumerationOptions Clone(this EnumerationOptions options)
        {
            return new EnumerationOptions()
            {
                AttributesToSkip = options.AttributesToSkip,
                BufferSize = options.BufferSize,
                IgnoreInaccessible = options.IgnoreInaccessible,
                MatchCasing = options.MatchCasing,
                MatchType = options.MatchType,
                MaxRecursionDepth = options.MaxRecursionDepth,
                RecurseSubdirectories = options.RecurseSubdirectories,
                ReturnSpecialDirectories = options.ReturnSpecialDirectories,
            };
        }

    }
}