# Build stage: the full SDK image compiles and publishes the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the files restore needs first, so Docker can cache the restore layer
# and skip it on rebuilds where only .cs files changed.
COPY global.json ./
COPY src/ElectricalToolkit.Core/ElectricalToolkit.Core.csproj src/ElectricalToolkit.Core/
COPY src/ElectricalToolkit.Api/ElectricalToolkit.Api.csproj src/ElectricalToolkit.Api/
RUN dotnet restore src/ElectricalToolkit.Api/ElectricalToolkit.Api.csproj

COPY src/ src/
RUN dotnet publish src/ElectricalToolkit.Api/ElectricalToolkit.Api.csproj -c Release -o /app --no-restore

# Runtime stage: a much smaller image with only the ASP.NET Core runtime and the published app.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# .NET images listen on port 8080 and run as a non-root user named "app" by default.
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "ElectricalToolkit.Api.dll"]