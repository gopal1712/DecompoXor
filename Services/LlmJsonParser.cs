using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StoryDecomposer.Services;

internal static class LlmJsonParser
{
    public static JObject ParseObject(string response)
    {
        var json = response.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = json.IndexOf('\n');
            var lastFence = json.LastIndexOf("```");
            if (firstLineEnd >= 0 && lastFence > firstLineEnd)
            {
                json = json[(firstLineEnd + 1)..lastFence].Trim();
            }
        }

        var objectStart = json.IndexOf('{');
        var objectEnd = json.LastIndexOf('}');
        if (objectStart >= 0 && objectEnd > objectStart)
        {
            json = json[objectStart..(objectEnd + 1)];
        }

        if (json.Contains("\\\"", StringComparison.Ordinal))
        {
            json = json.Replace("\\\"", "\"");
        }

        return JsonConvert.DeserializeObject<JObject>(json) ?? new JObject();
    }
}