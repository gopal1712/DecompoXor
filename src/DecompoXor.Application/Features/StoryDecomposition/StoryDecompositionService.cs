namespace DecompoXor.Application.Features.StoryDecomposition;

using DecompoXor.Domain.Entities;
using DecompoXor.Infrastructure.Abstractions.AI;
using DecompoXor.Infrastructure.Abstractions.RAG;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DecompoXor.Application.Utilities;
using System.Collections.Generic;
using System.Linq;

public class StoryDecompositionService
{
    private const string PromptFileName = "story-analysis-prompt.txt";
    private static readonly HashSet<string> ValidTaskAreas = new(StringComparer.OrdinalIgnoreCase)
    {
        "UI",
        "API",
        "Database",
        "Authentication",
        "DevOps",
        "Testing",
        "Documentation",
        "Other"
    };

    private readonly IChatCompletionService _chatService;
    private readonly IRagService _ragService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StoryDecompositionService> _logger;

    public StoryDecompositionService(
        IChatCompletionService chatService,
        IRagService ragService,
        IConfiguration configuration,
        ILogger<StoryDecompositionService> logger)
    {
        _chatService = chatService;
        _ragService = ragService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Returns the list of CSV file names found in the configured pipeline input folder.
    /// The folder path is resolved from the "Pipeline:InputFolder" configuration value.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetInputCsvFileNamesAsync(CancellationToken cancellationToken = default)
    {
        var configuredFolder = _configuration["Pipeline:InputFolder"] ?? Path.Combine("Input", "Project1");
        var inputFolder = Path.Combine(AppContext.BaseDirectory, configuredFolder);
        var fileNames = await RunStageAsync("List pipeline input files", () => Task.Run<IReadOnlyList<string>>(() =>
        {
            if (!Directory.Exists(inputFolder))
            {
                return Array.Empty<string>();
            }

            // Retrieve all files in the configured folder, irrespective of extension.
            return Directory.GetFiles(inputFolder)
                .Select(Path.GetFileName)
                .Where(fileName => fileName is not null)
                .Select(fileName => fileName!)
                .ToArray();
        }, cancellationToken));

        return fileNames;
    }

    public async Task<DecompositionResult> DecomposeStoryAsync(Story story, CancellationToken cancellationToken = default)
    {
        var decompositionId = Guid.NewGuid();
        using var scope = _logger.BeginScope("DecompositionId: {DecompositionId}", decompositionId);

        var configuredFolder = _configuration["Pipeline:InputFolder"] ?? Path.Combine("Input", "Project1");
        var inputFolder = Path.Combine(AppContext.BaseDirectory, configuredFolder);
        var csvFiles = RunStage("Discover CSV inputs", () =>
            Directory.Exists(inputFolder) ? Directory.GetFiles(inputFolder, "*.csv") : Array.Empty<string>());

        for (var index = 0; index < csvFiles.Length; index++)
        {
            var csvPath = csvFiles[index];
            var content = await RunStageAsync($"Read CSV input {index + 1}", () =>
                File.ReadAllTextAsync(csvPath, cancellationToken));
            var docId = Path.GetFileName(csvPath);
            await RunStageAsync($"Index CSV input {index + 1}", () =>
                _ragService.IndexDocumentAsync(docId, content, cancellationToken));
        }

        await RunStageAsync("Index acceptance criteria", () =>
            _ragService.IndexDocumentAsync(decompositionId.ToString(), story.AcceptanceCriteria, cancellationToken));
        var context = await RunStageAsync("Retrieve related context", () =>
            _ragService.GetContextAsync(story.AcceptanceCriteria, cancellationToken));
        var prompt = await RunStageAsync("Load and prepare prompt", () =>
            LoadPromptAsync(PromptFileName, story.AcceptanceCriteria, context, cancellationToken));
        var response = await RunStageAsync("Generate story analysis", () =>
            _chatService.GenerateAsync(prompt, cancellationToken));
        var result = RunStage("Parse model response", () => LlmJsonParser.ParseObject(response));

        var tasksToken = FindAnalysisProperty(result, "tasks");
        var tasks = RunStage("Map tasks", () => ConvertToTasks(tasksToken));
        var questions = RunStage("Map questions", () => ConvertToQuestions(FindAnalysisProperty(result, "questions")));
        var reasoning = RunStage("Map reasoning", () => ConvertToReasoning(FindAnalysisProperty(result, "reasoning")));
        var estimate = RunStage("Parse estimate", () => ParseEstimate(FindAnalysisProperty(result, "estimatedTotalStoryPoints")));
        RunStage("Validate analysis", () => ValidateAnalysis(tasks, questions, reasoning, estimate));

        return new DecompositionResult
        {
            Tasks = tasks,
            Questions = questions,
            Reasoning = reasoning,
            EstimatedTotalStoryPoints = estimate
        };
    }

    private async Task<T> RunStageAsync<T>(string stageName, Func<Task<T>> operation)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            var result = await operation();
            _logger.LogInformation("Completed story decomposition stage {StageName} in {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Story decomposition stage {StageName} was cancelled after {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Story decomposition stage {StageName} failed after {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task RunStageAsync(string stageName, Func<Task> operation)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            await operation();
            _logger.LogInformation("Completed story decomposition stage {StageName} in {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Story decomposition stage {StageName} was cancelled after {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Story decomposition stage {StageName} failed after {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
            throw;
        }
    }

    private void RunStage(string stageName, Action operation)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            operation();
            _logger.LogInformation("Completed story decomposition stage {StageName} in {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Story decomposition stage {StageName} failed after {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
            throw;
        }
    }

