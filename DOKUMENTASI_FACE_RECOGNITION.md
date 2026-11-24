# Dokumentasi Face Recognition — Integrasi ke Aplikasi Sales

Terakhir diperbarui: 22 November 2026
Status: **Fase 1 selesai (library engine); integrasi UI/backend sudah berjalan
untuk check-in API, migrasi engine wajah belum**

> Catatan: dokumen ini fokus pada engine face recognition. Sejak integrasi
> backend, aplikasi Mobile **tidak lagi menyimpan kunjungan ke SQLite**;
> foto diunggah ke `upload.php` dan check-in dikirim ke `absen.php`. Lihat
> [INTEGRASI_API.md](INTEGRASI_API.md) bagian 11 untuk alur data terbaru.

Dokumen ini menjelaskan:
1. Kondisi face recognition sebelum perubahan.
2. Apa yang sudah dikerjakan pada tahap ini.
3. Arsitektur dan API baru.
4. Cara memakai library.
5. Hasil build dan pengujian.
6. Daftar kekurangan yang masih terbuka, agar bisa dicek ulang.

Dokumen rencana per fase ada di `TODO_INTEGRASI_FACE_RECOG.md`.

---

## 1. Ringkasan perubahan

Fokus tahap ini adalah **menyatukan engine face recognition** ke dalam satu library
bersama: `src/AbsensiSales.Face`.

Sebelumnya ada dua implementasi terpisah:

| Implementasi | Deteksi | Embedding | Input | Platform |
|---|---|---|---|---|
| `AbsensiSales.Face` | Haar Cascade (OpenCV) | SFace ONNX | Webcam `VideoCapture(0)` | Praktis Windows saja |
| `AbsensiFaceAppToken` | UltraFace RFB-320 ONNX | SFace ONNX | Foto dari `MediaPicker` | Windows + Android |

Sekarang `AbsensiSales.Face` memiliki **jalur baru** yang mengadopsi pendekatan
prototype:

- Deteksi wajah: UltraFace `version-RFB-320.onnx`.
- Embedding wajah: SFace `face_recognition_sface_2021dec.onnx`.
- Input: `byte[]` atau `Stream` (foto dari kamera/galeri), bukan webcam langsung.
- Template: embedding `float[]` → Base64.
- Decode gambar: SkiaSharp (JPEG/PNG + rotasi EXIF), bukan OpenCV.
- Verifikasi: cosine similarity terhadap template milik sales.

Jalur lama **tetap dipertahankan sementara** agar halaman verifikasi yang sekarang
belum rusak saat demo. Jalur lama akan dihapus pada Fase 3 setelah UI migrasi.

---

## 2. Kondisi sebelum perubahan

### 2.1 Alur yang dipakai aplikasi Mobile

```
Login (SQLite lokal)
  → Absensi pertama
  → Pilih toko
  → VerifikasiWajahPage
      → FaceService.Verifikasi(sales.Nama)
      → FaceVerificationService.VerifikasiDariKamera(namaSales)
      → VideoCapture(0) + Haar Cascade
      → bandingkan dengan foto di folder reference_faces/
  → Foto toko
  → VerifikasiDataPage
      → cek SkorWajah >= 0.363
      → simpan AbsensiKunjungan ke SQLite lokal
```

Masalah utama alur lama:

1. `VideoCapture(0)` tidak memakai kamera MAUI, sehingga tidak bisa diandalkan di Android.
2. Foto referensi dibaca dari folder `reference_faces/` di samping executable.
3. Pencocokan memakai nama depan, bukan `IdSales`.
4. Tidak ada proses pendaftaran wajah di aplikasi.
5. `Sales` belum menyimpan template wajah.
6. Skor wajah hanya tersimpan di SQLite; Mobile belum memanggil API check-in.
7. Threshold `0.363` ditulis ulang di beberapa tempat.
8. UI kamera masih berupa bingkai dekorasi, bukan preview kamera asli.

