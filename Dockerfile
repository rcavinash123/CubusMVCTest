FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY CubusMVCTest/*.csproj ./CubusMVCTest/

# copy everything else and build app
COPY CubusMVCTest/. ./CubusMVCTest/
WORKDIR /app/CubusMVCTest
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS runtime
WORKDIR /app
COPY --from=build /app/CubusMVCTest/out ./
ENTRYPOINT ["dotnet", "CubusMVCTest.dll"]