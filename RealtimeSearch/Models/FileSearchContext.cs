using NeeLaboratory.IO.Search;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileSearchContext : SearchContext
    {
        public FileSearchContext(SearchValueCache cache) : base(cache)
        {
            AddProfile(new DateSearchProfile());
            AddProfile(new SizeSearchProfile());
            AddProfile(new DirectorySearchProfile());
            AddProfile(new PinnedSearchProfile());
        }
    }

    public class DirectorySearchProfile : SearchProfile
    {
        public DirectorySearchProfile()
        {
            Options.Add(new SearchPropertyProfile("directory", BooleanSearchValue.Default));

            Alias.Add("/directory", new() { "/p.directory" });
        }
    }

    public class PinnedSearchProfile : SearchProfile
    {
        public PinnedSearchProfile()
        {
            Options.Add(new SearchPropertyProfile("pinned", BooleanSearchValue.Default));

            Alias.Add("/pinned", new() { "/p.pinned" });
        }
    }

}
