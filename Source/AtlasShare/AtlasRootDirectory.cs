using System.IO;

namespace AtlasShare
{
    public class AtlasRootDirectory
    {
        public AtlasRootDescription Description { get; }
        public DirectoryInfo Directory { get; }
        
        public AtlasRootDirectory(AtlasRootDescription description, DirectoryInfo directory)
        {
            Description = description;
            Directory = directory;
        }
    }
}
