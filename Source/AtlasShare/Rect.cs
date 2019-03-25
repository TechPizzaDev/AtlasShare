
namespace AtlasShare
{
    public struct Rect
    {
        public static readonly Rect Zero = new Rect();

        public int X;
        public int Y;
        public int W;
        public int H;

        public Rect(int x, int y, int width, int height)
        {
            this.X = x;
            this.Y = y;
            this.W = width;
            this.H = height;
        }
    }
}
