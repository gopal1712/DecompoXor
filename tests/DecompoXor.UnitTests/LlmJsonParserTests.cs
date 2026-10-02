using DecompoXor.Application.Utilities;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DecompoXor.UnitTests;

public class LlmJsonParserTests
{
    [Fact]
    public void ParseObject_MalformedJson_ReturnsEmptyObject()
    {
        // JSON where a task description contains an unexpected character (e.g., stray 'E')
        // Use a verbatim string with doubled quotes for JSON literals
        // Intentionally malformed JSON: missing the final closing brace
        string malformed = @"{ ""tasks"": [ { ""description"": ""Task 1"" }, { ""description"": ""Task 2"" } ]";

        JObject result = LlmJsonParser.ParseObject(malformed);
        Assert.NotNull(result);
        // Our parser returns an empty object on JsonReaderException
        Assert.Empty(result);
    }

    [Fact]
    public void ParseObject_JsonWrappedInTextOrMarkdown_ReturnsObject()
    {
        var input = "Here is the result:\n```json\n{\"tasks\":[{\"title\":\"Task 1\",\"description\":\"Do work\",\"areaOfChange\":\"API\"}],\"questions\":[\"Need more info?\"],\"reasoning\":[\"Because it matters\"],\"estimatedTotalStoryPoints\":5}\n```";

        var result = LlmJsonParser.ParseObject(input);

        Assert.NotNull(result);
        Assert.Equal("Task 1", result["tasks"]![0]!["title"]!.Value<string>());
        Assert.Equal(5, result["estimatedTotalStoryPoints"]!.Value<int>());
    }

    [Fact]
    public void ParseObject_JsonPrefixedByTextWithBracesInString_ReturnsObject()
    {
        var input = "Result: {\"tasks\":[{\"description\":\"Literal { and } braces with an escaped \\\"quote\\\"\"}]}";

        var result = LlmJsonParser.ParseObject(input);

        Assert.Equal("Literal { and } braces with an escaped \"quote\"", result["tasks"]![0]!["description"]!.Value<string>());
    }

    [Fact]
    public void ParseObject_MismatchedDelimiters_ReturnsEmptyObject()
    {
        var input = "{\"tasks\":[{\"title\":\"Task\"}]}]";

        var result = LlmJsonParser.ParseObject(input);

        Assert.Empty(result);
    }
}
