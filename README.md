# Jejak Sales — Aplikasi Absensi Sales (.NET MAUI)

Aplikasi absensi kunjungan sales ke toko dengan **verifikasi wajah**, **cek jarak GPS**, dan **persetujuan HR**. Dibangun dengan .NET MAUI (.NET 10) dan SQLite lokal.

Terdapat dua aplikasi:

| Aplikasi | Platform | Keterangan |
|---|---|---|
| **Jejak Sales** (`AbsensiSales.Mobile`) | Windows & Android | Untuk sales: absensi kunjungan, foto toko, laporan, pengajuan izin |
| **Jejak Sales HR** (`AbsensiSales.HR`) | Windows | Untuk HR: monitoring, approval, master data, laporan, payroll |

## Fitur

**Mobile (Sales)**
- Login & registrasi sales
- Absensi kunjungan: verifikasi wajah (SFace ONNX), absensi GPS dengan validasi radius toko
- Foto toko sebagai bukti kunjungan
- Laporan kunjungan (riwayat, status, skor wajah)
- Pengajuan izin/cuti beserta status persetujuan HR

**HR (Desktop)**
- Monitoring kunjungan sales secara langsung (peta, jarak, skor wajah)
- Approval / penolakan permohonan izin sales dengan catatan
- Master data sales & toko (tambah, ubah, hapus)
- Laporan kunjungan & payroll sales

## Struktur Proyek

```
AbsensiSales.slnx
├── src/
│   ├── AbsensiSales.Core/     # Model, database SQLite, repository, session, style
│   ├── AbsensiSales.Face/     # Verifikasi wajah (OpenCV DNN + model SFace ONNX)
│   ├── AbsensiSales.HR/       # Aplikasi desktop HR (MAUI, Windows)
│   └── AbsensiSales.Mobile/   # Aplikasi mobile sales (MAUI, Windows & Android)
├── models/                    # Model face recognition (unduh manual, lihat models/README.md)
└── reference_faces/           # Foto referensi wajah sales (tidak di-commit)
```

## Prasyarat

- [.NET 10 SDK](https://dotnet.microsoft.com/download) + workload **.NET MAUI** (`dotnet workload install maui`)
- Visual Studio 2026 (opsional, dengan workload .NET Multi-platform App UI development)
- HP Android (untuk menjalankan aplikasi Mobile di perangkat) — opsional

## Menjalankan

```bash
# Build seluruh solusi
dotnet build AbsensiSales.slnx

# Aplikasi Mobile — target Windows
dotnet run --project src/AbsensiSales.Mobile -f net10.0-windows10.0.19041.0

# Aplikasi Mobile — target Android (HP terhubung via adb/scrcpy)
dotnet run --project src/AbsensiSales.Mobile -f net10.0-android

# Aplikasi HR — Windows
dotnet run --project src/AbsensiSales.HR
```

Atau buka `AbsensiSales.slnx` di Visual Studio lalu pilih projek yang ingin dijalankan.

## Akun Demo

Database di-seed otomatis saat pertama kali dijalankan:

| Aplikasi | Username | Password |
|---|---|---|
| Mobile (Sales) | `andi.pratama` (atau sales lain: `dewi.lestari`, `rizky.putra`, ...) | `demo123` |
| HR | `admin` | `admin123` |

## Model Face Recognition

Verifikasi wajah memakai model ONNX yang **tidak di-commit** ke repo. Taruh file berikut di folder `models/`:

1. `face_recognition_sface_2021dec.onnx` — dari [opencv/opencv_zoo](https://github.com/opencv/opencv_zoo) → `models/face_recognition_sface`
2. `haarcascade_frontalface_default.xml` — dari [opencv/opencv](https://github.com/opencv/opencv) → `data/haarcascades/`

Foto referensi wajah sales (nama file = username sales, mis. `andi.jpg`) ditaruh di folder `reference_faces/` di samping executable aplikasi Mobile.

Jika file model belum tersedia, aplikasi otomatis memakai **mode simulasi** (verifikasi wajah dianggap cocok dengan skor ±94%).

## Teknologi

- [.NET MAUI](https://learn.microsoft.com/dotnet/maui/) — UI multi-platform
- [SQLite](https://www.sqlite.org/) via sqlite-net — database lokal (`absensi_sales.db3`)
- [OpenCvSharp4](https://github.com/shimat/opencvsharp) + [ONNX Runtime](https://onnxruntime.ai/) — deteksi & encoding wajah (model SFace)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — pola MVVM

## Prototype Face Recognition (AbsensiFaceAppToken)
Project di `src/AbsensiFaceAppToken` membuat token wajah dari foto (foto tidak disimpan).

Unduh dua model berikut dan letakkan di `src/AbsensiFaceAppToken/Resources/Raw/` (tidak ikut di-commit):
- `face_recognition_sface_2021dec.onnx` (github.com/opencv/opencv_zoo, folder models/face_recognition_sface)
- `version-RFB-320.onnx` (github.com/Linzaer/Ultra-Light-Fast-Generic-Face-Detector-1MB, folder models/onnx)

Jalankan dari folder project: `dotnet build -t:Run -f net10.0-android`

## Catatan

- Ini adalah proyek mockup kuliah: seluruh data tersimpan **lokal di perangkat masing-masing** (tidak ada backend/sinkronisasi antar device).
- Password tersimpan plain text sebagai bagian dari mockup — jangan dipakai di produksi.

   