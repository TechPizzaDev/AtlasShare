using GeneralShare;
using MonoGame.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AtlasShare
{
    public class AtlasSerializer
    {
        public delegate void TextureDelegate(IntPtr data, int imgWidth, int imgHeight, int index);

        public ImageSaveFormat SaveFormat { get; }

        public AtlasSerializer(ImageSaveFormat saveFormat)
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

            IntPtr data = IntPtr.Zero;
            try
            {
                if (!output.Exists)
                    output.Create();

                for (int texture = 0; texture < stateCount; texture++)
                {
                    AtlasPackerState state = packer._states[texture];
                    int width = state.Width;
                    int height = state.Height;
                    int pixels = width * height;

                    data = Marshal.AllocHGlobal(pixels * sizeof(Rgba32));
                    var ptr = (Rgba32*)data;
                    for (int i = 0; i < pixels; i++)
                        ptr[i] = default;

                    foreach (var item in state.Items)
                    {
                        using (Image img = item.AtlasImage.Image)
                        {
                            var srcRect = new Rect(0, 0, img.Width, img.Height);
                            switch (img.PixelFormat)
                            {
                                case ImagePixelFormat.Rgb:
                                    var input24 = (Rgb24*)img.GetPointer();
                                    Copy(input24, inputStride: srcRect.W, srcRect,
                                         ptr, outputStride: width, item.Rect);
                                    break;

                                case ImagePixelFormat.RgbWithAlpha:
                                    var input32 = (Rgba32*)img.GetPointer();
                                    Copy(input32, inputStride: srcRect.W, srcRect,
                                         ptr, outputStride: width, item.Rect);
                                    break;

                                default:
                                    throw new InvalidDataException(
                                        $"{img.PixelFormat} is unsupported, only 3 or 4 channels are supported.");
                            }

                            AddItem(new AtlasData.Item(item.AtlasImage.Tag, texture, item.Rect));
                        }
                    }

                    using (var img = new Image(data, width, height, ImagePixelFormat.RgbWithAlpha))
                    using (var fs = GetFileStream(textures, texture, output))
                        img.Save(fs, SaveFormat);

                    onTexture?.Invoke(data, width, height, texture);
                }

                for (int i = 0; i < singleCount; i++)
                {
                    using (AtlasImage item = packer._singles[i])
                    {
                        int index = i + stateCount; // add amount of states as offset
                        using (var fs = GetFileStream(textures, index, output))
                            item.Image.Save(fs, SaveFormat);

                        AddItem(new AtlasData.Item(item.Tag, index, 0, 0, item.Width, item.Height));
                        onTexture?.Invoke(item.Image.GetPointer(), item.Width, item.Height, index);
                    }
                }

                return new AtlasData(textures, items);
            }
            catch
            {
                foreach (var state in packer._states)
                    foreach (var item in state.Items)
                        item.AtlasImage.Dispose();

                foreach (var item in packer._singles)
                    item.Dispose();

                throw;
            }
            finally
            {
                if (data != IntPtr.Zero)
                    Marshal.FreeHGlobal(data);
            }
        }

        private FileStream GetFileStream(string[] textures, int index, DirectoryInfo output)
        {
            string extension = SaveFormat.ToString().ToLower();
            textures[index] = $"texture_{index}.{extension}";
            return new FileStream(Path.Combine(output.FullName, textures[index]), FileMode.Create);
        }

        public static unsafe void Copy(
            Rgba32* input, int inputStride, Rect src,
            Rgba32* output, int outputStride, Rect dst)
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

        private static unsafe void Copy(
            Rgb24* input, int inputStride, Rect src,
            Rgba32* output, int outputStride, Rect dst)
        {
            for (int y = src.Y; y < src.H; y++)
            {
                for (int x = src.X; x < src.W; x++)
                {
                    int outputIndex = x + dst.X + (y + dst.Y) * outputStride;
                    ref Rgb24 srcItem = ref input[x + y * inputStride];
                    output[outputIndex] = new Rgba32(srcItem.R, srcItem.G, srcItem.B, 255);
                }
            }
        }
    }
}