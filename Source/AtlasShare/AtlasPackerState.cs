using System.Collections.Generic;

namespace AtlasShare
{
    internal class AtlasPackerState
    {
        public const int STEP_SIZE = 16;

        private bool _turn;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public int MaxSize { get; }
        public ImageSpacing Spacing { get; }
        public bool IsSingle { get; }
        public List<Item> Items { get; }
        public RectPacker Packer { get; }
        public ChoiceHeuristic Heuristic { get; }

        public AtlasPackerState(int maxSize, ImageSpacing spacing, bool isSingle)
        {
            Width = STEP_SIZE;
            Height = STEP_SIZE;

            MaxSize = maxSize;
            Spacing = spacing;
            IsSingle = isSingle;
            Items = new List<Item>();
            Heuristic = ChoiceHeuristic.BestAreaFit;

            if (!IsSingle)
                Packer = new RectPacker(Width, Height, false);
        }

        public bool Step()
        {
            if (Width == MaxSize && Height == MaxSize)
                return false;

            _turn = !_turn;
            if (_turn)
                Width += STEP_SIZE;
            else
                Height += STEP_SIZE;

            if (Width > MaxSize)
                Width = MaxSize;

            if (Height > MaxSize)
                Height = MaxSize;

            Packer.Initialize(Width, Height, false);
            foreach (var item in Items)
            {
                Insert(item.AtlasImage, out Rect rect);
                item.Rect = rect;
            }

            return true;
        }

        public void Trim()
        {
            foreach(var free in Packer.FreeRectangles)
            {
                if (free.W == Packer.BinWidth)
                    Height -= free.H;
                else if (free.H == Packer.BinHeight)
                    Width -= free.W;
            }
        }

        public bool Insert(AtlasImage image, out Rect rect)
        {
            int width = image.Width + Spacing.Left + Spacing.Right;
            int height = image.Height + Spacing.Top + Spacing.Bottom;

            if (Packer.Insert(width, height, Heuristic, out rect))
            {
                // we don't want to change the size of the image,
                // so we remove the offsets after packing
                rect.X += Spacing.Left;
                rect.Y += Spacing.Top;
                rect.W -= Spacing.Left + Spacing.Right;
                rect.H -= Spacing.Top + Spacing.Bottom;

                return true;
            }
            return false;
        }

        public class Item
        {
            public Rect Rect { get; internal set; }
            public AtlasImage AtlasImage { get; }

            public Item(Rect rect, AtlasImage image)
            {
                Rect = rect;
                AtlasImage = image;
            }
        }
    }
}
