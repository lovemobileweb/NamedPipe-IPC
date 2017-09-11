
using Newtonsoft.Json;

namespace ObjectIPC
{
    class IpcResponseListGpo
    {
        [JsonProperty]
        private string test { get; set; }

        public void Set()
        {
            test = "bbb";
        }
    }
}
