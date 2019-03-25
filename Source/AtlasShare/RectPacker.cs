using GeneralShare.Collections;
using System;
using System.Collections.Generic;

namespace AtlasShare
{
    public enum ChoiceHeuristic
    {
        BestShortSideFit, ///< -BSSF: Positions the rectangle against the short side of a free rectangle into which it fits the best.
        BestLongSideFit, ///< -BLSF: Positions the rectangle against the long side of a free rectangle into which it fits the best.
        BestAreaFit, ///< -BAF: Positions the rectangle into the smallest free rect into which it fits.
        BottomLeftRule, ///< -BL: Does the Tetris placement.
        ContactPointRule ///< -CP: Choosest the placement where the rectangle touches other rects as much as possible.
    };

    /// <summary>
    /// Based on the Public Domain MaxRectsBinPack.cpp source by Jukka Jylänki:
    /// <see href="https://github.com/juj/RectangleBinPack/"/>
    /// <para/>
    /// Ported to C# by Sven Magnus, this version is also public domain;
    /// do whatever you want with it.
    /// </summary>
    internal class RectPacker
    {
        private ListArray<Rect> _usedRects;
        private ListArray<Rect> _freeRects;

        public int BinWidth { get; private set; } = 0;
        public int BinHeight { get; private set; } = 0;
        public bool AllowRotation { get; set; }

        public IReadOnlyList<Rect> UsedRectangles { get; }
        public IReadOnlyList<Rect> FreeRectangles { get; }

        public RectPacker(int width, int height, bool rotations)
        {
            _usedRects = new ListArray<Rect>();
            _freeRects = new ListArray<Rect>();
            UsedRectangles = _usedRects.AsReadOnly();
            FreeRectangles = _freeRects.AsReadOnly();
            
            Initialize(width, height, rotations);
        }

        public void Initialize(int width, int height, bool rotations)
        {
            BinWidth = width;
            BinHeight = height;
            AllowRotation = rotations;

            _usedRects.Clear();
            _freeRects.Clear();

            _freeRects.Add(new Rect(0, 0, width, height));
        }

        public bool Insert(int width, int height, ChoiceHeuristic method, out Rect rect)
        {
            var newNode = new Rect(0, 0, 0, 0);
            int score1 = 0; // Unused in this function. We don't need to know the score after finding the position.
            int score2 = 0;

            switch (method)
            {
                case ChoiceHeuristic.BestShortSideFit:
                    newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2);
                    break;

                case ChoiceHeuristic.BottomLeftRule:
                    newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2);
                    break;

                case ChoiceHeuristic.ContactPointRule:
                    newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
                    break;

                case ChoiceHeuristic.BestLongSideFit:
                    newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1);
                    break;

