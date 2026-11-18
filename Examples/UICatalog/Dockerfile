#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM opensuse/tumbleweed AS base
RUN zypper -n update
RUN zypper --non-interactive install libicu
RUN zypper --non-interactive install libncurses5
RUN zypper --non-interactive install wget
RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc
RUN wget https://packages.microsoft.com/config/opensuse/15/prod.repo
RUN mv prod.repo /etc/zypp/repos.d/microsoft-prod.repo
RUN chown root:root /etc/zypp/repos.d/microsoft-prod.repo
RUN zypper --non-interactive install dotnet-sdk-7.0

ENV LANG=en_US.UTF-8 \
	TERM=xterm-256color \
	DISPLAY=:0
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Terminal.Gui/Directory.Build.props", "Terminal.Gui/"]
COPY ["UICatalog/UICatalog.csproj", "UICatalog/"]
COPY ["Terminal.Gui/Terminal.Gui.csproj", "Terminal.Gui/"]
RUN dotnet restore "UICatalog/UICatalog.csproj"
COPY . .
WORKDIR "/src/UICatalog"
RUN dotnet build "UICatalog.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UICatalog.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UICatalog.dll"]