### 2.2 Yang sudah tersedia di backend

Backend PHP/MySQL sebenarnya sudah menyiapkan:

- `tbl_sales.face_data` (`TEXT`).
- `tbl_kunjungan.skor_wajah` (`DECIMAL(5,2)`).
- `login.php` mengembalikan `face_data` milik sales.
- `sales.php` menerima `face_data` saat HR menambah sales.
- `absen.php` menerima `skor_wajah` saat check-in.

Artinya, hambatan utama bukan skema database, melainkan **aplikasi Mobile belum
mengisi dan memakai data tersebut**.

---

## 3. Arsitektur setelah perubahan

### 3.1 Pembagian tanggung jawab

```
AbsensiSales.Core
  └── FaceScore.Threshold        // satu sumber batas skor

AbsensiSales.Face
  ├── FaceOptions                // konfigurasi engine
  ├── FaceBox                    // kotak wajah
  ├── RgbImage                   // gambar RGBA lintas platform
  ├── FaceImageLoader            // decode, EXIF, resize, crop, brightness
  ├── FaceDetector               // UltraFace ONNX
  ├── FaceEmbedder               // SFace ONNX (OpenCV + RgbImage)
  ├── FaceTemplateCodec          // float[] <-> Base64
  ├── FaceTemplateResult         // hasil pembuatan template
  ├── FaceVerifyResult           // hasil verifikasi
  └── FaceVerificationService    // orkestrasi engine baru + jalur lama

AbsensiSales.Mobile
  └── FaceService                // wrapper DI untuk library Face
```

### 3.2 Dua jalur di `FaceVerificationService`

```
Jalur baru (disarankan)
  foto/stream
    → FaceImageLoader.Load
    → FaceDetector.Detect
    → validasi jumlah wajah + ukuran + kecerahan
    → FaceEmbedder.GetEmbedding
    → FaceTemplateCodec.ToBase64
    → simpan template / bandingkan cosine similarity

Jalur lama (kompatibilitas demo)
  webcam
    → OpenCV VideoCapture
    → Haar Cascade
    → SFace embedding
    → folder reference_faces
```

`ModeSimulasi` bernilai `true` hanya jika **tidak ada** engine baru maupun jalur lama
yang berhasil dimuat.

> Penting: jalur baru **tidak pernah membuat template palsu**. Jika model belum ada,
> `BuatTemplate` mengembalikan error, bukan template simulasi.

---

## 4. API baru

### 4.1 Konfigurasi

```csharp
var options = new FaceOptions
{
    MatchThreshold = FaceScore.Threshold, // default 0.363
    MaxFacesAllowed = 1,
    MinFaceSize = 80,
    MinDetectionScore = 0.7f,
    MinBrightness = 35,
    MaxBrightness = 235
};
```

### 4.2 Membuat service

Dari folder model (Windows/desktop):

```csharp
var service = new FaceVerificationService(AppContext.BaseDirectory);
```

Dari byte model (cocok untuk Android/asset MAUI):

```csharp
var service = new FaceVerificationService(detectorModelBytes, sfaceModelBytes);
```

### 4.3 Membuat template wajah

```csharp
FaceTemplateResult hasil = service.BuatTemplate(fotoBytes);

if (!hasil.Berhasil)
{
    // tampilkan hasil.Pesan ke pengguna
    return;
}

string templateBase64 = hasil.Template!;
// simpan ke Sales.FaceData (belum diimplementasikan) atau backend
```

Aturan yang sudah diterapkan:

- Wajah harus terdeteksi.
- Jumlah wajah tidak boleh lebih dari `MaxFacesAllowed`.
- Sisi wajah minimal `MinFaceSize` piksel.
- Kecerahan wajah harus dalam rentang `MinBrightness`–`MaxBrightness`.
- Foto hanya diproses di memori; tidak disimpan oleh library.

### 4.4 Verifikasi wajah