                case ChoiceHeuristic.BestAreaFit:
                    newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2);
                    break;
            }

            if (newNode.H == 0)
            {
                rect = Rect.Zero;
                return false;
            }

            int numRectanglesToProcess = _freeRects.Count;
            for (int i = 0; i < numRectanglesToProcess; ++i)
            {
                if (SplitFreeNode(ref _freeRects.GetReferenceAt(i), ref newNode))
                {
                    _freeRects.RemoveAt(i);
                    --i;
                    --numRectanglesToProcess;
                }
            }

            PruneFreeList();
            _usedRects.Add(newNode);
            rect = newNode;
            return true;
        }

        public void Insert(List<Rect> rects, List<Rect> dst, ChoiceHeuristic method)
        {
            dst.Clear();

            while (rects.Count > 0)
            {
                int bestScore1 = int.MaxValue;
                int bestScore2 = int.MaxValue;
                int bestRectIndex = -1;
                var bestNode = new Rect(0, 0, 0, 0);

                for (int i = 0; i < rects.Count; ++i)
                {
                    int score1 = 0;
                    int score2 = 0;
                    Rect newNode = ScoreRect(rects[i].W, rects[i].H, method, ref score1, ref score2);

                    if (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2))
                    {
                        bestScore1 = score1;
                        bestScore2 = score2;
                        bestNode = newNode;
                        bestRectIndex = i;
                    }
                }

                if (bestRectIndex == -1)
                    return;

                PlaceRect(ref bestNode);
                rects.RemoveAt(bestRectIndex);
            }
        }

        void PlaceRect(ref Rect node)
        {
            int numRectanglesToProcess = _freeRects.Count;
            for (int i = 0; i < numRectanglesToProcess; ++i)
            {
                if (SplitFreeNode(ref _freeRects.GetReferenceAt(i), ref node))
                {
                    _freeRects.RemoveAt(i);
                    --i;
                    --numRectanglesToProcess;
                }
            }

            PruneFreeList();
            _usedRects.Add(node);
        }

        Rect ScoreRect(int width, int height, ChoiceHeuristic method, ref int score1, ref int score2)
        {
            var newNode = new Rect(0, 0, 0, 0);
            score1 = int.MaxValue;
            score2 = int.MaxValue;

            switch (method)
            {
                case ChoiceHeuristic.BestShortSideFit:
                    newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2); break;

                case ChoiceHeuristic.BottomLeftRule:
                    newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2); break;

                case ChoiceHeuristic.ContactPointRule:
                    newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
                    score1 = -score1; // Reverse since we are minimizing, but for contact point score bigger is better.
                    break;

                case ChoiceHeuristic.BestLongSideFit:
                    newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1); break;

                case ChoiceHeuristic.BestAreaFit:
                    newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2); break;
            }

            // Cannot fit the current rectangle.
            if (newNode.H == 0)
            {
                score1 = int.MaxValue;
                score2 = int.MaxValue;
            }

            return newNode;
        }

        /// <summary>
        /// Computes the ratio of used surface area.
        /// </summary>
        public float GetOccupancy()
        {
            ulong usedSurfaceArea = 0;
            for (int i = 0; i < _usedRects.Count; ++i)
                usedSurfaceArea += (uint)_usedRects[i].W * (uint)_usedRects[i].H;

            return (float)((double)usedSurfaceArea / (BinWidth * BinHeight));
        }

        Rect FindPositionForNewNodeBottomLeft(int width, int height, ref int bestY, ref int bestX)
        {
            var bestNode = new Rect(0, 0, 0, 0);
            //memset(bestNode, 0, sizeof(Rect));

            bestY = int.MaxValue;

            for (int i = 0; i < _freeRects.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (_freeRects[i].W >= width && _freeRects[i].H >= height)
                {
                    int topSideY = _freeRects[i].Y + height;
                    if (topSideY < bestY || (topSideY == bestY && _freeRects[i].X < bestX))
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = width;
                        bestNode.H = height;
                        bestY = topSideY;
                        bestX = _freeRects[i].X;
                    }
                }
                if (AllowRotation && _freeRects[i].W >= height && _freeRects[i].H >= width)
                {
                    int topSideY = _freeRects[i].Y + width;
                    if (topSideY < bestY || (topSideY == bestY && _freeRects[i].X < bestX))
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = height;
                        bestNode.H = width;
                        bestY = topSideY;
                        bestX = _freeRects[i].X;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestShortSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
        {
            var bestNode = new Rect(0, 0, 0, 0);
            //memset(&bestNode, 0, sizeof(Rect));

            bestShortSideFit = int.MaxValue;

            for (int i = 0; i < _freeRects.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (_freeRects[i].W >= width && _freeRects[i].H >= height)
                {
                    int leftoverHoriz = Math.Abs(_freeRects[i].W - width);
                    int leftoverVert = Math.Abs(_freeRects[i].H - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = width;
                        bestNode.H = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }

                if (AllowRotation && _freeRects[i].W >= height && _freeRects[i].H >= width)
                {
                    int flippedLeftoverHoriz = Math.Abs(_freeRects[i].W - height);
                    int flippedLeftoverVert = Math.Abs(_freeRects[i].H - width);
                    int flippedShortSideFit = Math.Min(flippedLeftoverHoriz, flippedLeftoverVert);
                    int flippedLongSideFit = Math.Max(flippedLeftoverHoriz, flippedLeftoverVert);

                    if (flippedShortSideFit < bestShortSideFit ||
                        (flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit))
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = height;
                        bestNode.H = width;
                        bestShortSideFit = flippedShortSideFit;
                        bestLongSideFit = flippedLongSideFit;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestLongSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
        {
            var bestNode = new Rect(0, 0, 0, 0);
            //memset(&bestNode, 0, sizeof(Rect));

            bestLongSideFit = int.MaxValue;

            for (int i = 0; i < _freeRects.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (_freeRects[i].W >= width && _freeRects[i].H >= height)
                {
                    int leftoverHoriz = Math.Abs(_freeRects[i].W - width);
                    int leftoverVert = Math.Abs(_freeRects[i].H - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = width;
                        bestNode.H = height;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }

                if (AllowRotation && _freeRects[i].W >= height && _freeRects[i].H >= width)
                {
                    int leftoverHoriz = Math.Abs(_freeRects[i].W - height);
                    int leftoverVert = Math.Abs(_freeRects[i].H - width);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                    int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                    if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = height;
                        bestNode.H = width;
                        bestShortSideFit = shortSideFit;
                        bestLongSideFit = longSideFit;
                    }
                }
            }
            return bestNode;
        }

        Rect FindPositionForNewNodeBestAreaFit(int width, int height, ref int bestAreaFit, ref int bestShortSideFit)
        {
            var bestNode = new Rect(0, 0, 0, 0);
            //memset(&bestNode, 0, sizeof(Rect));

            bestAreaFit = int.MaxValue;

            for (int i = 0; i < _freeRects.Count; ++i)
            {
                int areaFit = _freeRects[i].W * _freeRects[i].H - width * height;

                // Try to place the rectangle in upright (non-flipped) orientation.
                if (_freeRects[i].W >= width && _freeRects[i].H >= height)
                {
                    int leftoverHoriz = Math.Abs(_freeRects[i].W - width);
                    int leftoverVert = Math.Abs(_freeRects[i].H - height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = width;
                        bestNode.H = height;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }

                if (AllowRotation && _freeRects[i].W >= height && _freeRects[i].H >= width)
                {
                    int leftoverHoriz = Math.Abs(_freeRects[i].W - height);
                    int leftoverVert = Math.Abs(_freeRects[i].H - width);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = height;
                        bestNode.H = width;
                        bestShortSideFit = shortSideFit;
                        bestAreaFit = areaFit;
                    }
                }
            }
            return bestNode;
        }

        /// Returns 0 if the two intervals i1 and i2 are disjoint, or the length of their overlap otherwise.
        int CommonIntervalLength(int i1start, int i1end, int i2start, int i2end)
        {
            if (i1end < i2start || i2end < i1start)
                return 0;
            return Math.Min(i1end, i2end) - Math.Max(i1start, i2start);
        }

        int ContactPointScoreNode(int x, int y, int width, int height)
        {
            int score = 0;

            if (x == 0 || x + width == BinWidth)
                score += height;
            if (y == 0 || y + height == BinHeight)
                score += width;

            for (int i = 0; i < _usedRects.Count; ++i)
            {
                if (_usedRects[i].X == x + width || _usedRects[i].X + _usedRects[i].W == x)
                    score += CommonIntervalLength(
                        _usedRects[i].Y, _usedRects[i].Y + _usedRects[i].H, y, y + height);

                if (_usedRects[i].Y == y + height || _usedRects[i].Y + _usedRects[i].H == y)
                    score += CommonIntervalLength(
                        _usedRects[i].X, _usedRects[i].X + _usedRects[i].W, x, x + width);
            }
            return score;
        }

        Rect FindPositionForNewNodeContactPoint(int width, int height, ref int bestContactScore)
        {
            var bestNode = new Rect(0, 0, 0, 0);
            //memset(&bestNode, 0, sizeof(Rect));

            bestContactScore = -1;

            for (int i = 0; i < _freeRects.Count; ++i)
            {
                // Try to place the rectangle in upright (non-flipped) orientation.
                if (_freeRects[i].W >= width && _freeRects[i].H >= height)
                {
                    int score = ContactPointScoreNode(_freeRects[i].X, _freeRects[i].Y, width, height);
                    if (score > bestContactScore)
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = width;
                        bestNode.H = height;
                        bestContactScore = score;
                    }
                }
                if (AllowRotation && _freeRects[i].W >= height && _freeRects[i].H >= width)
                {
                    int score = ContactPointScoreNode(_freeRects[i].X, _freeRects[i].Y, height, width);
                    if (score > bestContactScore)
                    {
                        bestNode.X = _freeRects[i].X;
                        bestNode.Y = _freeRects[i].Y;
                        bestNode.W = height;
                        bestNode.H = width;
                        bestContactScore = score;
                    }
                }
            }
            return bestNode;
        }

        bool SplitFreeNode(ref Rect freeNode, ref Rect usedNode)
        {
            // Test with SAT if the rectangles even intersect.
            if (usedNode.X >= freeNode.X + freeNode.W || usedNode.X + usedNode.W <= freeNode.X ||
                usedNode.Y >= freeNode.Y + freeNode.H || usedNode.Y + usedNode.H <= freeNode.Y)
                return false;

            if (usedNode.X < freeNode.X + freeNode.W && usedNode.X + usedNode.W > freeNode.X)
            {
                // New node at the top side of the used node.
                if (usedNode.Y > freeNode.Y && usedNode.Y < freeNode.Y + freeNode.H)
                {
                    Rect newNode = freeNode;
                    newNode.H = usedNode.Y - newNode.Y;
                    _freeRects.Add(newNode);
                }

                // New node at the bottom side of the used node.
                if (usedNode.Y + usedNode.H < freeNode.Y + freeNode.H)
                {
                    Rect newNode = freeNode;
                    newNode.Y = usedNode.Y + usedNode.H;
                    newNode.H = freeNode.Y + freeNode.H - (usedNode.Y + usedNode.H);
                    _freeRects.Add(newNode);
                }
            }

            if (usedNode.Y < freeNode.Y + freeNode.H && usedNode.Y + usedNode.H > freeNode.Y)
            {
                // New node at the left side of the used node.
                if (usedNode.X > freeNode.X && usedNode.X < freeNode.X + freeNode.W)
                {
                    Rect newNode = freeNode;
                    newNode.W = usedNode.X - newNode.X;
                    _freeRects.Add(newNode);
                }

                // New node at the right side of the used node.
                if (usedNode.X + usedNode.W < freeNode.X + freeNode.W)
                {
                    Rect newNode = freeNode;
                    newNode.X = usedNode.X + usedNode.W;
                    newNode.W = freeNode.X + freeNode.W - (usedNode.X + usedNode.W);
                    _freeRects.Add(newNode);
                }
            }

            return true;
        }

        void PruneFreeList()
        {
            for (int i = 0; i < _freeRects.Count; ++i)
            {
                for (int j = i + 1; j < _freeRects.Count; ++j)
                {
                    ref Rect rectI = ref _freeRects.GetReferenceAt(i);
                    ref Rect rectJ = ref _freeRects.GetReferenceAt(j);

                    if (IsContainedIn(ref rectI, ref rectJ))
                    {
                        _freeRects.RemoveAt(i);
                        --i;
                        break;
                    }

                    if (IsContainedIn(ref rectJ, ref rectI))
                    {
                        _freeRects.RemoveAt(j);
                        --j;
                    }
                }
            }
        }

        static bool IsContainedIn(ref Rect a, ref Rect b)
        {
            return a.X >= b.X 
                && a.Y >= b.Y 
                && a.X + a.W <= b.X + b.W 
                && a.Y + a.H <= b.Y + b.H;
        }
    }
}