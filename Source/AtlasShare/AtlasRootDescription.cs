using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AtlasShare
{
    [JsonObject]
    public class AtlasRootDescription
    {
        [JsonProperty, DefaultValue(false)]
        public bool ForceSingleTexture { get; } = false;

        [JsonProperty, DefaultValue(true)]
        public bool IncludeRootName { get; } = true;
        
        [JsonProperty]
        public IReadOnlyList<string> Exclusions { get; }
        
        [JsonConstructor]
        public AtlasRootDescription(
            bool forceSingleTexture, bool includeRootName, IList<string> exclusions)
        {
            ForceSingleTexture = forceSingleTexture;
            IncludeRootName = includeRootName;
            
            var exclusionSource = exclusions ?? Array.Empty<string>();
            Exclusions = new ReadOnlyCollection<string>(exclusionSource);
        }

        public AtlasRootDescription() : this(false, true, null)
        {
        }
    }
}