```csharp
FaceVerifyResult hasil = service.Verifikasi(fotoKameraBytes, templateBase64);

if (hasil.Cocok && hasil.Skor >= FaceScore.Threshold)
{
    // lanjutkan absensi
}
else
{
    // tampilkan hasil.Pesan
}
```

`FaceVerifyResult` sekarang memiliki properti tambahan:

```csharp
public record FaceVerifyResult(bool Cocok, double Skor, string Pesan, bool Simulasi)
{
    public int JumlahWajah { get; init; }
}
```

### 4.5 Kodec template

```csharp
string token = FaceTemplateCodec.ToBase64(embedding);
float[] embedding = FaceTemplateCodec.FromBase64(token);

if (FaceTemplateCodec.TryFromBase64(token, out var parsed))
{
    // token valid
}
```

Detail format:

- 1 nilai `float` = 4 byte little-endian.
- Embedding SFace saat ini 128 nilai = 512 byte.
- Base64 dari 512 byte ± 684 karakter.
- Token inilah yang cocok disimpan di `tbl_sales.face_data`, bukan foto.

### 4.6 Wrapper Mobile

`src/AbsensiSales.Mobile/Services/FaceService.cs` sekarang menyediakan:

```csharp
bool EngineSiap { get; }
FaceVerifyResult Verifikasi(string namaSales);              // jalur lama
FaceTemplateResult BuatTemplate(byte[] foto);               // jalur baru
FaceVerifyResult VerifikasiDenganTemplate(byte[] foto, string template);
```

Method lama tetap ada supaya `VerifikasiWajahViewModel` yang sekarang masih bisa
dipakai.

---

## 5. Perubahan file

### 5.1 File baru

| File | Isi |
|---|---|
| `src/AbsensiSales.Core/Services/FaceScore.cs` | Konstanta threshold bersama |
| `src/AbsensiSales.Face/FaceOptions.cs` | Konfigurasi engine |
| `src/AbsensiSales.Face/FaceBox.cs` | Record kotak wajah |
| `src/AbsensiSales.Face/FaceImageLoader.cs` | `RgbImage`, decode EXIF, resize, crop, brightness |
| `src/AbsensiSales.Face/FaceDetector.cs` | Detektor UltraFace ONNX + NMS |
| `src/AbsensiSales.Face/FaceTemplateCodec.cs` | Konversi embedding ↔ Base64 |
| `src/AbsensiSales.Face/FaceTemplateResult.cs` | Hasil pembuatan template |
| `DOKUMENTASI_FACE_RECOGNITION.md` | Dokumen ini |
| `TODO_INTEGRASI_FACE_RECOG.md` | Rencana per fase (sudah ada sebelumnya) |

### 5.2 File yang diubah

| File | Perubahan |
|---|---|
| `src/AbsensiSales.Face/AbsensiSales.Face.csproj` | Tambah `SkiaSharp` 4.155.0 |
| `src/AbsensiSales.Face/FaceEmbedder.cs` | Constructor `byte[]`, overload `RgbImage`, cosine aman terhadap dimensi berbeda |
| `src/AbsensiSales.Face/FaceVerificationService.cs` | Jalur baru (template + stream), jalur lama dipertahankan, opsi threshold |
| `src/AbsensiSales.Face/FaceVerifyResult.cs` | Tambah `JumlahWajah` |
| `src/AbsensiSales.Core/Models/AbsensiKunjungan.cs` | Memakai `FaceScore.Threshold` |
| `src/AbsensiSales.Mobile/ViewModels/VerifikasiDataViewModel.cs` | Memakai `FaceScore.Threshold` |
| `src/AbsensiSales.Mobile/Services/FaceService.cs` | Wrapper `BuatTemplate` dan `VerifikasiDenganTemplate` |

### 5.3 File yang **belum** diubah

Ini penting untuk mengecek batas pekerjaan:

