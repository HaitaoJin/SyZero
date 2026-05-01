FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SyZero.Example3.SPA/SyZero.Example3.Web/SyZero.Example3.Web.csproj", "SyZero.Example3.SPA/SyZero.Example3.Web/"]
COPY ["SyZero.Example3.SPA/SyZero.Example3.Application/SyZero.Example3.Application.csproj", "SyZero.Example3.SPA/SyZero.Example3.Application/"]
COPY ["SyZero.Example3.SPA/SyZero.Example3.Core/SyZero.Example3.Core.csproj", "SyZero.Example3.SPA/SyZero.Example3.Core/"]
COPY ["SyZero.Example3.SPA/SyZero.Example3.IApplication/SyZero.Example3.IApplication.csproj", "SyZero.Example3.SPA/SyZero.Example3.IApplication/"]
RUN dotnet restore "SyZero.Example3.SPA/SyZero.Example3.Web/SyZero.Example3.Web.csproj"
COPY . .
WORKDIR "/src/SyZero.Example3.SPA/SyZero.Example3.Web"
RUN dotnet build "SyZero.Example3.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SyZero.Example3.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SyZero.Example3.Web.dll"]
