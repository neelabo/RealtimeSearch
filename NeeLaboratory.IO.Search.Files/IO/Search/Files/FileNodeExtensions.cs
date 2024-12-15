//#define LOCAL_DEBUG

using NeeLaboratory.Collections;

namespace NeeLaboratory.IO.Search.Files
{
    public static class FileNodeExtensions
    {
        public static string GetFullPath(this Node<FileContent> node)
        {
            if (node.Parent is null && node.Name[1] == ':')
            {
                return node.Name + '\\';
            }
            else
            {
                return node.FullName;
            }
        }
    }
}