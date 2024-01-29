using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SimComLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ModuleManifestReleaseNoteItem
    {
        [JsonProperty]
        public string LastUpdate;
        [JsonProperty]
        public string OlderHistory;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ModuleManifestReleaseNotes
    {
        [JsonProperty]
        public ModuleManifestReleaseNoteItem neutral;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ModuleManifest
    {
        [JsonProperty]
        public string[] dependencies;
        [JsonProperty]
        public string name;
        [JsonProperty]
        public string content_type;
        [JsonProperty]
        public string title;
        [JsonProperty]
        public string manufacturer;
        [JsonProperty]
        public string creator;
        [JsonProperty]
        public string package_version;
        [JsonProperty]
        public string minimum_game_version;
        [JsonProperty]
        public ModuleManifestReleaseNotes release_notes;
        public int major_Version;
        public int minor_Version;
        public int revision;
    }
}
