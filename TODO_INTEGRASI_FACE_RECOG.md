# To-Do Integrasi Face Recognition ke Aplikasi Sales

Status: **Fase 1 selesai di library; integrasi UI/backend belum**
Target aplikasi: `src/AbsensiSales.Mobile` (Jejak Sales)
Sumber fitur wajah: `src/AbsensiSales.Face` dan prototype `src/AbsensiFaceAppToken`
Backend: PHP/MySQL (`backend/api`, `backend/database`)

Dokumentasi teknis dan laporan kekurangan: **`DOKUMENTASI_FACE_RECOGNITION.md`**

### Status ringkas

| Fase | Status |
|---|---|
| 1. Satukan engine + API template/verifikasi | Selesai (library), unit test belum |
| 2. Pendaftaran wajah sales | Belum |
| 3. Migrasi UI verifikasi | Belum |
| 4. Sinkronisasi backend/HR | Belum |
| 5. Keamanan & privasi | Belum |
| 6. Pengujian & dokumentasi | Sebagian (dokumentasi teknis sudah) |

---

## 1. Kondisi saat ini (gap yang harus disambungkan)

| Area | Kondisi sekarang | Dampak |
|---|---|---|
| Engine wajah | `AbsensiSales.Face` sekarang punya jalur baru UltraFace + SFace, tetapi UI masih memakai jalur lama Haar + webcam | Engine baru belum dipakai aplikasi |
| Prototype | Kode UltraFace + `ImageUtil` sudah dipindahkan ke `AbsensiSales.Face`; `AbsensiFaceAppToken` masih ada sebagai salinan terpisah | Perlu hapus duplikasi nanti |
| Foto referensi | Dibaca dari folder `reference_faces/` di samping executable | Tidak ada pendaftaran wajah lewat aplikasi |
| Kamera | `FaceVerificationService` memakai `VideoCapture(0)` | Hanya jalan di Windows; Android tidak memakai kamera MAUI |
| Identitas | Pencocokan memakai nama depan (`namaTerbaik.Contains(...)`) | Tidak terikat ke `IdSales`, rawan salah cocok |
| Penyimpanan | Skor wajah kini dikirim ke API saat check-in; SQLite lokal tidak dipakai lagi | Template wajah masih dibaca dari foto referensi, belum tersimpan per sales |
| Backend | `tbl_sales.face_data` dan `tbl_kunjungan.skor_wajah` sudah ada | `skor_wajah` sudah dikirim; `face_data` belum diisi dari aplikasi |
| Sinkronisasi | Mobile sudah memanggil `ApiService.UploadFotoAsync` + `CheckInAsync` | HR menerima skor wajah, tetapi verifikasi masih jalur lama |
| Threshold | Sudah dipusatkan di `Core/Services/FaceScore.cs`; prototype masih punya salinan | Perlu hapus salinan prototype |
| UI | Bingkai kamera hanya dekorasi, bukan preview kamera asli | Pengguna tidak melihat posisi wajahnya |
| Mode simulasi | Otomatis aktif kalau model tidak ada | Harus tetap terlihat saat demo, jangan sampai lolos di release |

---

## 2. Keputusan desain yang perlu disepakati dulu (P0)

- [x] Tetapkan `AbsensiSales.Face` sebagai **satu-satunya library wajah**; `AbsensiFaceAppToken` hanya menjadi sumber kode/referensi. *(Engine sudah dipindahkan; salinan prototype belum dihapus.)*
- [ ] Pilih model verifikasi:
  - [ ] Rekomendasi: **on-device** — embedding dibuat di HP, template disimpan, server hanya menerima skor/status.
  - [ ] Alternatif: server-side matching — kirim foto/template ke backend (lebih berat, perlu kebijakan privasi).
- [x] Tentukan format data wajah di backend: **Base64 embedding SFace** (rekomendasi) atau path foto. *(Library sudah memakai Base64 template; backend belum diisi.)*
- [ ] Tentukan kapan wajib daftar wajah:
  - [ ] Setelah registrasi akun sales.
  - [ ] Saat login pertama jika `face_data` kosong.
