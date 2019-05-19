using System.IO;

namespace AtlasShare
{
    public class AtlasImage
    {
        public FileInfo File { get; }
        public string RelativePath { get; }

        public int Width { get; }
        public int Height { get; }

        public AtlasImage(FileInfo image, string relativePath, int width, int height)
        {
            File = image;
            RelativePath = relativePath;
            Width = width;
            Height = height;
        }
    }
}
