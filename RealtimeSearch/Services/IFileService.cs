namespace NeeLaboratory.RealtimeSearch
{
    public interface IFileService
    {
        void Delete(string folderPath, string fileName);
        T? Read<T>(string folderPath, string fileName);
        void Write<T>(string folderPath, string fileName, T content, bool isCreateBackup);
    }
}