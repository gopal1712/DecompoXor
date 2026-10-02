using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DecompoXor.Application.Utilities;

public static class LlmJsonParser
{
    private static bool IsCompleteJson(string json)
    {
        var expectedClosers = new Stack<char>();
        var inString = false;
        var isEscaped = false;
        var rootStarted = false;

        for (var index = 0; index < json.Length; index++)
        {
            var ch = json[index];
            if (inString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    isEscaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (!rootStarted)
            {
                if (char.IsWhiteSpace(ch))
                {
                    continue;
                }

                if (ch != '{')
                {
                    return false;
                }

                rootStarted = true;
            }

            switch (ch)
            {
                case '"':
                    inString = true;
                    break;
                case '{':
                    expectedClosers.Push('}');
                    break;
                case '[':
                    expectedClosers.Push(']');
                    break;
                case '}':
                case ']':
                    if (expectedClosers.Count == 0 || expectedClosers.Pop() != ch)
                    {
                        return false;
                    }

                    if (expectedClosers.Count == 0 && json[(index + 1)..].Trim().Length > 0)
                    {
                        return false;
                    }
                    break;
            }
        }

        return rootStarted && !inString && expectedClosers.Count == 0;
    }

    private static IEnumerable<(int Start, int End, string Json)> ExtractTopLevelObjects(string text)
    {
        var expectedClosers = new Stack<char>();
        var inString = false;
        var isEscaped = false;
        var objectStart = -1;

        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if (inString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    isEscaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            switch (ch)
            {
                case '{':
                    if (expectedClosers.Count == 0)
                    {
                        objectStart = index;
                    }

                    expectedClosers.Push('}');
                    break;
                case '[':
                    expectedClosers.Push(']');
                    break;
                case '}':
                case ']':
                    if (expectedClosers.Count == 0 || expectedClosers.Pop() != ch)
                    {
                        expectedClosers.Clear();
                        objectStart = -1;
                        break;
                    }

                    if (expectedClosers.Count == 0 && objectStart >= 0)
                    {
                        yield return (objectStart, index + 1, text[objectStart..(index + 1)]);
                        objectStart = -1;
                    }

                    break;
            }
        }
    }

    private static bool TryParseObject(string json, out JObject result)
    {
        try
        {
            var token = JToken.Parse(json);
            if (token is JObject parsedObject)
            {
                result = parsedObject;
                return true;
            }
        }
        catch (JsonException)
        {
        }

        result = new JObject();
        return false;
    }

    public static JObject ParseObject(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new JObject();
        }

        var json = response.Trim();

        var fenceStart = json.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var firstNewLine = json.IndexOf('\n', fenceStart);
            var fenceEnd = json.LastIndexOf("```", StringComparison.Ordinal);

            if (firstNewLine >= 0 && fenceEnd > firstNewLine)
            {
                json = json[(firstNewLine + 1)..fenceEnd].Trim();
            }
        }

        if (IsCompleteJson(json) && TryParseObject(json, out var parsedObject))
        {
            return parsedObject;
        }

        foreach (var candidate in ExtractTopLevelObjects(json))
        {
            var trailingText = json[candidate.End..].TrimStart();
            if (trailingText.Length > 0 && trailingText[0] is ']' or '}' or ',')
            {
                continue;
            }

            if (IsCompleteJson(candidate.Json) && TryParseObject(candidate.Json, out parsedObject))
            {
                return parsedObject;
            }
        }

        return new JObject();
    }
}
