namespace DecompoXor.Application.Features.StoryDecomposition;

using DecompoXor.Domain.Entities;
using DecompoXor.Infrastructure.Abstractions.AI;
using DecompoXor.Infrastructure.Abstractions.RAG;
using Newtonsoft.Json.Linq;
using DecompoXor.Application.Utilities;

public class StoryDecompositionService
{
    private const string PromptFileName = "story-analysis-prompt.txt";
    private readonly IChatCompletionService _chatService;
    private readonly IRagService _ragService;

    public StoryDecompositionService(IChatCompletionService chatService, IRagService ragService)
    {
        _chatService = chatService;
        _ragService = ragService;
    }

    public async Task<DecompositionResult> DecomposeStory(Story story)
    {
        var context = await _ragService.GetContext(story.AcceptanceCriteria);
        var prompt = await LoadPrompt(PromptFileName, story.AcceptanceCriteria, context);
        var response = await _chatService.GenerateAsync(prompt);
        var result = LlmJsonParser.ParseObject(response);
        // Convert the raw JSON tasks directly into the domain model type.
        var tasks = ConvertToTasks(result["tasks"]);
        var questions = ConvertToQuestions(result["questions"]);
        var reasoning = ConvertToReasoning(result["reasoning"]);
        var estimate = ParseEstimate(result["estimatedTotalStoryPoints"]);

        // Validate the analysis result using the same business rules as the original implementation.
        ValidateAnalysis(tasks, questions, reasoning, estimate);

        return new DecompositionResult
        {
            Tasks = tasks,
            Questions = questions,
            Reasoning = reasoning,
            EstimatedTotalStoryPoints = estimate
        };
    }

    // The helper methods are copied verbatim from the original implementation.
    private static async Task<string> LoadPrompt(string fileName, string acceptanceCriteria, string context)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "prompts", fileName);
        var template = await File.ReadAllTextAsync(path);
        return template.Replace("{acceptanceCriteria}", acceptanceCriteria).Replace("{context}", context);
    }

    private static List<DomainTask> ConvertToTasks(JToken? tasksArray)
    {
        var tasks = new List<DomainTask>();
        foreach (var taskToken in tasksArray as JArray ?? new JArray())
        {
            var taskJson = taskToken as JObject ?? (taskToken as JArray)?.OfType<JObject>().FirstOrDefault();
            if (taskJson is null) continue;
            tasks.Add(new DomainTask
            {
                Title = taskJson.Value<string>("title") ?? string.Empty,
                Description = taskJson.Value<string>("description") ?? taskJson.Value<string>("desc") ?? string.Empty,
                AreaOfChange = taskJson.Value<string>("areaOfChange") ?? string.Empty
            });
        }
        return tasks;
    }

    private static List<string> ConvertToQuestions(JToken? questionsToken)
    {
        var questions = new List<string>();
        foreach (var q in questionsToken as JArray ?? [])
        {
            string? question = q.Type switch
            {
                JTokenType.String => q.Value<string>(),
                JTokenType.Object => q.Value<string>("question") ?? q.Value<string>("text") ?? q.Value<string>("title") ?? q.Value<string>("description") ?? q.Value<string>("desc"),
                _ => null
            };
            if (!string.IsNullOrWhiteSpace(question)) questions.Add(question);
        }
        return questions;
    }

    private static int ParseEstimate(JToken? estimateToken)
    {
        return estimateToken?.Type switch
        {
            JTokenType.Integer => estimateToken.Value<int>(),
            JTokenType.String when int.TryParse(estimateToken.Value<string>(), out var est) => est,
            _ => 0
        };
    }

    private static List<string> ConvertToReasoning(JToken? reasoningToken)
    {
        if (reasoningToken is JObject obj)
        {
            return obj.Properties()
                .Select(p => p.Value.Type == JTokenType.String ? p.Value.Value<string>() : p.Value.ToString(Newtonsoft.Json.Formatting.None))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
        }
        if (reasoningToken is JArray arr)
        {
            return arr.Select(i => i.Type == JTokenType.String ? i.Value<string>() : i.ToString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
        }
        var str = reasoningToken?.Value<string>();
        return string.IsNullOrWhiteSpace(str) ? [] : [str];
    }

    // Replicates the validation logic from the original StoryDecomposer project.
    private static void ValidateAnalysis(
        List<DomainTask> tasks,
        List<string> questions,
        List<string> reasoning,
        int estimate)
    {
        // Task count and content validation
        if (tasks.Count is < 4 or > 7 || tasks.Any(task =>
                string.IsNullOrWhiteSpace(task.Title) ||
                string.IsNullOrWhiteSpace(task.Description) ||
                task.Title.Contains("Task name", StringComparison.OrdinalIgnoreCase) ||
                task.Description.Contains("Detailed description", StringComparison.OrdinalIgnoreCase) ||
                task.AreaOfChange.Contains('|') ||
                task.AreaOfChange.Contains(',') ||
                task.AreaOfChange.Contains(" or ", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The LLM returned an invalid task breakdown.");
        }

        // Questions and reasoning validation
        if (questions.Count is < 5 or > 7 ||
            questions.Any(q => q.Contains("Question 1", StringComparison.OrdinalIgnoreCase)) ||
            reasoning.Count == 0 ||
            reasoning.Any(r => r.Contains("reasoning1", StringComparison.OrdinalIgnoreCase)) ||
            estimate is not (1 or 3 or 5 or 8 or 13))
        {
            throw new InvalidOperationException("The LLM returned an incomplete story analysis.");
        }
    }
}
