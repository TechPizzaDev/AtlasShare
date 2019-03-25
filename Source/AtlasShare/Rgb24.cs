using System.Runtime.InteropServices;

namespace AtlasShare
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rgb24
    {
        public byte R;
        public byte G;
        public byte B;

        public Rgb24(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }
    }
}