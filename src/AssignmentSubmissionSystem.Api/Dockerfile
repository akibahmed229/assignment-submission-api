# Stage 1: Runtime base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Stage 2: Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies (optimized for Docker layer caching)
COPY ["src/AssignmentSubmissionSystem.Api/AssignmentSubmissionSystem.Api.csproj", "src/AssignmentSubmissionSystem.Api/"]
RUN dotnet restore "src/AssignmentSubmissionSystem.Api/AssignmentSubmissionSystem.Api.csproj"

# Copy remaining source files and build
COPY . .
WORKDIR "/src/src/AssignmentSubmissionSystem.Api"
RUN dotnet build "AssignmentSubmissionSystem.Api.csproj" -c Release -o /app/build

# Stage 3: Publish output binaries
FROM build AS publish
RUN dotnet publish "AssignmentSubmissionSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final runtime container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AssignmentSubmissionSystem.Api.dll"]