- `src/AbsensiSales.Mobile/ViewModels/VerifikasiWajahViewModel.cs`
- `src/AbsensiSales.Mobile/Views/VerifikasiWajahPage.xaml`
- `src/AbsensiSales.Core/Models/Sales.cs`
- `src/AbsensiSales.Core/Repositories/SalesRepository.cs`
- `src/AbsensiSales.Core/Data/AppDatabase.cs`
- `src/AbsensiSales.Core/Api/ApiModels.cs`
- `src/AbsensiSales.Core/Api/ApiService.cs`
- `backend/api/*.php`
- `backend/database/*.sql`
- `src/AbsensiFaceAppToken/*` (masih menjadi prototype terpisah)

---

## 6. Model yang dibutuhkan

Letakkan di folder `models/` (Windows) atau `Resources/Raw` (Android, untuk tahap
berikutnya):

1. `version-RFB-320.onnx`
   - Ultra-Light-Fast-Generic-Face-Detector-1MB.
   - Sumber: https://github.com/Linzaer/Ultra-Light-Fast-Generic-Face-Detector-1MB
   - Folder `models/onnx`.
2. `face_recognition_sface_2021dec.onnx`
   - Sumber: https://github.com/opencv/opencv_zoo
   - Folder `models/face_recognition_sface`.
3. `haarcascade_frontalface_default.xml` (opsional, hanya jalur lama)
   - Sumber: https://github.com/opencv/opencv
   - Folder `data/haarcascades`.

Model tidak di-commit ke repository. Jika `version-RFB-320.onnx` belum ada,
engine baru tidak aktif dan `BuatTemplate` akan mengembalikan pesan error.

---

## 7. Cara memakai di Android (target tahap berikutnya)

Library sudah menyediakan constructor `byte[]`, tetapi `FaceService` di Mobile
**belum** memuat model dari asset. Rencana pemakaian:

```csharp
static async Task<byte[]> BacaAssetAsync(string nama)
{
    using var stream = await FileSystem.OpenAppPackageFileAsync(nama);
    using var buffer = new MemoryStream();
    await stream.CopyToAsync(buffer);
    return buffer.ToArray();
}

var detector = await BacaAssetAsync("version-RFB-320.onnx");
var sface = await BacaAssetAsync("face_recognition_sface_2021dec.onnx");

var service = new FaceVerificationService(detector, sface);
```

Langkah yang belum dikerjakan:

- Menambahkan model ke `MauiAsset`/`Resources/Raw`.
- Mengubah `FaceService` menjadi inisialisasi async.
- Mengganti `AppContext.BaseDirectory` sebagai sumber model.

---

## 8. Hasil build dan pengujian

### 8.1 Build

Perintah yang dijalankan:

```powershell
dotnet build src\AbsensiSales.Core\AbsensiSales.Core.csproj
dotnet build src\AbsensiSales.Face\AbsensiSales.Face.csproj
dotnet build src\AbsensiSales.Mobile\AbsensiSales.Mobile.csproj -f net10.0-windows10.0.19041.0
dotnet build AbsensiSales.slnx -m:1
```

Hasil:

- `AbsensiSales.Core` — sukses.
- `AbsensiSales.Face` — sukses, 0 error.
- `AbsensiSales.Mobile` (Windows) — sukses, 0 error.
- Seluruh solusi dengan `-m:1` — sukses, 0 error.

Catatan:

- Build paralel penuh pernah gagal di XAML source generator
  (`InitializeComponent` tidak ditemukan) setelah menambahkan SkiaSharp.
  Build ulang secara berurutan (`-m:1`) sukses. Ini perlu dicek ulang jika
  terjadi lagi; kemungkinan besar masalah incremental/paralel XAML generator,
  bukan kode face recognition.
- Warning yang tersisa berasal dari package lama dan bukan dari perubahan ini:
  - `NU1903` kerentanan `SQLitePCLRaw.lib.e_sqlite3.android` 2.1.11.
  - `NU1603` `CommunityToolkit.Maui` 8.0.3 tidak ditemukan, memakai 9.0.0.
  - `XA0141` ONNX Runtime 1.17.1 belum mendukung page size 16 KB Android 16.
