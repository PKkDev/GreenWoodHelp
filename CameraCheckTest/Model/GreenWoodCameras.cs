using Newtonsoft.Json;

namespace CameraCheckTest.Model
{
    public class GreenWoodCameras
    {
        [JsonProperty("cameras")]
        public List<CameraData> Cameras;

        public CameraData? this[string id]
        {
            get => Cameras.FirstOrDefault(x => x.Id == id);
        }
    }

    public class CameraData
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("url")]
        public string Url;
    }
}
