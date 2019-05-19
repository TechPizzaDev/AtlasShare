using System.Collections.Generic;

namespace AtlasShare
{
    public class AtlasPacker
    {
        internal List<AtlasImage> _singles;
        internal List<AtlasPackerState> _states;

        public int MaxSize { get; }
        public ImageSpacing Spacing { get; }

        public int PackCount => _states.Count + _singles.Count;
        
        public int TotalItemCount
        {
            get
            {
                int count = _singles.Count;
                foreach (var state in _states)
                    count += state.Items.Count;
                return count;
            }
        }

        public AtlasPacker(int maxSize, ImageSpacing spacing)
        {
            Spacing = spacing;

            if (maxSize < 256)
                MaxSize = 256;
            else if (maxSize > 16384)
                MaxSize = 16384;
            else
                MaxSize = maxSize;

            _singles = new List<AtlasImage>();
            _states = new List<AtlasPackerState>
            {
                new AtlasPackerState(maxSize, Spacing, false)
            };
        }

        public void TrimStates()
        {
            foreach (var state in _states)
                state.Trim();
        }

        public void PackBatch(AtlasImageBatch batch)
        {
            PackBatchInternal(batch);
            if (batch.SubBatches != null)
            {
                foreach (var subBatch in batch.SubBatches)
                    PackBatchInternal(subBatch);
            }
        }
        
        private void PackBatchInternal(AtlasImageBatch batch)
        {
            foreach(var img in batch.Images)
            {
                if (img.Width > MaxSize || img.Height > MaxSize)
                {
                    _singles.Add(img);
                    continue;
                }
                
                int index = 0;

                TryInsert:
                AtlasPackerState state = _states[index];
                if (!state.Insert(img, out Rect rect))
                {
                    if (!state.Step())
                    {
                        index++;
                        while (index < _states.Count && _states[index].IsSingle)
                            index++;

                        if (index == _states.Count)
                            _states.Add(new AtlasPackerState(MaxSize, Spacing, false));
                    }

                    goto TryInsert;
                }

                state.Items.Add(new AtlasPackerState.Item(rect, img));
            }
        }

    }
}
