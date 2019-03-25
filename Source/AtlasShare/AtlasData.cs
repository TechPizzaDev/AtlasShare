using Newtonsoft.Json;
using System.Collections.Generic;

namespace AtlasShare
{
    [JsonObject]
    public class AtlasData
    {
        public string[] Textures { get; }
        public IReadOnlyList<Item> Items { get; }

        [JsonConstructor]
        public AtlasData(string[] textures, List<Item> items)
        {
            Textures = textures;
            Items = items.AsReadOnly();
        }

        [JsonObject]
        public class Item
        {
            [JsonProperty] public string Key { get; }
            [JsonProperty("T")] public int Texture { get; }

            [JsonProperty] public int X { get; }
            [JsonProperty] public int Y { get; }
            [JsonProperty("W")] public int Width { get; }
            [JsonProperty("H")] public int Height { get; }

            [JsonConstructor]
            public Item(
                string key, int texture,
                int x, int y, int width, int height)
            {
                Key = key;
                Texture = texture;

                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public Item(string key, int texture, Rect rect) :
                this(key, texture, rect.X, rect.Y, rect.W, rect.H)
            {
            }
        }
    }
}