- [ ] Tetapkan aturan verifikasi: batas skor `0.363`, maksimal 3 percobaan, lalu eskalasi manual ke HR.
- [ ] Tegaskan batasan mockup: belum ada liveness/anti-spoofing produksi; jangan dipakai untuk absensi nyata tanpa penguatan.

**Output fase ini:** keputusan tertulis singkat di README atau dokumen ini sebelum coding.

---

## 3. Fase 1 — Satukan engine face recognition (P0)

### 3.1 Pindahkan engine yang lebih baik ke library bersama

- [ ] Pindahkan `FaceDetector.cs` (UltraFace/`version-RFB-320.onnx`) dari `src/AbsensiFaceAppToken/Services` ke `src/AbsensiSales.Face`.
- [ ] Pindahkan `ImageUtil.cs` + `RgbImage` (SkiaSharp, resize, EXIF) ke `AbsensiSales.Face`.
- [ ] Pertahankan `FaceEmbedder.cs` SFace sebagai embedder tunggal.
- [ ] Tambahkan `FaceTemplateCodec` untuk konversi `float[]` ↔ Base64 (adaptasi dari `TokenStore.ToToken` / `FromToken`).
- [ ] Buat `FaceOptions` berisi `MatchThreshold = 0.363`, batas ukuran wajah minimum, dan jumlah percobaan.
- [ ] Hapus angka `0.363` dari ViewModel dan model; gunakan `FaceOptions.MatchThreshold`.

### 3.2 Refactor service verifikasi

File: `src/AbsensiSales.Face/FaceVerificationService.cs`

- [ ] Ubah metode utama menjadi berbasis gambar/stream:
  - [ ] `VerifikasiDariStream(Stream gambar, string templateBase64)`
  - [ ] `BuatTemplateDariStream(Stream gambar)`
- [ ] Hapus ketergantungan `VideoCapture(0)` dari alur utama.
- [ ] Jika webcam Windows masih dibutuhkan untuk demo, pisahkan menjadi adapter desktop.
- [ ] Hasil verifikasi harus memuat: `Cocok`, `Skor`, `Pesan`, `JumlahWajah`, `Simulasi`.
- [ ] Tolak jika wajah tidak terdeteksi atau lebih dari satu wajah.
- [ ] Tambahkan pengecekan kualitas sederhana: ukuran wajah minimum, posisi di tengah, kecerahan.
- [ ] Pastikan gambar hanya diproses di memori dan tidak disimpan permanen.

### 3.3 Model & dependency

- [ ] Muat model dari asset MAUI (`FileSystem.OpenAppPackageFileAsync`) atau folder yang disalin ke output.
- [ ] Tambahkan model ke mekanisme asset/`MauiAsset` tanpa meng-commit file ONNX.
- [ ] Pastikan `AbsensiSales.Face` membawa dependency `SkiaSharp` + `Microsoft.ML.OnnxRuntime` yang kompatibel Windows & Android.
- [ ] Uji build: `dotnet build src/AbsensiSales.Mobile -f net10.0-windows10.0.19041.0` dan `-f net10.0-android`.
- [ ] Tambahkan unit test untuk:
  - [ ] cosine similarity,
  - [ ] batas threshold (di bawah/di atas),
  - [ ] encode/decode Base64,
  - [ ] foto tanpa wajah / dua wajah.

**Definition of Done Fase 1:** satu foto dari stream bisa menghasilkan embedding dan dicocokkan dengan template Base64 tanpa membaca folder referensi.

---

## 4. Fase 2 — Pendaftaran wajah sales (P0)

### 4.1 Model & database lokal

- [ ] Tambahkan properti ke `src/AbsensiSales.Core/Models/Sales.cs`:
  - [ ] `FaceData` (string? / Base64 template)
  - [ ] `FaceUpdatedAt` (string?)
  - [ ] Opsional: `FaceVersion` (int)