- APK Android yang dibangun saat ini hanya memuat `lib/x86_64`, karena target
  build yang dipakai. Untuk HP ARM perlu build dengan RID arm64 dan diuji ulang.

### 8.2 Pengujian yang sudah dilakukan

- Kompilasi seluruh project.
- Pemeriksaan statis alur baru (input stream → deteksi → embedding → template).
- Pemeriksaan bahwa threshold tidak lagi ditulis ulang di model dan ViewModel.

### 8.3 Pengujian yang **belum** dilakukan

- Uji dengan model ONNX asli (file model belum ada di repo).
- Uji deteksi pada foto nyata.
- Uji akurasi/threshold dengan beberapa orang.
- Uji kamera Android asli.
- Uji unit otomatis (belum ada project test).
- Uji beban/kinerja di HP kelas bawah.

Artinya, kode sudah **compile-ready**, tetapi belum **runtime-verified** dengan
model dan data wajah nyata.

---

## 9. Kekurangan dan risiko yang masih terbuka

### 9.1 Blocker untuk integrasi end-to-end

| # | Kekurangan | Dampak | Rencana |
|---|---|---|---|
| 1 | UI verifikasi masih memakai jalur lama `Verifikasi(namaSales)` | Engine baru belum dipakai di aplikasi | Fase 3 |
| 2 | Belum ada halaman pendaftaran wajah | Sales belum punya template | Fase 2 |
| 3 | `Sales` belum punya `FaceData` | Template tidak punya tempat penyimpanan lokal | Fase 2 |
| 4 | `FaceService` belum memuat model dari asset Android | Android masih bergantung folder executable | Fase 2/3 |
| 5 | Belum ada sinkronisasi `face_data` ke backend | HR tidak menerima data wajah | Fase 4 |
| 6 | Mobile belum memanggil `ApiService.CheckInAsync` | `skor_wajah` tidak sampai ke HR | Fase 4 |

### 9.2 Kualitas dan keamanan

| # | Kekurangan | Dampak | Rencana |
|---|---|---|---|
| 7 | Belum ada liveness/anti-spoofing | Foto orang lain bisa dicoba | Fase 5 |
| 8 | Threshold 0.363 belum dikalibrasi | False accept/reject belum diketahui | Fase 5 |
| 9 | Belum ada rate limit percobaan | Brute force belum dicegah | Fase 5 |
| 10 | Template disimpan sebagai Base64 biasa | Belum ada enkripsi at-rest | Fase 5 |
| 11 | Belum ada audit log pendaftaran/verifikasi | Sulit menelusuri penyalahgunaan | Fase 5 |
| 12 | Belum ada kebijakan hapus data wajah | Privasi belum lengkap | Fase 5 |

### 9.3 Utang teknis

| # | Kekurangan | Dampak | Rencana |
|---|---|---|---|
| 13 | Jalur lama (OpenCV + webcam + Haar) masih ada | Dua jalur kode, dependency OpenCV tetap | Fase 3 |
| 14 | `AbsensiFaceAppToken` masih punya salinan `FaceDetector`/`FaceEmbedder` | Duplikasi kode | Fase 1 lanjutan |
| 15 | Belum ada unit test | Regresi sulit dideteksi | Fase 6 |
| 16 | Error handling masih berbasis pesan string | Sulit membedakan jenis kegagalan di UI | Fase 3 |
| 17 | Belum ada mekanisme retry/queue sinkronisasi offline | Data bisa tertinggal | Fase 4 |
| 18 | Build paralel penuh sempat gagal di XAML generator | Perlu verifikasi CI | Fase 6 |

### 9.4 Catatan desain yang perlu dikonfirmasi

1. **Satu template per sales atau beberapa template?**
   Saat ini desain mengarah ke satu template (`face_data` satu kolom). Untuk
   kacamata/masker/kondisi cahaya berbeda, beberapa template lebih toleran,
   tetapi butuh tabel tambahan.
