using GeneralShare;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;

namespace AtlasShare
{
    public class AtlasSerializer
    {
        public delegate void TextureDelegate(Span<Rgba32> data, int imgWidth, int imgHeight, int index);

        public IImageFormat SaveFormat { get; }

        public AtlasSerializer(IImageFormat saveFormat)
        {
            SaveFormat = saveFormat;
        }

        public unsafe AtlasData Serialize(
            AtlasPacker packer, DirectoryInfo output,
            TextureDelegate onTexture, ProgressDelegate onProgress)
        {
            int stateCount = packer._states.Count;
            int singleCount = packer._singles.Count;
            var textures = new string[stateCount + singleCount];

            int totalItems = packer._singles.Count;
            foreach (var state in packer._states)
                totalItems += state.Items.Count;

            var items = new List<AtlasData.Item>(totalItems);

            void AddItem(AtlasData.Item item)
            {
                items.Add(item);
                onProgress.Invoke(items.Count / (float)totalItems);
            }

            try
            {
                if (!output.Exists)
                    output.Create();

                for (int texture = 0; texture < stateCount; texture++)
                {
                    AtlasPackerState state = packer._states[texture];
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
                                switch (img.PixelType.BitsPerPixel)
                                {
                                    case 32:
                                        var input32 = img.GetPixelSpan();
                                        Copy(
                                            input32, inputStride: srcRect.W, srcRect,
                                            resultSpan, outputStride: width, item.Rect);
                                        break;

                                    default:
                                        throw new InvalidDataException(
                                            $"{img.PixelType} is unsupported, only 4 channels are supported.");
                                }

                                AddItem(new AtlasData.Item(item.AtlasImage.Tag, texture, item.Rect));
                            }
                        }

                        using (var fs = GetFileStream(textures, texture, output))
                            result.Save(fs, SaveFormat);

                        onTexture?.Invoke(resultSpan, width, height, texture);
                    }
                }

                for (int i = 0; i < singleCount; i++)
                {
                    var item = packer._singles[i];
                    using (var img = Image.Load<Rgba32>(item.File.OpenRead()))
                    {
                        int index = i + stateCount; // add amount of states as offset
                        using (var fs = GetFileStream(textures, index, output))
                            img.Save(fs, SaveFormat);

                        AddItem(new AtlasData.Item(item.Tag, index, 0, 0, item.Width, item.Height));
                        onTexture?.Invoke(img.GetPixelSpan(), item.Width, item.Height, index);
                    }
                }

                return new AtlasData(textures, items);
            }
            catch
            {
                throw;
            }
        }

        private FileStream GetFileStream(string[] textures, int index, DirectoryInfo output)
        {
            string extension = SaveFormat.ToString().ToLower();
            textures[index] = $"texture_{index}.{extension}";
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