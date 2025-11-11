using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace MusMux
{
    class SongItem
    {
        public TagLib.Tag Properties { get; private set; }
        public string Path { get; private set; }

        public SongItem(string path)
        {
            Path = path;
            TagLib.File file = TagLib.File.Create(path);
            Properties = file.Tag;
            file.Dispose();
        }
    }
}