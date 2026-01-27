using System.Text.Json;

namespace Mafrecal.WebDashboard.Helpers
{
    public static class JsonHelper
    {
        public static Dictionary<string, object> DeserializeToDictionary(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, object>();

            using var doc = JsonDocument.Parse(json);
            return ParseElement(doc.RootElement);
        }

        private static Dictionary<string, object> ParseElement(JsonElement element)
        {
            var dict = new Dictionary<string, object>();

            foreach (var prop in element.EnumerateObject())
            {
                switch (prop.Value.ValueKind)
                {
                    case JsonValueKind.Object:
                        dict[prop.Name] = ParseElement(prop.Value);
                        break;
                    case JsonValueKind.Array:
                        dict[prop.Name] = ParseArray(prop.Value);
                        break;
                    default:
                        dict[prop.Name] = prop.Value.ToString();
                        break;
                }
            }

            return dict;
        }

        private static List<object> ParseArray(JsonElement array)
        {
            var list = new List<object>();
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                    list.Add(ParseElement(item));
                else if (item.ValueKind == JsonValueKind.Array)
                    list.Add(ParseArray(item));
                else
                    list.Add(item.ToString());
            }
            return list;
        }
    }

}
