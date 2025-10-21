FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Contracts/Contracts.csproj Contracts/
COPY CommentDatabase/CommentDatabase.csproj CommentDatabase/
COPY CommentService.Api/CommentService.Api.csproj CommentService.Api/

RUN dotnet restore CommentService.Api/CommentService.Api.csproj

COPY Contracts/ Contracts/
COPY CommentDatabase/ CommentDatabase/
COPY CommentService.Api/ CommentService.Api/

RUN dotnet publish CommentService.Api/CommentService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","CommentService.Api.dll"]
