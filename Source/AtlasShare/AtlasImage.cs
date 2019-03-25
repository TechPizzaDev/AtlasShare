using MonoGame.Imaging;
using System;

namespace AtlasShare
{
    public class AtlasImage : IDisposable
    {
        public Image Image { get; }
        public string Tag { get; }

        public int Width => Image.Width;
        public int Height => Image.Height;

        public AtlasImage(Image image, string tag)
        {
            Image = image;
            Tag = tag;
        }

        public void Dispose()
        {
            Image.Dispose();
        }
    }
}
