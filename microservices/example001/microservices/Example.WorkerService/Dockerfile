FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Example.WorkerService/Example.WorkerService.csproj", "Example.WorkerService/"]
COPY ["microservices.ServiceDefaults/microservices.ServiceDefaults.csproj", "microservices.ServiceDefaults/"]
RUN dotnet restore "Example.WorkerService/Example.WorkerService.csproj"

COPY . .
WORKDIR "/src/Example.WorkerService"
RUN dotnet publish "Example.WorkerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5088
EXPOSE 5088
ENTRYPOINT ["dotnet", "Example.WorkerService.dll"]
