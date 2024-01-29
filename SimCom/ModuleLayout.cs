using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SimComLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ModuleLayoutContentItem
    {
        [JsonProperty]
        public string path;
        [JsonProperty]
        public uint size;
        [JsonProperty]
        public long date;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ModuleLayout
    {
        [JsonProperty]
        public List<ModuleLayoutContentItem> content = new List<ModuleLayoutContentItem>();
    }
}
