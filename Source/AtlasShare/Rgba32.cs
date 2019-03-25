using System.Runtime.InteropServices;

namespace AtlasShare
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rgba32
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
        
        public Rgba32(byte r, byte g, byte b, byte a)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }
    }
}