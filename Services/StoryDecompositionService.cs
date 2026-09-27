using Newtonsoft.Json.Linq;
using StoryDecomposer.Models;

namespace StoryDecomposer.Services;

public class StoryDecompositionService
{
    private const string PromptFileName = "story-analysis-prompt.txt";
    private readonly ILLMService _llmService;
    private readonly RAGService _ragService;
    
    public StoryDecompositionService(ILLMService llmService, RAGService ragService)
    {
        _llmService = llmService;
        _ragService = ragService;
    }
    
    public async System.Threading.Tasks.Task<DecompositionResult> DecomposeStory(Story story)
    {
        // Get context from similar acceptance criteria in RAG
        var context = await _ragService.GetContext(story.AcceptanceCriteria);
        
        var prompt = await LoadPrompt(PromptFileName, story.AcceptanceCriteria, context);
        
        var response = await _llmService.Generate(prompt, context);
        
        // Parse JSON response
        var result = LlmJsonParser.ParseObject(response);
        
        // Convert to DecompositionResult
        var tasks = ConvertToTasks(result["tasks"]);
        var questions = ConvertToQuestions(result["questions"]);
        var reasoning = ConvertToReasoning(result["reasoning"]);
        var estimate = ParseEstimate(result["estimatedTotalStoryPoints"]);

       // ValidateAnalysis(tasks, questions, reasoning, estimate);

        return new DecompositionResult
        {
            Tasks = tasks,
            Questions = questions,
            Reasoning = reasoning,
            EstimatedTotalStoryPoints = estimate
        };
    }

    private static void ValidateAnalysis(
        List<StoryDecomposer.Models.Task> tasks,
        List<string> questions,
        List<string> reasoning,
        int estimate)
    {
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

        if (questions.Count is < 5 or > 7 ||
            questions.Any(question => question.Contains("Question 1", StringComparison.OrdinalIgnoreCase)) ||
            reasoning.Count == 0 ||
            reasoning.Any(item => item.Contains("reasoning1", StringComparison.OrdinalIgnoreCase)) ||
            estimate is not (1 or 3 or 5 or 8 or 13 ))
        {
            throw new InvalidOperationException("The LLM returned an incomplete story analysis.");
        }
    }

    private static List<string> ConvertToQuestions(JToken? questionsToken)
    {
        var questions = new List<string>();
        foreach (var questionToken in questionsToken as JArray ?? [])
        {
            var question = questionToken.Type switch
            {
                JTokenType.String => questionToken.Value<string>(),
                JTokenType.Object => questionToken.Value<string>("question")
                    ?? questionToken.Value<string>("text")
                    ?? questionToken.Value<string>("title")
                    ?? questionToken.Value<string>("description")
                    ?? questionToken.Value<string>("desc"),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(question))
            {
                questions.Add(question);
            }
        }

        return questions;
    }

    private static int ParseEstimate(JToken? estimateToken)
    {
        return estimateToken?.Type switch
        {
            JTokenType.Integer => estimateToken.Value<int>(),
            JTokenType.String when int.TryParse(estimateToken.Value<string>(), out var estimate) => estimate,
            _ => 0
        };
    }

    private static List<string> ConvertToReasoning(JToken? reasoningToken)
    {
        if (reasoningToken is JObject reasoningObject)
        {
            return reasoningObject.Properties()
                .Select(property => property.Value.Type == JTokenType.String
                    ? property.Value.Value<string>()
                    : property.Value.ToString(Newtonsoft.Json.Formatting.None))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList()!;
        }

        if (reasoningToken is JArray reasoningArray)
        {
            return reasoningArray
                .Select(item => item.Type == JTokenType.String
                    ? item.Value<string>()
                    : item is JObject reasoningItem
                        ? reasoningItem.Value<string>("reasoning")
                            ?? reasoningItem.Value<string>("title")
                            ?? reasoningItem.Value<string>("description")
                            ?? reasoningItem.Value<string>("desc")
                        : null)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList()!;
        }

        var reasoning = reasoningToken?.Value<string>();
        return string.IsNullOrWhiteSpace(reasoning) ? [] : [reasoning];
    }

    private static async System.Threading.Tasks.Task<string> LoadPrompt(
        string fileName,
        string acceptanceCriteria,
        string context)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "prompts", fileName);
        var template = await File.ReadAllTextAsync(path);
        return template
            .Replace("{acceptanceCriteria}", acceptanceCriteria)
            .Replace("{context}", context);
    }
    
    private List<StoryDecomposer.Models.Task> ConvertToTasks(JToken? tasksArray)
    {
        var tasks = new List<StoryDecomposer.Models.Task>();
        foreach (var taskToken in tasksArray as JArray ?? new JArray())
        {
            var taskJson = taskToken as JObject
                ?? (taskToken as JArray)?.OfType<JObject>().FirstOrDefault();

            if (taskJson is null)
            {
                continue;
            }

            tasks.Add(new StoryDecomposer.Models.Task
            {
                Title = taskJson.Value<string>("title") ?? string.Empty,
                Description = taskJson.Value<string>("description")
                    ?? taskJson.Value<string>("desc")
                    ?? string.Empty,
                AreaOfChange = taskJson.Value<string>("areaOfChange") ?? string.Empty
            });
        }
        return tasks;
    }
}