- [ ] Pastikan `AppDatabase.GetConnectionAsync()` membuat/menambah kolom baru dengan aman untuk database lama.
- [ ] Tambahkan method di `SalesRepository`:
  - [ ] `SimpanFaceDataAsync(int salesId, string template)`
  - [ ] `HapusFaceDataAsync(int salesId)`
  - [ ] `GetFaceDataAsync(int salesId)`

### 4.2 Halaman pendaftaran wajah

Buat halaman baru, misalnya `Views/DaftarWajahPage.xaml` + `ViewModels/DaftarWajahViewModel.cs`.

- [ ] Tampilkan preview kamera asli (bukan bingkai dekorasi).
- [ ] Minta izin kamera (`Permissions.Camera`) dan tangani penolakan.
- [ ] Ambil foto lewat `MediaPicker.CapturePhotoAsync()` atau `CameraView`.
- [ ] Proses foto:
  - [ ] deteksi wajah,
  - [ ] validasi hanya satu wajah,
  - [ ] validasi ukuran/posisi/kecerahan,
  - [ ] buat template,
  - [ ] simpan ke `Sales.FaceData`.
- [ ] Tampilkan status berhasil/gagal dan tombol coba lagi.
- [ ] Hapus foto sementara setelah template dibuat.
- [ ] Daftarkan route di `AppShell.xaml.cs` dan DI di `MauiProgram.cs`.

### 4.3 Sambungkan ke alur akun

- [ ] Setelah `RegisterViewModel` berhasil membuat akun, arahkan ke halaman daftar wajah.
- [ ] Saat login, jika `sales.FaceData` kosong, tampilkan kewajiban daftar wajah sebelum bisa absensi.
- [ ] Sediakan menu **Daftar ulang wajah** (di halaman profil/beranda; jika belum ada, buat halaman profil sederhana).
- [ ] Saat daftar ulang, minta konfirmasi dan timpa template lama.

**Definition of Done Fase 2:** sales bisa mendaftarkan wajah dari kamera perangkat, template tersimpan di SQLite, dan tetap ada setelah aplikasi ditutup.

---

## 5. Fase 3 — Sambungkan verifikasi ke alur absensi (P0)

File utama:
- `src/AbsensiSales.Mobile/ViewModels/VerifikasiWajahViewModel.cs`
- `src/AbsensiSales.Mobile/Views/VerifikasiWajahPage.xaml`
- `src/AbsensiSales.Mobile/ViewModels/VerifikasiDataViewModel.cs`
- `src/AbsensiSales.Mobile/Services/FaceService.cs`

- [ ] Ganti bingkai dekorasi dengan preview kamera asli atau alur ambil foto yang jelas.
- [ ] Ubah `FaceService` menjadi async dan menerima `Sales`/template, bukan hanya nama.
- [ ] `VerifikasiWajahViewModel`:
  - [ ] ambil foto dari kamera,
  - [ ] panggil service dengan `sales.FaceData`,
  - [ ] cocokkan hanya dengan template milik sales yang login,
  - [ ] tampilkan skor dan pesan yang jelas,
  - [ ] batasi percobaan (3x),
  - [ ] hanya lanjut ke foto toko jika `Cocok == true`.
- [ ] Hapus pencocokan berbasis nama depan.
- [ ] Simpan metadata verifikasi ke `AbsensiFlowState`:
  - [ ] `SkorWajah`
  - [ ] `WaktuVerifikasiWajah`
  - [ ] Opsional: `MetodeVerifikasi` / `FaceDataVersion`
- [ ] Di `VerifikasiDataViewModel`, gunakan `FaceOptions.MatchThreshold`, bukan `0.363`.
- [ ] Saat menyimpan `AbsensiKunjungan`, simpan skor wajah dan status verifikasi.
- [ ] Jika mode simulasi aktif, tampilkan label **MODE SIMULASI** dan jangan aktifkan di build Release.
- [ ] Tangani kasus: kamera ditolak, wajah tidak ada, dua wajah, cahaya kurang, wajah orang lain.

