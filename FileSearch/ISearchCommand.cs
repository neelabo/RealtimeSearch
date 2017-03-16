namespace NeeLaboratory.IO.Search
{
    public interface ISearchCommand
    {
        string Keyword { get; set; }
        SearchOption Option { get; set; }

        void Exec();
        string ToString();
    }
}