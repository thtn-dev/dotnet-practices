FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Example.AuthService/Example.AuthService.csproj", "Example.AuthService/"]
COPY ["microservices.ServiceDefaults/microservices.ServiceDefaults.csproj", "microservices.ServiceDefaults/"]
RUN dotnet restore "Example.AuthService/Example.AuthService.csproj"

COPY . .
WORKDIR "/src/Example.AuthService"
RUN dotnet publish "Example.AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5100
EXPOSE 5100
ENTRYPOINT ["dotnet", "Example.AuthService.dll"]
