using GeneralShare;
using MonoGame.Imaging;
using System;
using System.Collections.Generic;
using System.IO;

namespace AtlasShare
{
    public class AtlasRootRetreiver
    {
        private static readonly HashSet<string> _supportedImgExtensions;

        static AtlasRootRetreiver()
        {
            _supportedImgExtensions = new HashSet<string>(5, StringComparer.OrdinalIgnoreCase)
            {
                ".jpeg", ".jpg", ".tga", ".bmp", ".png"
            };
        }

        private DirectoryInfo _directory;
        private HashSet<string> _exclusionCache;

        public AtlasRootRetreiver(DirectoryInfo directory)
        {
            _directory = directory;
            _exclusionCache = new HashSet<string>(StringComparer.Ordinal);
        }

        public List<AtlasRootDirectory> GetDirectories()
        {
            var list = new List<AtlasRootDirectory>();
            foreach (var file in _directory.GetFiles("*.json", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(file.FullName);
                var matchingDirectory = new DirectoryInfo(Path.Combine(_directory.FullName, name));
                if (matchingDirectory.Exists)
                {
                    var description = JsonUtils.Deserialize<AtlasRootDescription>(file);
                    list.Add(new AtlasRootDirectory(description, matchingDirectory));
                }
            }
            return list;
        }

        public AtlasImageBatch GetImageBatch(AtlasRootDirectory directory)
        {
            foreach (var exclusion in directory.Description.Exclusions)
                _exclusionCache.Add(PathHelper.GetNormalizedPath(exclusion));
            
            var dir = directory.Directory;
            var batch = CreateBatch(directory.Description, dir, dir);

            _exclusionCache.Clear();
            return batch;
        }

        private AtlasImageBatch CreateBatch(AtlasRootDescription desc, DirectoryInfo origin, DirectoryInfo dir)
        {
            var batch = new AtlasImageBatch(desc, dir);
            foreach (var file in dir.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                string relativeFilePath = PathHelper.GetNormalizedPath(PathHelper.GetRelativePath(origin, file));
                if (_exclusionCache.Remove(relativeFilePath) ||
                    !_supportedImgExtensions.Contains(file.Extension))
                    continue;

                var img = new Image(file.OpenRead());
                if (img.Info != null && img.Info.IsValid())
                {
                    string relativeImgPath = PathHelper.GetNormalizedPath(PathHelper.GetRelativePath(_directory, file));
                    relativeImgPath = Path.ChangeExtension(relativeImgPath, null);

                    if (!desc.IncludeRootName)
                    {
                        int firstSlash = relativeImgPath.IndexOf('/');
                        if(firstSlash != -1)
                            relativeImgPath = relativeImgPath.Substring(firstSlash + 1);
                    }
                    
                    batch.Images.Add(new AtlasImage(img, relativeImgPath));
                }
                else
                    img.Dispose();
            }

            foreach (var subDir in dir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                string relativeSubDirPath = PathHelper.GetNormalizedPath(PathHelper.GetRelativePath(origin, subDir));
                if (_exclusionCache.Remove(relativeSubDirPath))
                    continue;

                var subBatch = CreateBatch(desc, origin, subDir);
                batch.SubBatches.Add(subBatch);
            }
            return batch;
        }
    }
}