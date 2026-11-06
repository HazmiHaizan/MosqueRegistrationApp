FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MosqueRegistrationApp.csproj", "./"]
RUN dotnet restore "MosqueRegistrationApp.csproj"
COPY . .
RUN dotnet publish "MosqueRegistrationApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables for robust container runtime
ENV ASPNETCORE_URLS=http://+:10000
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 10000
ENTRYPOINT ["dotnet", "MosqueRegistrationApp.dll"]