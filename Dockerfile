
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY MyApp.sln .
COPY src/MyApp.Domain/*.csproj ./src/MyApp.Domain/
COPY src/MyApp.Application/*.csproj ./src/MyApp.Application/
COPY src/MyApp.Infrastructure/*.csproj ./src/MyApp.Infrastructure/
COPY src/MyApp.Api/*.csproj ./src/MyApp.Api/
COPY tests/MyApp.Tests/*.csproj ./tests/MyApp.Tests/

RUN dotnet restore

COPY . .


WORKDIR /app/src/MyApp.Api
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /out .


ENV ASPNETCORE_ENVIRONMENT=Production


ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "MyApp.Api.dll"]