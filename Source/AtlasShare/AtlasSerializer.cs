using GeneralShare;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AtlasShare
{
    public class AtlasSerializer
    {
        public delegate void TextureDelegate(Span<Rgba32> data, Size size, int index);

        public IImageFormat SaveFormat { get; }
        public string FileExtension { get; }

        public AtlasSerializer(IImageFormat saveFormat)
        {
            SaveFormat = saveFormat;
            FileExtension = SaveFormat.FileExtensions.First();
        }

        public unsafe AtlasData Serialize(
            AtlasPacker packer, DirectoryInfo output,
            TextureDelegate onTexture, ProgressDelegate onProgress)
        {
            int stateCount = packer._states.Count;
            int singleCount = packer._singles.Count;
            var textures = new string[stateCount + singleCount];

            int totalItemCount = packer.TotalItemCount;
            var items = new List<AtlasData.Item>(totalItemCount);

            if (!output.Exists)
                output.Create();

            void AddItem(AtlasData.Item item)
            {
                items.Add(item);
                onProgress.Invoke(items.Count / (float)totalItemCount);
            }

            for (int textureIndex = 0; textureIndex < stateCount; textureIndex++)
            {
                AtlasPackerState state = packer._states[textureIndex];
                int width = state.Width;
                int height = state.Height;

                using (var result = new Image<Rgba32>(width, height))
                {
                    Span<Rgba32> resultSpan = result.GetPixelSpan();
                    foreach (AtlasPackerState.Item item in state.Items)
                    {
                        using (var img = Image.Load<Rgba32>(item.AtlasImage.File.OpenRead()))
                        {
                            var srcRect = new Rect(0, 0, img.Width, img.Height);
                            var input32 = img.GetPixelSpan();

                            Copy(input32, inputStride: srcRect.W, srcRect,
                                resultSpan, outputStride: width, item.Rect);

                            AddItem(new AtlasData.Item(item.AtlasImage.RelativePath, textureIndex, item.Rect));
                        }
                    }

                    using (var fs = GetFileStream(textures, textureIndex, output))
                        result.Save(fs, SaveFormat);

                    onTexture?.Invoke(resultSpan, new Size(width, height), textureIndex);
                }
            }

            for (int singleIndex = 0; singleIndex < singleCount; singleIndex++)
            {
                AtlasImage item = packer._singles[singleIndex];
                using (var img = Image.Load<Rgba32>(item.File.OpenRead()))
                {
                    int index = singleIndex + stateCount; // add amount of states as offset
                    using (var fs = GetFileStream(textures, index, output))
                        img.Save(fs, SaveFormat);

                    AddItem(new AtlasData.Item(item.RelativePath, index, 0, 0, item.Width, item.Height));
                    onTexture?.Invoke(img.GetPixelSpan(), new Size(item.Width, item.Height), index);
                }
            }

            return new AtlasData(textures, items);
        }

        private FileStream GetFileStream(string[] textures, int index, DirectoryInfo output)
        {
            textures[index] = string.Join("texture_", index.ToString(), FileExtension);
            return new FileStream(Path.Combine(output.FullName, textures[index]), FileMode.Create);
        }

        public static unsafe void Copy(
            Span<Rgba32> input, int inputStride, Rect src,
            Span<Rgba32> output, int outputStride, Rect dst)
        {
            for (int y = src.Y; y < src.H; y++)
            {
                for (int x = src.X; x < src.W; x++)
                {
                    int outputIndex = x + dst.X + (y + dst.Y) * outputStride;
                    output[outputIndex] = input[x + y * inputStride];
                }
            }
        }
    }
}