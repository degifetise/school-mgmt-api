FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY HaladeHighSchool.Api.csproj ./
RUN dotnet restore HaladeHighSchool.Api.csproj

COPY . ./
RUN dotnet publish HaladeHighSchool.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build --chown=app:app /app/publish ./
RUN mkdir -p /app/wwwroot/uploads && chown -R app:app /app/wwwroot/uploads
USER app

CMD ["sh", "-c", "export ASPNETCORE_URLS=\"http://+:${PORT:-8080}\"; exec dotnet HaladeHighSchool.Api.dll"]
