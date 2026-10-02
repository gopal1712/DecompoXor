# Build and publish the API with the .NET 10 SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ ./src/
RUN dotnet restore src/DecompoXor.Api/DecompoXor.Api.csproj
RUN dotnet publish src/DecompoXor.Api/DecompoXor.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Use the smaller ASP.NET runtime image for deployment.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./
COPY --from=build /src/src/DecompoXor.Api/Input ./Input

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Groq__Model=openai/gpt-oss-20b \
    Groq__BaseUrl=https://api.groq.com/openai/v1 \
    Groq__ChatCompletionsPath=/chat/completions \
    Groq__Temperature=0.2 \
    Groq__MaxTokens=4096 \
    Openrouter__BaseUrl=https://openrouter.ai/api/v1 \
    Openrouter__EmbeddingPath=/embeddings \
    Openrouter__EmbeddingModel=all-MiniLM-L6-v2 \
    Pipeline__InputFolder=/app/Input/Project1

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "DecompoXor.Api.dll"]