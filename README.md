# Jejak Sales

Aplikasi absensi kunjungan sales dengan verifikasi wajah, validasi lokasi GPS, dan persetujuan HR. Dibangun dengan .NET MAUI (aplikasi Mobile & HR) dan backend PHP/MySQL.

## Mengambil Repo

### Clone

```bash
git clone https://github.com/kvalencius/jejak.git
cd jejak
```

Branch `main` adalah sumber deploy. Untuk pengembangan, gunakan branch `development`:

```bash
git checkout development
```

### Fork (untuk kontribusi)

1. Buka repo di GitHub, lalu klik **Fork**.
2. Clone hasil fork:
   ```bash
   git clone https://github.com/<username>/jejak.git
   cd jejak
   ```
3. Tambahkan repo asal sebagai `upstream`:
   ```bash
   git remote add upstream https://github.com/kvalencius/jejak.git
   ```
4. Buat branch kerja dari `development`:
   ```bash
   git fetch upstream
   git checkout -b fitur-baru upstream/development
   ```
5. Commit, push ke fork, lalu buka Pull Request dengan target branch **`development`** (bukan `main`).

## Prasyarat

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Workload .NET MAUI: `dotnet workload install maui`
- Visual Studio 2026 (opsional)
- Perangkat Android (opsional, untuk menjalankan aplikasi Mobile)

## Menjalankan

```bash
# Build seluruh solusi
dotnet build AbsensiSales.slnx

# Aplikasi Mobile — Windows
dotnet run --project src/AbsensiSales.Mobile -f net10.0-windows10.0.19041.0

# Aplikasi Mobile — Android
dotnet run --project src/AbsensiSales.Mobile -f net10.0-android

# Aplikasi HR — Windows
dotnet run --project src/AbsensiSales.HR
```

Bisa juga membuka `AbsensiSales.slnx` di Visual Studio lalu memilih projek yang ingin dijalankan.

## Konfigurasi

- **URL backend**: atur di `src/AbsensiSales.Core/Api/ApiConfig.cs`.
- **Koneksi database backend**: isi `backend/api/config.php` sesuai hosting masing-masing.
- **Model face recognition**: unduh sesuai petunjuk di `models/README.md`. Tanpa model, aplikasi memakai mode simulasi.
- **Setup backend & database**: lihat [INTEGRASI_API.md](INTEGRASI_API.md).

## Struktur Proyek

```
AbsensiSales.slnx
├── src/
│   ├── AbsensiSales.Core/     # Model, klien API, session, style
│   ├── AbsensiSales.Face/     # Verifikasi wajah (OpenCV DNN + model SFace ONNX)
│   ├── AbsensiSales.HR/       # Aplikasi desktop HR (MAUI, Windows)
│   └── AbsensiSales.Mobile/   # Aplikasi mobile sales (MAUI, Windows & Android)
├── models/                    # Model face recognition (unduh manual)
├── reference_faces/           # Foto referensi wajah sales (tidak di-commit)
└── backend/                   # API PHP + skema MySQL
```
