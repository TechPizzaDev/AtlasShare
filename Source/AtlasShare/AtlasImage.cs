using System.IO;

namespace AtlasShare
{
    public class AtlasImage
    {
        public FileInfo File { get; }
        public string Tag { get; }

        public int Width { get; }
        public int Height { get; }

        public AtlasImage(FileInfo image, int width, int height, string tag)
        {
            File = image;
            Tag = tag;
            Width = width;
            Height = height;
        }
    }
}
