using GeneralShare;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace AtlasShare
{
    public class AtlasRootRetreiver
    {
        private DirectoryInfo _directory;
        private Configuration _config;
        private HashSet<string> _exclusionSet;

        public AtlasRootRetreiver(DirectoryInfo directory, Configuration config)
        {
            _directory = directory;
            _config = config;
            _exclusionSet = new HashSet<string>(StringComparer.Ordinal);
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
                _exclusionSet.Add(PathHelper.GetNormalizedPath(exclusion));
            
            var dir = directory.Directory;
            var batch = CreateBatch(directory.Description, dir, dir);

            _exclusionSet.Clear();
            return batch;
        }

        private AtlasImageBatch CreateBatch(AtlasRootDescription desc, DirectoryInfo origin, DirectoryInfo dir)
        {
            var batch = new AtlasImageBatch(desc, dir);
            foreach (var file in dir.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                string relativeFilePath = PathHelper.GetNormalizedPath(PathHelper.GetRelativePath(origin, file));
                if (_exclusionSet.Contains(relativeFilePath))
                    continue;

                var imgInfo = Image.Identify(_config, file.OpenRead());
                if (imgInfo != null)
                {
                    string relativeImgPath = PathHelper.GetNormalizedPath(PathHelper.GetRelativePath(_directory, file));
                    relativeImgPath = Path.ChangeExtension(relativeImgPath, null);

                    if (!desc.IncludeRootName)
                    {
                        int firstSeparator = relativeImgPath.IndexOf('/');
                        if (firstSeparator != -1)
                            relativeImgPath = relativeImgPath.Substring(firstSeparator + 1);
                    }
                    batch.Images.Add(new AtlasImage(file, relativeImgPath, imgInfo.Width, imgInfo.Height));
                }
            }

            foreach (var subDir in dir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                string relativeSubDirPath = PathHelper.GetNormalizedPath(PathHelper.GetRelativePath(origin, subDir));
                if (_exclusionSet.Contains(relativeSubDirPath))
                    continue;

                var subBatch = CreateBatch(desc, origin, subDir);
                batch.SubBatches.Add(subBatch);
            }
            return batch;
        }
    }
}