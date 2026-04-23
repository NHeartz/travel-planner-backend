# 1. ใช้ Image ของ .NET SDK สำหรับการ Build โค้ด
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# คัดลอกไฟล์โปรเจกต์และติดตั้ง Library ต่างๆ
COPY ["TravelPlanner.API.csproj", "./"]
RUN dotnet restore

# คัดลอกโค้ดทั้งหมดและทำการ Publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 2. ใช้ Image ของ .NET Runtime สำหรับการรันแอป (ขนาดเล็กและเบา)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TravelPlanner.API.dll"]