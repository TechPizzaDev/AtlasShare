using System.Collections.Generic;
using System.IO;

namespace AtlasShare
{
    public class AtlasImageBatch
    {
        public AtlasRootDescription Description;
        public DirectoryInfo Directory;
        public List<AtlasImage> Images;
        public List<AtlasImageBatch> SubBatches;

        public AtlasImageBatch(AtlasRootDescription desc, DirectoryInfo directory)
        {
            Description = desc;
            Directory = directory;
            Images = new List<AtlasImage>();

            if (!desc.ForceSingleTexture)
                SubBatches = new List<AtlasImageBatch>();
        }
    }
}
