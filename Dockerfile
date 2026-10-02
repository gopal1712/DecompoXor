FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ ./src/
RUN dotnet restore src/DecompoXor.Api/DecompoXor.Api.csproj
RUN dotnet publish src/DecompoXor.Api/DecompoXor.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./
COPY --from=build /src/src/DecompoXor.Api/Input ./Input

ENV ASPNETCORE_HTTP_PORTS=8080
ENV Pipeline__InputFolder=/app/Input/Project1

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "DecompoXor.Api.dll"]