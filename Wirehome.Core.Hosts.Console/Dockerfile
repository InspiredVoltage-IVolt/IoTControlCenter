#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Wirehome.Core.Hosts.Console/Wirehome.Core.Hosts.Console.csproj", "Wirehome.Core.Hosts.Console/"]
COPY ["Wirehome.Core/Wirehome.Core.csproj", "Wirehome.Core/"]
RUN dotnet restore "Wirehome.Core.Hosts.Console/Wirehome.Core.Hosts.Console.csproj"
COPY . .
WORKDIR "/src/Wirehome.Core.Hosts.Console"
RUN dotnet build "Wirehome.Core.Hosts.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Wirehome.Core.Hosts.Console.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wirehome.Core.Hosts.Console.dll"]