**Definition of Done Fase 3:** sales yang terdaftar bisa absen dengan wajahnya; wajah orang lain ditolak; tidak ada jalan lanjut tanpa verifikasi yang sah.

---

## 6. Fase 4 — Sinkronisasi ke backend & HR (P1)

### 6.1 Endpoint wajah

- [ ] Buat `backend/api/wajah.php`:
  - [ ] `GET action=status` — cek apakah sales sudah punya `face_data`.
  - [ ] `POST action=simpan` — simpan/ubah template Base64 milik sales yang login.
  - [ ] `POST action=hapus` — hapus template.
  - [ ] Validasi role `Sales`, panjang Base64, dan hanya boleh mengubah data sendiri.
- [ ] Opsional: `POST action=verifikasi` jika nanti matching dipindah ke server.
- [ ] Tambahkan kolom `face_updated_at` di `tbl_sales` jika diperlukan.

### 6.2 Klien API

File: `src/AbsensiSales.Core/Api/ApiService.cs`, `ApiModels.cs`

- [ ] Tambahkan `GetFaceStatusAsync()`.
- [ ] Tambahkan `SimpanFaceDataAsync(string template)`.
- [ ] Tambahkan `HapusFaceDataAsync()`.
- [ ] Pastikan `LoginData.Sales.FaceData` dipakai saat login.
- [ ] Saat login berhasil:
  - [ ] ambil `face_data` dari server,
  - [ ] simpan ke SQLite lokal jika lokal kosong,
  - [ ] jika berbeda, tentukan kebijakan (server menang atau minta daftar ulang).

### 6.3 Kirim hasil absensi ke backend

- [ ] Petakan identitas lokal ke backend:
  - [ ] `Sales.Nip` ↔ `nomor_sales`
  - [ ] `Toko.KodeToko` ↔ `kode_toko`
- [ ] Setelah kunjungan tersimpan lokal, panggil `ApiService.CheckInAsync` dengan:
  - [ ] `IdToko` backend,
  - [ ] latitude/longitude,
  - [ ] `SkorWajah`,
  - [ ] `TujuanKunjungan`, `Catatan`,
  - [ ] `FotoBukti` (nama file/path sesuai kebijakan backend).
- [ ] Simpan `id_kunjungan` backend ke data lokal.
- [ ] Jika offline, simpan status `PendingSinkron` dan sediakan mekanisme retry.
- [ ] Validasi server:
  - [ ] skor wajah dalam rentang 0–1,
  - [ ] tolak skor di bawah threshold jika wajah wajib,
  - [ ] jangan percaya mentah-mentah skor dari client; minimal catat sebagai audit.
- [ ] HR:
  - [ ] tampilkan status **Wajah terdaftar / belum terdaftar** di master sales,
  - [ ] tampilkan skor dan metode verifikasi di detail kunjungan,
  - [ ] tambahkan badge/filter untuk sales tanpa `face_data`.

**Definition of Done Fase 4:** absensi yang sudah diverifikasi wajah muncul di backend/HR lengkap dengan skor wajah, dan template wajah sales tersimpan di `tbl_sales.face_data`.

---

## 7. Fase 5 — Keamanan, privasi, dan edge case (P1)

- [ ] Jangan menyimpan foto mentah wajah; simpan template/embedding saja.
- [ ] Simpan template secara aman (minimal tidak plain text di file publik; pertimbangkan secure storage).
- [ ] Minta persetujuan pengguna saat pendaftaran wajah.
- [ ] Sediakan hapus data wajah saat akun dihapus atau sales keluar.
- [ ] Tambahkan audit log: siapa mendaftar, kapan, dari perangkat mana.
- [ ] Batasi percobaan verifikasi untuk mencegah brute force.
- [ ] Pertimbangkan liveness/anti-spoofing sederhana (gerakan/blink) atau dokumentasikan bahwa fitur ini belum ada.
- [ ] Tangani re-enroll, ganti akun, reset aplikasi, dan reinstall.
- [ ] Pastikan `face_data` tidak ikut ter-log atau tampil di UI.
- [ ] Pastikan mode simulasi tidak bisa lolos ke build produksi.