    private T RunStage<T>(string stageName, Func<T> operation)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            var result = operation();
            _logger.LogInformation("Completed story decomposition stage {StageName} in {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Story decomposition stage {StageName} failed after {ElapsedMilliseconds} ms", stageName, timer.ElapsedMilliseconds);
            throw;
        }
    }

    // The helper methods are copied verbatim from the original implementation.
    private static async Task<string> LoadPromptAsync(string fileName, string acceptanceCriteria, string context, CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "prompts", fileName);
        var template = await File.ReadAllTextAsync(path, cancellationToken);
        return template.Replace("{acceptanceCriteria}", acceptanceCriteria).Replace("{context}", context);
    }

    private static List<DomainTask> ConvertToTasks(JToken? tasksArray)
    {
        var tasks = new List<DomainTask>();
        if (tasksArray is JValue { Type: JTokenType.String } serializedTasks)
        {
            try
            {
                tasksArray = JToken.Parse(serializedTasks.Value<string>() ?? string.Empty);
            }
            catch (JsonException)
            {
                return tasks;
            }
        }

        var taskTokens = tasksArray switch
        {
            JArray array => array,
            JObject taskObject => new JArray(taskObject),
            _ => new JArray()
        };

        foreach (var taskToken in taskTokens)
        {
            var taskJson = taskToken as JObject ?? (taskToken as JArray)?.OfType<JObject>().FirstOrDefault();
            if (taskJson is null) continue;
            tasks.Add(new DomainTask
            {
                Title = GetPropertyValue(taskJson, "title") ?? string.Empty,
                Description = GetPropertyValue(taskJson, "description") ?? GetPropertyValue(taskJson, "desc") ?? string.Empty,
                AreaOfChange = GetPropertyValue(taskJson, "areaOfChange") ?? string.Empty
            });
        }
        return tasks;
    }

    private static JToken? FindAnalysisProperty(JObject result, string propertyName)
    {
        var value = result.GetValue(propertyName, StringComparison.OrdinalIgnoreCase);
        if (value is not null)
        {
            return value;
        }

        foreach (var wrapperName in new[] { "analysis", "result", "data", "output" })
        {
            if (result.GetValue(wrapperName, StringComparison.OrdinalIgnoreCase) is JObject nestedResult)
            {
                value = FindAnalysisProperty(nestedResult, propertyName);
                if (value is not null)
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static string? GetPropertyValue(JObject value, string propertyName)
        => value.GetValue(propertyName, StringComparison.OrdinalIgnoreCase)?.Value<string>();

    private static List<string> ConvertToQuestions(JToken? questionsToken)
    {
        var questions = new List<string>();
        foreach (var q in questionsToken as JArray ?? [])
        {
            string? question = q.Type switch
            {
                JTokenType.String => q.Value<string>(),
                JTokenType.Object => GetPropertyValue((JObject)q, "question") ?? GetPropertyValue((JObject)q, "text") ?? GetPropertyValue((JObject)q, "title") ?? GetPropertyValue((JObject)q, "description") ?? GetPropertyValue((JObject)q, "desc"),
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
        var taskIssues = new List<string>();
        if (tasks.Count is < 4 or > 7)
        {
            taskIssues.Add($"task-count:{tasks.Count}");
        }

        for (var index = 0; index < tasks.Count; index++)
        {
            var task = tasks[index];
            var taskNumber = index + 1;

            if (string.IsNullOrWhiteSpace(task.Title))
            {
                taskIssues.Add($"task-{taskNumber}-missing-title");
            }
            else if (task.Title.Contains("Task name", StringComparison.OrdinalIgnoreCase))
            {
                taskIssues.Add($"task-{taskNumber}-placeholder-title");
            }

            if (string.IsNullOrWhiteSpace(task.Description))
            {
                taskIssues.Add($"task-{taskNumber}-missing-description");
            }
            else if (task.Description.Contains("Detailed description", StringComparison.OrdinalIgnoreCase))
            {
                taskIssues.Add($"task-{taskNumber}-placeholder-description");
            }

            if (!ValidTaskAreas.Contains(task.AreaOfChange.Trim()))
            {
                taskIssues.Add($"task-{taskNumber}-invalid-area");
            }
        }

        if (taskIssues.Count > 0)
        {
            throw new InvalidOperationException(
                $"The LLM returned an invalid task breakdown. Issues: {string.Join(", ", taskIssues)}.");
        }

        var analysisIssues = new List<string>();
        if (questions.Count is < 5 or > 7)
        {
            analysisIssues.Add($"question-count:{questions.Count}");
        }

        if (questions.Any(question => question.Contains("Question 1", StringComparison.OrdinalIgnoreCase)))
        {
            analysisIssues.Add("placeholder-question");
        }

        if (reasoning.Count == 0)
        {
            analysisIssues.Add("missing-reasoning");
        }
        else if (reasoning.Any(item => item.Contains("reasoning1", StringComparison.OrdinalIgnoreCase)))
        {
            analysisIssues.Add("placeholder-reasoning");
        }

        if (estimate is not (1 or 3 or 5 or 8 or 13))
        {
            analysisIssues.Add($"invalid-estimate:{estimate}");
        }

        if (analysisIssues.Count > 0)
        {
            throw new InvalidOperationException(
                $"The LLM returned an incomplete story analysis. Issues: {string.Join(", ", analysisIssues)}.");
        }
    }
}
