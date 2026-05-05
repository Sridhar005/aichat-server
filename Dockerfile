
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY MyApp.slnx .
COPY MyApp.Controllers/*.csproj ./MyApp.Controllers/
COPY MyApp.Data/*.csproj ./MyApp.Data/
COPY MyApp.Middleware/*.csproj ./MyApp.Middleware/
COPY MyApp.Models/*.csproj ./MyApp.Models/
COPY MyApp.Services/*.csproj ./MyApp.Services/
COPY MyApp.Api/*.csproj ./MyApp.Api/


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