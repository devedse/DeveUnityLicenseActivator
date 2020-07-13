# Stage 1
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder
WORKDIR /source

# caches restore result by copying csproj file separately
#COPY /NuGet.config /source/
COPY /DeveUnityLicenseActivator/*.csproj /source/DeveUnityLicenseActivator/
COPY /DeveUnityLicenseActivator.ConsoleApp/*.csproj /source/DeveUnityLicenseActivator.ConsoleApp/
COPY /DeveUnityLicenseActivator.Tests/*.csproj /source/DeveUnityLicenseActivator.Tests/
COPY /DeveUnityLicenseActivator.sln /source/
RUN ls
RUN dotnet restore

# copies the rest of your code
COPY . .
RUN dotnet build --configuration Release
RUN dotnet test --configuration Release ./DeveUnityLicenseActivator.Tests/DeveUnityLicenseActivator.Tests.csproj
RUN dotnet publish ./DeveUnityLicenseActivator.ConsoleApp/DeveUnityLicenseActivator.ConsoleApp.csproj --output /app/ --configuration Release

# Stage 2
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "DeveUnityLicenseActivator.ConsoleApp.dll"]