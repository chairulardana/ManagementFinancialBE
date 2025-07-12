# Gunakan image dasar dari .NET SDK untuk membangun aplikasi
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set direktori kerja menjadi manageuang
WORKDIR /manageuang

# Salin file proyek dan restore dependensi
COPY *.csproj ./
RUN dotnet restore

# Salin sisa kode sumber dan bangun aplikasi
COPY . ./
RUN dotnet publish -c Release -o app

# Gunakan image runtime untuk menjalankan aplikasi (aspnet untuk runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /manageuang
COPY --from=build /manageuang/app .

# Tentukan port yang akan digunakan
ENV ASPNETCORE_URLS=http://+:9999;
EXPOSE 9999

# Perintah untuk menjalankan aplikasi
ENTRYPOINT ["dotnet", "AuthAPI.dll"]
