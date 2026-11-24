# Jejak Sales — Aplikasi Absensi Sales (.NET MAUI)

Aplikasi absensi kunjungan sales ke toko dengan **verifikasi wajah**, **cek jarak GPS**, dan **persetujuan HR**. Dibangun dengan .NET MAUI (.NET 10). Aplikasi **Mobile dan HR sama-sama memakai backend PHP/MySQL**; tidak ada lagi data dummy di tampilan.

Terdapat dua aplikasi:

| Aplikasi | Platform | Keterangan |
|---|---|---|
| **Jejak Sales** (`AbsensiSales.Mobile`) | Windows & Android | Untuk sales: absensi kunjungan, foto toko, laporan, pengajuan izin |
| **Jejak Sales HR** (`AbsensiSales.HR`) | Windows | Untuk HR: monitoring, approval, master data, laporan, payroll (data dari API) |

## Fitur

**Mobile (Sales)**
- Login & registrasi sales
- Absensi kunjungan: verifikasi wajah (SFace ONNX), absensi GPS asli dengan validasi radius toko
- Foto toko sebagai bukti kunjungan (diunggah ke server, bukan placeholder)
- Laporan kunjungan (riwayat, status, skor wajah) langsung dari API
- Pengajuan izin/cuti beserta status persetujuan HR
- Semua daftar, detail, dan status diambil dari backend PHP/MySQL

**HR (Desktop)**
- Monitoring kehadiran sales hari ini (status, wilayah, waktu absen)
- Approval / penolakan permohonan izin sales dengan catatan
- Master data sales & toko (tambah, ubah, hapus)
- Laporan kunjungan & payroll sales
- Detail kunjungan menampilkan foto bukti toko asli dari server
  (`foto_bukti`); bila belum ada, tampil status kosong, bukan gambar contoh
- Identitas akun HR di sidebar diambil dari sesi login, bukan teks tetap
- Seluruh data diambil langsung dari backend PHP/MySQL (tanpa data dummy)

## Struktur Proyek

```
AbsensiSales.slnx
├── src/
│   ├── AbsensiSales.Core/     # Model, klien API, session, style
│   ├── AbsensiSales.Face/     # Verifikasi wajah (OpenCV DNN + model SFace ONNX)
│   ├── AbsensiSales.HR/       # Aplikasi desktop HR (MAUI, Windows)
│   └── AbsensiSales.Mobile/   # Aplikasi mobile sales (MAUI, Windows & Android)
├── models/                    # Model face recognition (unduh manual, lihat models/README.md)
├── reference_faces/           # Foto referensi wajah sales (tidak di-commit)
└── backend/                   # API PHP + skema MySQL (lihat INTEGRASI_API.md)
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

Semua akun pengujian berada di database MySQL, bukan di perangkat. Import
`backend/database/03_seed_demo.sql` untuk membuat akun demo. Login Mobile
menerima **email**, **nama lengkap**, atau **bagian sebelum `@`** pada email.

| Role | Email | Password |
|---|---|---|
| HR | `hr@example.com` | `hr123456` |
| Sales | `andi@example.com` | `demo123` |
| Sales | `sales@example.com` | `sales123` |

Kalau login ditolak (`Email/username atau password salah`), jalankan
`backend/database/05_akun_login.sql` di phpMyAdmin — file itu memaksa password
akun demo diperbarui.

## Backend Online

Repo ini juga menyertakan backend PHP + MySQL untuk hosting online
(InfinityFree). **Aplikasi Mobile dan HR sama-sama memakai backend ini sebagai
satu-satunya sumber data**: login, daftar toko, check-in, riwayat kunjungan,
permohonan izin, sampai foto bukti kunjungan.

```
backend/api/          # endpoint PHP (login, absen, toko, sales, permohonan, laporan, penggajian, upload)
backend/api/uploads/  # penyimpanan foto bukti kunjungan (harus writable)
backend/database/     # skema SQL sesuai ERD tbl_* + kolom opsional
```

Foto toko diambil dari kamera perangkat, diunggah ke `upload.php`, lalu nama
file hasil unggahan disimpan pada `tbl_kunjungan.foto_bukti`.

Panduan lengkap upload, URL, dan cara cek koneksi ada di
**[INTEGRASI_API.md](INTEGRASI_API.md)**. Di aplikasi Mobile, buka halaman login
lalu klik **Tes Koneksi Backend**.

## Model Face Recognition

Verifikasi wajah memakai model ONNX yang **tidak di-commit** ke repo. Taruh file berikut di folder `models/`:

1. `face_recognition_sface_2021dec.onnx` — dari [opencv/opencv_zoo](https://github.com/opencv/opencv_zoo) → `models/face_recognition_sface`
2. `haarcascade_frontalface_default.xml` — dari [opencv/opencv](https://github.com/opencv/opencv) → `data/haarcascades/`

Foto referensi wajah sales (nama file = username sales, mis. `andi.jpg`) ditaruh di folder `reference_faces/` di samping executable aplikasi Mobile.

Jika file model belum tersedia, aplikasi otomatis memakai **mode simulasi** (verifikasi wajah dianggap cocok dengan skor ±94%).

## Teknologi

- [.NET MAUI](https://learn.microsoft.com/dotnet/maui/) — UI multi-platform
- PHP + MySQL — backend online, satu-satunya sumber data Mobile & HR
- [OpenCvSharp4](https://github.com/shimat/opencvsharp) + [ONNX Runtime](https://onnxruntime.ai/) — deteksi & encoding wajah (model SFace)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — pola MVVM

## Prototype Face Recognition (AbsensiFaceAppToken)
Project di `src/AbsensiFaceAppToken` membuat token wajah dari foto (foto tidak disimpan).

Unduh dua model berikut dan letakkan di `src/AbsensiFaceAppToken/Resources/Raw/` (tidak ikut di-commit):
- `face_recognition_sface_2021dec.onnx` (github.com/opencv/opencv_zoo, folder models/face_recognition_sface)
- `version-RFB-320.onnx` (github.com/Linzaer/Ultra-Light-Fast-Generic-Face-Detector-1MB, folder models/onnx)

Jalankan dari folder project: `dotnet build -t:Run -f net10.0-android`

## Catatan

- Ini adalah proyek mockup kuliah. Aplikasi Mobile dan HR mengambil seluruh
  data dari backend PHP/MySQL; tidak ada data contoh yang ditampilkan di UI.
- Skema SQLite lama masih ada di `AbsensiSales.Core` untuk kemungkinan
  cache/offline, tetapi tidak dipakai dan tidak di-seed.
- Backend PHP + MySQL tersedia di folder `backend/`. Lihat
  [INTEGRASI_API.md](INTEGRASI_API.md) untuk detail.
- Password di backend PHP sudah di-hash dengan `password_hash()`; aplikasi
  tidak menyimpan password secara lokal.

   