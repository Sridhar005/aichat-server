
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app


COPY . .


RUN dotnet restore
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE 10000

ENTRYPOINT ["dotnet", "MyApp.dll"]