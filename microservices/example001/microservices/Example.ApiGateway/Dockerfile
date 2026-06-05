FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Example.ApiGateway/Example.ApiGateway.csproj", "Example.ApiGateway/"]
COPY ["microservices.ServiceDefaults/microservices.ServiceDefaults.csproj", "microservices.ServiceDefaults/"]
RUN dotnet restore "Example.ApiGateway/Example.ApiGateway.csproj"

COPY . .
WORKDIR "/src/Example.ApiGateway"
RUN dotnet publish "Example.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Example.ApiGateway.dll"]
