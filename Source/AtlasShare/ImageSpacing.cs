
namespace AtlasShare
{
    public struct ImageSpacing
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;

        public ImageSpacing(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
        
        public override string ToString()
        {
            return $"Left: {Left}, Right: {Right}, Top: {Top}, Bottom: {Bottom}";
        }
    }
}