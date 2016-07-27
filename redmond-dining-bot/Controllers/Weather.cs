using msftbot.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace msftbot
{
    internal class WeatherActions
    {
        internal WeatherActions()
        { }

        internal async Task<string> GetWeather()
        {
            // Get JSON – List of all Cafes
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(Constants.weatherRedmond);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            //dynamic obj = JArray.Parse(responseBody);

            var converter = new ExpandoObjectConverter();
            dynamic message = JsonConvert.DeserializeObject<ExpandoObject>(responseBody, converter);

            return "hold";

        }
    }
}