2. **Verifikasi di perangkat atau server?**
   Rencana saat ini: embedding dibuat di perangkat, server menerima skor/status.
   Jika server harus melakukan matching, perlu endpoint dan kebijakan privasi baru.
3. **Apakah foto wajah boleh disimpan?**
   Rencana saat ini: tidak. Hanya template/embedding yang disimpan.
4. **Apakah absensi wajib wajah?**
   Perlu keputusan produk: wajib, opsional, atau hanya untuk kunjungan tertentu.
5. **Apa yang terjadi jika model tidak tersedia?**
   Jalur lama memakai simulasi. Jalur baru menolak membuat template. Perlu
   kebijakan demo vs release yang eksplisit.

---

## 10. Cara mengecek ulang pekerjaan ini

### 10.1 Cek kode

1. Buka `src/AbsensiSales.Face/FaceVerificationService.cs`.
   - Pastikan ada method `BuatTemplate` dan `Verifikasi` berbasis `byte[]`/`Stream`.
   - Pastikan `BuatTemplate` mengembalikan error jika model tidak ada.
2. Buka `src/AbsensiSales.Face/FaceDetector.cs`.
   - Pastikan memakai `version-RFB-320.onnx`.
3. Buka `src/AbsensiSales.Face/FaceTemplateCodec.cs`.
   - Pastikan konversi memakai little-endian dan menolak Base64 tidak valid.
4. Buka `src/AbsensiSales.Face/FaceOptions.cs`.
   - Pastikan threshold diambil dari `FaceScore.Threshold`.
5. Buka `src/AbsensiSales.Core/Services/FaceScore.cs`.
   - Pastikan hanya ada satu konstanta threshold.
6. Cari angka `0.363` di seluruh repo:
   ```powershell
   Select-String -Path src\**\*.cs -Pattern '0\.363'
   ```
   Seharusnya hanya muncul di `FaceScore.cs` (plus dokumentasi).
7. Buka `src/AbsensiSales.Mobile/ViewModels/VerifikasiWajahViewModel.cs`.
   - Pastikan masih memakai method lama. Ini menandakan Fase 3 belum dikerjakan.

### 10.2 Cek build

```powershell
dotnet build src\AbsensiSales.Face\AbsensiSales.Face.csproj
dotnet build AbsensiSales.slnx -m:1
```

### 10.3 Cek runtime (setelah model tersedia)

1. Unduh `version-RFB-320.onnx` dan `face_recognition_sface_2021dec.onnx`.
2. Letakkan di folder `models/` di samping executable Mobile.
3. Buat foto wajah uji.
4. Panggil `BuatTemplate` dari kode uji/console.
5. Panggil `Verifikasi` dengan foto yang sama dan foto orang lain.
6. Periksa:
   - foto yang sama menghasilkan skor tinggi,
   - foto orang lain di bawah threshold,
   - foto tanpa wajah mengembalikan pesan error,
   - foto dua wajah ditolak.

---

## 11. Rencana lanjutan singkat

| Fase | Fokus | Status |
|---|---|---|
| 1 | Satukan engine + API template/verifikasi | **Selesai (library)** |
| 2 | `Sales.FaceData`, halaman pendaftaran, simpan template | Belum |
| 3 | Migrasi UI verifikasi ke engine baru + template sales | Belum |
| 4 | Sinkronisasi backend `wajah.php` + `skor_wajah` ke HR | Belum |
| 5 | Liveness, enkripsi, audit, rate limit | Belum |
| 6 | Unit test, uji perangkat, dokumentasi final | Sebagian (dokumen ini) |

Langkah paling logis berikutnya adalah **Fase 2**: menambahkan `FaceData` ke
`Sales`, membuat halaman pendaftaran wajah, dan menyimpan template hasil
`BuatTemplate` ke database lokal. Setelah itu baru UI verifikasi bisa dimigrasikan
tanpa mengubah alur bisnis terlebih dahulu.