---

## 8. Fase 6 — Pengujian & dokumentasi (P0/P1)

### 8.1 Matriks pengujian

- [ ] Windows: kamera webcam + model asli.
- [ ] Android: kamera belakang/depan + izin kamera.
- [ ] Sales sudah terdaftar vs belum terdaftar.
- [ ] Wajah benar vs wajah orang lain.
- [ ] Tidak ada wajah, dua wajah, wajah terlalu kecil, cahaya gelap.
- [ ] Foto miring/rotasi EXIF.
- [ ] Izin kamera ditolak.
- [ ] Offline saat absensi.
- [ ] Daftar ulang wajah.
- [ ] Login dengan akun berbeda setelah ada template.

### 8.2 Dokumentasi

- [ ] Perbarui `README.md` (alur wajah yang baru, bukan folder `reference_faces` lagi).
- [ ] Perbarui `models/README.md` (model yang dipakai, cara menaruh model).
- [ ] Perbarui `INTEGRASI_API.md` (endpoint `wajah.php`, `face_data`, `skor_wajah`).
- [ ] Perbarui `ERD_ANALYSIS.md` jika menambah kolom `face_updated_at`.
- [ ] Buat skrip/demo data untuk mendaftarkan wajah sales demo.
- [ ] Siapkan skenario demo presentasi: daftar wajah → absensi → verifikasi → HR melihat skor.

**Definition of Done Fase 6:** semua skenario utama lulus di Windows dan Android, dokumentasi sesuai kode, dan demo bisa dijalankan tanpa folder `reference_faces`.

---

## 9. Urutan pengerjaan yang disarankan

1. **Fase 1** — satukan engine dan hilangkan ketergantungan webcam/folder referensi.
2. **Fase 2** — buat pendaftaran wajah dan simpan template di `Sales`.
3. **Fase 3** — sambungkan verifikasi ke alur absensi memakai template sales.
4. **Fase 4** — kirim template dan skor ke backend/HR.
5. **Fase 5** — penguatan keamanan dan edge case.
6. **Fase 6** — pengujian menyeluruh dan dokumentasi.

Untuk demo kuliah, **Fase 1–3 sudah cukup** untuk menunjukkan face recognition benar-benar terhubung ke aplikasi sales. Fase 4–6 melengkapi integrasi backend, keamanan, dan kesiapan presentasi.

---

## 10. File yang paling sering disentuh

| Kebutuhan | File |
|---|---|
| Engine wajah | `src/AbsensiSales.Face/*.cs` |
| Referensi engine prototype | `src/AbsensiFaceAppToken/Services/*.cs` |
| Service Mobile | `src/AbsensiSales.Mobile/Services/FaceService.cs` |
| UI verifikasi | `src/AbsensiSales.Mobile/Views/VerifikasiWajahPage.xaml` |
| ViewModel verifikasi | `src/AbsensiSales.Mobile/ViewModels/VerifikasiWajahViewModel.cs` |
| Validasi & simpan kunjungan | `src/AbsensiSales.Mobile/ViewModels/VerifikasiDataViewModel.cs` |
| Model sales | `src/AbsensiSales.Core/Models/Sales.cs` |
| Repository sales | `src/AbsensiSales.Core/Repositories/SalesRepository.cs` |
| Database lokal | `src/AbsensiSales.Core/Data/AppDatabase.cs` |
| Model API | `src/AbsensiSales.Core/Api/ApiModels.cs` |
| Service API | `src/AbsensiSales.Core/Api/ApiService.cs` |
| Backend login/sales/absen | `backend/api/login.php`, `sales.php`, `absen.php` |
| Backend wajah baru | `backend/api/wajah.php` |
| Skema database | `backend/database/*.sql` |
| Dokumentasi | `README.md`, `INTEGRASI_API.md`, `ERD_ANALYSIS.md`, `models/README.md` |
