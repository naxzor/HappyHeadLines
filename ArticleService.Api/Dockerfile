# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ../.. .
RUN dotnet restore ArticleService.Api/ArticleService.Api.csproj
RUN dotnet publish ArticleService.Api/ArticleService.Api.csproj -c Release -o /app

# run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ArticleService.Api.dll"]