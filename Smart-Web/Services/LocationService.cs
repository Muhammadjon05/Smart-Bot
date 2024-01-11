using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smart_Bot.Resources;

namespace Smart_Bot.Services;

public class LocationService
{
    public async Task<Components> GetLocationName(double latitude, double longitude)
    {
        using (var httpClient = new HttpClient())
        {
            var apiKey = "78c13aa2c9094e438095605552034373";
            var apiUrl =
                $"https://api.opencagedata.com/geocode/v1/json?q={latitude:0.#########}+{longitude:0.#########}&key={apiKey}";

            var response = await httpClient.GetStringAsync(apiUrl);
            JObject jObject = JsonConvert.DeserializeObject<JObject>(response);
            JObject components = jObject?.SelectToken("results[0].components") as JObject;

            // Convert the JObject components into a strongly-typed object (Components class)
            Components resultComponents = components != null
                ? new Components
                {
                    city = components.Value<string>("city"),
                    country = components.Value<string>("country"),
                    county = components.Value<string>("county"),
                    district = components.Value<string>("district"),
                    state = components.Value<string>("state")
                }
                : new Components();
            return resultComponents;
            
        }
    }
}