//#define LOCAL_DEBUG

using System.Text;

namespace NeeLaboratory.IO.Search.Files
{
    public class FileHeader : IEquatable<FileHeader?>
    {
        public FileHeader(byte[] magic, int version)
        {
            if (magic.Length != 4) throw new ArgumentException($"{nameof(magic)} must be 4 bytes.");
            Magic = magic;
            Version = version;
        }

        public FileHeader(ReadOnlySpan<byte> magic, int version) : this(magic.ToArray(), version)
        {
        }


        public byte[] Magic { get; init; }
        public int Version { get; init; }

        public static void Write(Stream stream, FileHeader header)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(header.Magic);
            writer.Write(header.Version);
        }

        public static FileHeader Read(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            var magic = reader.ReadBytes(4);
            var version = reader.ReadInt32();
            return new FileHeader(magic, version);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as FileHeader);
        }

        public bool Equals(FileHeader? other)
        {
            return other is not null &&
                Magic.SequenceEqual(other.Magic) &&
                Version == other.Version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Magic, Version);
        }

        public static bool operator ==(FileHeader? left, FileHeader? right)
        {
            return EqualityComparer<FileHeader>.Default.Equals(left, right);
        }

        public static bool operator !=(FileHeader? left, FileHeader? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"Magic=\"{System.Text.Encoding.ASCII.GetString(Magic)}\", Version={Version}";
        }
    }
}
