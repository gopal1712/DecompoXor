# DecompoXor: RAG-Powered Story Decomposition

DecompoXor is a RAG-powered ASP.NET Core Web API that uses retrieved context and AI generation to convert story acceptance criteria into implementation tasks, refinement questions, reasoning, and an overall story-point estimate.

The API accepts only `acceptanceCriteria`. Story title and description are not required.

## Features

- Decomposes acceptance criteria into actionable tasks.
- Categorizes tasks as UI, API, Database, Authentication, DevOps, Testing, Documentation, or Other.
- Generates clarification questions for planning and refinement.
- Returns structured reasoning for the task breakdown.
- Estimates the complete story using `1`, `3`, `5`, `8`, or `13` story points.
- Loads one combined analysis prompt from the `prompts` directory.
- Uses a retrieval-augmented generation (RAG) pipeline with OpenRouter embeddings and an in-memory vector store.
- Uses Groq for text generation and OpenRouter for embeddings.
- Includes a same-origin chat UI for submitting acceptance criteria and reviewing decomposition results.

## RAG Pipeline

1. The API receives acceptance criteria.
2. The application embeds configured CSV files and the acceptance criteria through OpenRouter.
3. The in-memory vector store retrieves up to three similar documents.
4. The application loads the analysis prompt and requests a JSON response from Groq, with a configured completion limit of 4096 tokens.
5. The response is parsed and validated before the API returns tasks, questions, reasoning, and an estimate.

The current prompt template does not include a `{context}` placeholder, so retrieved documents are not yet passed into the Groq prompt.

## Requirements

- .NET SDK 10.0 or later
- A Groq API key for chat completions
- An OpenRouter API key for embeddings
- Network access to both providers when calling the API

## Configuration

Groq is the registered text-generation provider. OpenRouter is used for embeddings. Configuration comes from `appsettings.json`, user secrets, and environment variables; user secrets and environment variables override the JSON file.

| Setting | Purpose | Default |
| --- | --- | --- |
| `Groq:ApiKey` | Groq authentication | Required |
| `Groq:Model` | Chat model | `openai/gpt-oss-20b` |
| `Groq:BaseUrl` | Groq API base URL | `https://api.groq.com/openai/v1` |
| `Groq:ChatCompletionsPath` | Chat completions route | `/chat/completions` |
| `Groq:Temperature` | Generation temperature | `0.2` |
| `Groq:MaxTokens` | Maximum generated tokens | `4096` |
| `Openrouter:ApiKey` | OpenRouter authentication | Required |
| `Openrouter:BaseUrl` | Embeddings API base URL | `https://openrouter.ai/api/v1` |
| `Openrouter:EmbeddingPath` | Embeddings route | `/embeddings` |
| `Openrouter:EmbeddingModel` | Embedding model | `all-MiniLM-L6-v2` |
| `Pipeline:InputFolder` | CSV input directory | `Input/Project1` relative to the application base directory |

The repository's `appsettings.json` may already contain provider credentials. User secrets or environment variables override those values at runtime, but do not remove secrets from Git history. Remove committed credentials from tracked files and rotate them before sharing or publishing the repository.

### Store local API keys

From the repository root, initialize user secrets for the API project and set both provider keys:

```powershell
dotnet user-secrets init --project src/DecompoXor.Api/DecompoXor.Api.csproj
dotnet user-secrets set "Groq:ApiKey" "your-groq-api-key" --project src/DecompoXor.Api/DecompoXor.Api.csproj
dotnet user-secrets set "Openrouter:ApiKey" "your-openrouter-api-key" --project src/DecompoXor.Api/DecompoXor.Api.csproj
```

Run `init` only once for this project; if it already has a `UserSecretsId`, skip that command.

Alternatively, set environment variables in the PowerShell session that will run the API:

```powershell
$env:Groq__ApiKey = "your-groq-api-key"
$env:Openrouter__ApiKey = "your-openrouter-api-key"
```

Never commit real API keys. If a key has already been committed or shared, revoke it and create a replacement.

## Run Locally

From the repository root, restore and build the solution, then start the HTTP launch profile:

```powershell
dotnet restore DecompoXor.slnx
dotnet build DecompoXor.slnx
dotnet run --project src/DecompoXor.Api/DecompoXor.Api.csproj --launch-profile http
```

The `http` profile serves at `http://localhost:5235`; Swagger is at `http://localhost:5235/swagger`. The default `DecompoXor.Api` profile uses `http://localhost:5180` and `https://localhost:7180`. The `https` profile uses `https://localhost:7097` and `http://localhost:5235`.

### Use the chat UI

Open the profile URL, for example `http://localhost:5235/`. Enter acceptance criteria or choose a sample prompt, then select **Decompose**. The UI shows an animated processing state followed by the estimate, task breakdown, questions, and reasoning. Use **Copy JSON** to copy the full API response, **Retry** after a failed request, or **New analysis** to clear the conversation. Press Enter to submit or Shift+Enter to add a line.

The chat UI calls the same-origin `POST /api/story/decompose` endpoint. Swagger remains available at `/swagger`, and the endpoint can also be called directly as described below.

CSV input paths are resolved relative to the running application's base directory, not the repository root. To use the sample CSV under the source tree, set the folder to its absolute path before starting the API:

```powershell
$env:Pipeline__InputFolder = (Resolve-Path "src/DecompoXor.Api/Input/Project1").Path
dotnet run --project src/DecompoXor.Api/DecompoXor.Api.csproj --launch-profile http
```

