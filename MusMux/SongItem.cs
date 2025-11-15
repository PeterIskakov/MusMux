namespace MusMux
{
    public class SongItem
    {
        public readonly string Title;
        public readonly string Artist;
        public readonly string Path;

        public SongItem(string path)
        {
            Path = path;
            TagLib.File file = TagLib.File.Create(path);
            Title = file.Tag.Title;
            Title ??= System.IO.Path.GetFileName(path);
            Artist = file.Tag.FirstPerformer;
            file.Dispose();
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not SongItem) return false;
            SongItem s = (SongItem)obj;
            return s.Path == Path;
        }

        public override int GetHashCode()
        {
            return 17 + Path.GetHashCode();
        }
    }
}