Each CSV file in that directory is read and indexed during story decomposition. The application currently does not copy the source `Input` directory to the build output automatically.

## API Usage

### Decompose a story

```http
POST /api/story/decompose
Content-Type: application/json
```

Request body:

```json
{
  "acceptanceCriteria": "User can enter card details. Payment is processed successfully. Error handling for failed payments."
}
```

Using PowerShell's `Invoke-RestMethod`:

```powershell
$body = @{
    acceptanceCriteria = "User can enter card details. Payment is processed successfully. Error handling for failed payments."
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
    -Uri "http://localhost:5235/api/story/decompose" `
    -ContentType "application/json" `
    -Body $body
```

## Output

Example response:

```json
{
  "tasks": [
    {
      "title": "Build the card details form",
      "description": "Create a payment form that collects the card details required by the story, with client-side validation for mandatory values and clear field-level errors. Keep submission state accessible and prevent duplicate requests while processing. Confirm data is sent securely and is not persisted by the UI.",
      "areaOfChange": "UI"
    },
    {
      "title": "Integrate payment processing",
      "description": "Integrate the selected payment gateway through a server-side API client, keeping secrets out of browser code and logs. Validate the request, handle success and decline responses, and map provider failures to safe API errors. Add duplicate-submission protection if the gateway supports it.",
      "areaOfChange": "API"
    },
    {
      "title": "Show payment outcomes",
      "description": "Show a confirmation only after the API reports a successful payment, and present an actionable message when the charge is declined or the gateway is unavailable. Preserve entered form data when retry is safe, prevent duplicate submissions, and ensure the UI does not display sensitive card values after completion.",
      "areaOfChange": "UI"
    },
    {
      "title": "Test payment scenarios",
      "description": "Add automated tests for valid and invalid card input, successful gateway responses, declined transactions, and provider outages. Use a test double rather than the live payment provider, and assert expected API statuses plus that logs and error payloads do not expose card data.",
      "areaOfChange": "Testing"
    }
  ],
  "questions": [
    "Which payment gateway API and environment should be used?",
    "Which card fields and validation rules are required?",
    "Should card details be stored, or only sent to the gateway?",
    "What confirmation should users see after a successful payment?",
    "What recovery path should be offered after a declined payment?"
  ],
  "reasoning": {
    "acceptanceCriteriaMapping": "The form and payment flow cover the card-entry and processing criteria; the outcome view covers success and failure handling.",
    "breakdownStrategy": "The work is split across UI, API integration, outcome handling, and tests.",
    "technicalConsiderations": "Gateway failures and handling of card data require careful validation and error handling.",
    "estimationJustification": "The estimate reflects the UI, external integration, failure handling, and verification work described in the criteria."
  },
  "estimatedTotalStoryPoints": 8
}
```

### Response fields

| Field | Type | Description |
| --- | --- | --- |
| `tasks` | array | Implementation tasks generated from the acceptance criteria. |
| `tasks[].title` | string | Short task name. |
| `tasks[].description` | string | Task details and technical scope. |
| `tasks[].areaOfChange` | string | One of `UI`, `API`, `Database`, `Authentication`, `DevOps`, `Testing`, `Documentation`, or `Other`. |
| `questions` | array | Clarifying questions for story refinement. |
| `reasoning` | object | Mapping, breakdown strategy, technical considerations, and estimate justification. |
| `estimatedTotalStoryPoints` | integer | One estimate for the complete story. Accepted values are `1`, `3`, `5`, `8`, and `13`. |

The response must contain 4-7 tasks, 5-7 questions, all four non-empty reasoning fields, and an estimate of `1`, `3`, `5`, `8`, or `13`. Invalid or truncated model output returns HTTP `502 Bad Gateway`.

## Prompt Files

The prompt template is stored at:

```text
src/DecompoXor.Api/prompts/story-analysis-prompt.txt
```

The file is copied to the application output directory during build. The current runtime placeholder is:

- `{acceptanceCriteria}`

Rebuild or restart the application after changing prompt files.

## Dependencies

Package references are listed in the project files under `src/` and the test project files under `tests/`.

## Project Structure

```text
src/DecompoXor.Api/             HTTP API, configuration, and prompts
src/DecompoXor.Application/     Story decomposition workflow and JSON parsing
src/DecompoXor.Domain/          Story, task, and result entities
src/DecompoXor.Infrastructure/  Groq, OpenRouter, RAG, and vector-store implementations
tests/                          Unit and integration tests
```

## Current Limitations

- The vector store is in-memory and loses its data when the application stops.
- CSV files in the configured input folder are indexed during story decomposition; there is no separate indexing endpoint.
- Retrieved RAG context is not currently included in the prompt template.
- The API does not process real payments; payment-related output is planning guidance only.
- The API does not currently validate that `acceptanceCriteria` is non-empty before calling the AI providers.
- Estimates are relative story-point predictions based on the prompt rubric, not calibrated delivery-time forecasts.
- The integration test currently configures an `AI:Provider=mock` setting that is not used by dependency injection; running it still calls the configured external providers and requires valid API keys and network access.

## Tests

Run the unit tests from the repository root:

```powershell
dotnet test tests/DecompoXor.UnitTests/DecompoXor.UnitTests.csproj
```

The integration tests use `WebApplicationFactory` but currently reach the configured Groq and OpenRouter services. Run them only when provider credentials and network access are available:

```powershell
dotnet test tests/DecompoXor.IntegrationTests/DecompoXor.IntegrationTests.csproj
```

Builds may report NuGet package-version or nullable-reference warnings. Review warnings and package updates before deploying publicly.
