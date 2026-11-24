# Integrasi Backend PHP → .NET MAUI

Dokumen ini menjelaskan cara menyambungkan aplikasi MAUI ke backend PHP +
database MySQL online (InfinityFree), dan cara memastikan koneksinya berhasil.

---

## 1. File yang sudah dibuat

### Backend (upload ke hosting)

```
backend/
├── api/
│   ├── .htaccess        # meneruskan header Authorization
│   ├── config.php       # ← EDIT: host, database, username, password
│   ├── helpers.php      # header JSON, CORS, fungsi response
│   ├── db.php           # koneksi PDO
│   ├── auth.php         # validasi token Bearer
│   ├── index.php        # info API
│   ├── health.php       # ← cek koneksi backend + database
│   ├── login.php
│   ├── register.php
│   ├── logout.php
│   ├── toko.php         # daftar toko + CRUD toko (khusus HR)
│   ├── absen.php        # check-in / check-out
│   ├── upload.php       # unggah foto bukti kunjungan (Base64 → file)
│   ├── kunjungan.php    # riwayat kunjungan
│   ├── permohonan.php   # izin / cuti + approve/reject HR
│   ├── laporan.php      # laporan HR
│   ├── sales.php        # daftar & kelola sales (khusus HR)
│   ├── penggajian.php   # rekap & proses gaji (khusus HR)
│   └── uploads/         # foto bukti kunjungan (harus writable)
└── database/
    ├── 01_schema.sql             # ← import ini di phpMyAdmin
    ├── 02_optional_columns.sql   # opsional (lihat bagian 9)
    ├── 03_seed_demo.sql          # opsional: toko + akun demo
    └── 04_kolom_admin_hr.sql     # opsional: kolom tambahan panel HR
```

### Aplikasi MAUI

```
src/AbsensiSales.Core/Api/
├── ApiConfig.cs    # ← EDIT: alamat domain backend
├── ApiResult.cs    # pembungkus hasil (sukses/gagal/pesan)
├── ApiModels.cs    # DTO yang namanya sama persis dengan JSON PHP
└── ApiService.cs   # semua pemanggilan endpoint
```

Tombol **"Tes Koneksi Backend"** ditambahkan di halaman login.

---

## 2. URL backend kamu

Alamat backend mengikuti folder tempat file PHP diletakkan:

```
https://DOMAIN-KAMU/api/health.php   -> file ada di htdocs/api
https://DOMAIN-KAMU/health.php       -> file ada di htdocs (root)
```

Base URL yang benar adalah folder yang berisi `login.php` dan `health.php`.
Contoh kalau file ditaruh di root `htdocs`:

```
https://DOMAIN-KAMU/health.php
https://DOMAIN-KAMU/login.php
```

`DOMAIN-KAMU` adalah domain yang diberikan hosting, contoh:

```
https://domainmu.rf.gd/api/health.php
https://domainmu.infinityfreeapp.com/api/health.php
```

### Cara menemukan domain kamu

1. Login ke **https://dash.infinityfree.com**
2. Pilih akun hosting kamu → **Manage**
3. Lihat **Account Information** / **Your Domains**
4. Domain biasanya berakhiran `.rf.gd`, `.wuaze.com`, atau
   `.infinityfreeapp.com`

> **Penting:** folder upload di InfinityFree bernama **`htdocs`**, bukan
> `public_html`. Kalau file ditaruh di `public_html`, hasilnya **404 Not Found**.

---

## 3. Langkah upload

1. **Buat database** (kalau belum)
   - Dash InfinityFree → **MySQL Databases** → Create Database
   - Catat: **Hostname**, **Database name**, **Username**, **Password**

2. **Import tabel**
   - Buka **phpMyAdmin** → pilih database → tab **Import**
   - Upload `backend/database/01_schema.sql` → **Go**
   - Pastikan 11 tabel `tbl_*` muncul (termasuk `tbl_api_token`)
   - Opsional: upload juga `backend/database/03_seed_demo.sql` untuk mengisi
     toko contoh dan akun demo. **Ganti password akun demo** sebelum dipakai
     di server publik.
   - **Wajib kalau login ditolak:** upload `backend/database/05_akun_login.sql`.
     File ini memperbarui password akun demo walaupun emailnya sudah ada
     (seed biasa tidak mengubah password lama).

3. **Upload file PHP**
   - Dash → **File Manager** (atau FTP/FileZilla)
   - Masuk ke **`htdocs`** → buat folder **`api`**
   - Upload **semua isi** `backend/api/` ke dalam `htdocs/api/`
   - Pastikan `.htaccess` juga ikut ter-upload

4. **Edit `config.php`**
   ```php
   'host'     => 'sqlxxx.infinityfree.com',
   'database' => 'if0_xxxxxxx_absensi_sales',
   'username' => 'if0_xxxxxxx',
   'password' => 'GANTI_PASSWORD_DATABASE',
   ```

5. **Aktifkan SSL (disarankan)**
   - Dash → **SSL Certificates** → aktifkan untuk domain kamu
   - Tunggu beberapa menit, lalu gunakan `https://...`

---

## 4. Cara cek backend sudah konek atau belum

### A. Lewat browser (paling cepat)

Buka:

```
https://DOMAIN-KAMU/api/health.php
```

Kalau berhasil, muncul JSON seperti ini:

```json
{
  "success": true,
  "message": "Backend dan database terhubung.",
  "data": {
    "status": "ok",
    "database": "connected",
    "server_time": "2026-11-12 09:30:00",
    "timezone": "Asia/Jakarta",
    "php_version": "8.2.x",
    "mysql_version": "10.6.x-MariaDB",
    "tabel": {
      "tbl_pengguna": true,
      "tbl_toko": true,
      "tbl_sales": true,
      "tbl_hr": true,
      "tbl_kunjungan": true,
      "tbl_permohonan": true,
      "tbl_api_token": true
    },
    "tabel_lengkap": true
  }
}
```

Arti hasilnya:

| Yang terlihat | Arti |
|---|---|
| `"success": true` + `"tabel_lengkap": true` | Backend **dan** database siap dipakai |
| `"success": true` + `"tabel_lengkap": false` | Database konek, tapi ada tabel yang belum dibuat → import `01_schema.sql` |
| `"Koneksi database gagal"` | `config.php` salah (host/nama db/user/password) |
| Halaman **404** | File tidak ada di `htdocs/api`, atau nama folder salah |
| Halaman **500** | Ada error PHP; cek **Error Log** di panel hosting |
| Halaman HTML login hosting | Domain salah, atau file tidak ter-upload |

Cek juga:

```
https://DOMAIN-KAMU/api/
```

Harus muncul daftar endpoint dalam bentuk JSON.

### B. Lewat aplikasi

1. Jalankan aplikasi MAUI
2. Di halaman **Masuk**, klik **"Tes Koneksi Backend"**
3. Di bawah tombol akan muncul salah satu pesan:
   - `Terhubung. Database: connected ... Semua tabel tersedia.` → **berhasil**
   - `Tidak terhubung. Tidak bisa menghubungi server di ...` → URL / internet / SSL
   - `Terhubung, tetapi tabel berikut belum ada: ...` → import SQL
   - `URL backend belum diisi ...` → isi `ApiConfig.cs` (bagian 5)

Alamat yang sedang dipakai juga ditampilkan kecil di bawah tombol
(`Backend: https://.../api`).

### C. Lewat Postman / VS Code REST Client

File contoh sudah tersedia di `backend/api/contoh-request.http`.

Urutan uji:

1. `GET /health.php`
2. `POST /login.php` → salin `data.token`
3. `GET /toko.php` dengan header `Authorization: Bearer <token>`
4. `POST /absen.php` check-in
5. `POST /absen.php` check-out

### D. Kalau hosting mengirim halaman JavaScript

Sebagian hosting gratis (termasuk InfinityFree pada domain tertentu) memasang
proteksi **browser check**. Request dari aplikasi (bukan browser) dibalas halaman
HTML berisi `aes.js` / `slowAES` / cookie `__test`, bukan JSON.

Aplikasi HR sudah menangani ini otomatis lewat `TantanganHosting`: `ApiService`
menghitung cookie `__test`, menyimpannya, lalu mengulang request yang sama.
Kalau hosting tetap menolak, pesan di aplikasi akan menyebut verifikasi
JavaScript — solusi paling stabil adalah memindahkan API ke hosting tanpa
proteksi tersebut.

Catatan: endpoint yang **tidak ada** di server dialihkan InfinityFree ke
`errors.infinityfree.net/errors/404/`. Aplikasi akan menampilkan pesan
`Endpoint '...' tidak ditemukan di server (404)`. Artinya file PHP belum
ter-upload, bukan masalah jaringan.

---

## 5. Isi alamat backend di aplikasi

Buka `src/AbsensiSales.Core/Api/ApiConfig.cs`, ganti satu baris:

```csharp
public const string DefaultBaseUrl = "https://GANTI-DOMAIN";
```

Base URL = folder tempat `login.php` berada, tanpa garis miring di akhir:
- file di `htdocs/api` → `https://DOMAIN-KAMU/api`
- file di root `htdocs` → `https://DOMAIN-KAMU`

Setelah itu build ulang aplikasi.

---

## 6. Cara memakai ApiService

`ApiService` sudah didaftarkan di DI (`MauiProgram.cs`), jadi bisa langsung
di-inject ke ViewModel:

```csharp
public partial class ContohViewModel : ObservableObject
{
    private readonly ApiService _api;

    public ContohViewModel(ApiService api) => _api = api;

    [RelayCommand]
    private async Task LoginAsync()
    {
        var hasil = await _api.LoginAsync("email@contoh.com", "password");

        if (!hasil.Sukses)
        {
            PesanError = hasil.Pesan;   // sudah ramah dibaca
            return;
        }

        // Token tersimpan otomatis di _api.Token
        await _api.GetTokoAsync();
    }
}
```

Contoh unggah foto lalu check-in:

```csharp
// 1. Unggah foto asli (byte[] dari kamera) → server mengembalikan nama file.
var unggah = await _api.UploadFotoAsync(fotoBytes);
if (!unggah.Sukses)
{
    PesanError = unggah.Pesan;
    return;
}

// 2. Simpan check-in dengan nama file hasil unggahan.
var hasil = await _api.CheckInAsync(new CheckInRequest
{
    IdToko = 2,
    LatitudeCheckin = -7.2854,
    LongitudeCheckin = 112.7398,
    FotoBukti = unggah.Data!.File,   // "uploads/kunjungan_...jpg"
    JarakMeter = jarak,
    SkorWajah = skor,
    TujuanKunjungan = "Restock",
    Catatan = "Display rak depan"
});

if (hasil.Sukses)
    IdKunjungan = hasil.Data!.IdKunjungan;
```

Semua method mengembalikan `ApiResult<T>` — **tidak melempar exception** untuk
masalah jaringan, jadi aman dipanggil dari UI.

---

## 7. Daftar endpoint

| Method | Endpoint | Token | Keterangan |
|---|---|---|---|
| GET | `health.php` | tidak | Cek backend + database |
| POST | `login.php` | tidak | Body `{email, password}`; `email` boleh berisi email, nama lengkap, atau bagian sebelum `@` → token |
| POST | `register.php` | tidak | Daftar akun Sales |
| POST | `logout.php` | ya | Hapus token |
| GET | `toko.php` | ya | `?q=` cari, `?id_toko=` detail |
| POST | `toko.php` | ya (HR) | `action=create|update|delete` |
| POST | `upload.php` | ya (Sales) | Body `{data: "<base64>"}` → `{file, url}` foto bukti |
| POST | `absen.php` | ya (Sales) | `action=checkin` / `action=checkout` |
| GET | `kunjungan.php` | ya | `?tanggal=` / `?periode=` / `?status=` / `?id_kunjungan=` / `?id_sales=` |
| GET/POST | `permohonan.php` | ya | Sales: ajukan/lihat; HR: approve/reject |
| GET | `laporan.php` | ya (HR) | `jenis=kunjungan|permohonan|sales` |
| GET | `sales.php` | ya (HR) | `?q=` cari; daftar sales + akunnya |
| POST | `sales.php` | ya (HR) | `action=create|update|delete` — kelola akun sales |
| GET | `penggajian.php` | ya (HR) | `?periode=YYYY-MM` → rekap gaji terhitung |
| POST | `penggajian.php` | ya (HR) | `action=process` → simpan rekap gaji |

Bentuk response selalu sama:

```json
{ "success": true,  "message": "...", "data": { } }
{ "success": false, "message": "...", "code": "KODE_ERROR" }
```

Kode error yang sering muncul:

| Kode | Arti |
|---|---|
| `TOKEN_REQUIRED` | Header `Authorization` belum dikirim |
| `TOKEN_INVALID` | Token salah / kedaluwarsa (24 jam) |
| `INVALID_CREDENTIALS` | Email/username atau password salah |
| `DATA_DUPLICATE` | Email / nomor_sales / nomor_telepon sudah dipakai |
| `ALREADY_CHECKED_IN` | Sudah check-in ke toko itu dan belum check-out |
| `OUTSIDE` (pesan) | Check-in tetap tersimpan tapi lokasi di luar radius |
| `MISSING_PHOTO` / `INVALID_IMAGE` | Body upload kosong atau bukan gambar valid |
| `PHOTO_TOO_LARGE` | Foto melebihi 5 MB |
| `UPLOAD_FAILED` / `UPLOAD_DIR_FAILED` | Folder `uploads/` tidak writable |
| `DB_CONNECTION_ERROR` | `config.php` salah |

---

## 8. Mapping nama kolom (ERD → PHP/C#)

| Lama (mockup SQLite) | Baru (ERD / PHP / DTO C#) |
|---|---|
| `Sales.Id` | `id_pengguna` + `id_sales` |
| `sales_id` / `SalesId` | `id_sales` / `IdSales` |
| `absensi_kunjungan` | `tbl_kunjungan` |
| `tanggal` + `waktu_catat` | `waktu_checkin` |
| `waktu_absensi_awal` | tidak dipakai di ERD |
| `status` (Draf/Tersimpan) | `status_verifikasi` (Pending/Valid/Invalid) |
| `toko.radius_meter` | `tbl_toko.radius` (PHP tetap mengirim alias `radius_meter`) |

Catatan penting:

- Login memakai **`tbl_pengguna.email`** (atau `nama`), bukan tabel `sales`.
- Absensi memakai **`id_sales`**, bukan `pengguna_id`.
- Role yang valid: **`Sales`** dan **`HR`** (huruf besar).
- Status permohonan dari HR: **`Approved` / `Rejected`**, bukan
  `Disetujui` / `Ditolak`.

---

## 9. Kolom opsional

### 9a. Data tambahan kunjungan

ERD hanya menyimpan check-in/check-out. Aplikasi MAUI juga mengirim
`jarak_meter`, `skor_wajah`, `tujuan_kunjungan`, dan `catatan`.

Kalau ingin data itu ikut tersimpan, jalankan sekali di phpMyAdmin:

```sql
-- isi file backend/database/02_optional_columns.sql
```

Kalau tidak dijalankan pun tidak error — `absen.php` otomatis mengabaikan
kolom yang belum ada.

### 9b. Kolom tambahan panel HR

Panel HR menampilkan beberapa data yang tidak ada di ERD inti:

| Tabel | Kolom | Dipakai untuk |
|---|---|---|
| `tbl_toko` | `kode_toko`, `pemilik`, `telepon` | Data master toko |
| `tbl_permohonan` | `lampiran`, `catatan_hr` | Lampiran izin & catatan approval |
| `tbl_pengguna` | `wilayah` | Wilayah kerja sales |

Jalankan sekali di phpMyAdmin:

```sql
-- isi file backend/database/04_kolom_admin_hr.sql
```

Sama seperti kolom kunjungan, semua endpoint PHP mendeteksi kolom ini
secara dinamis (`kolomTabel()`), jadi aplikasi **tetap jalan** walau file
SQL ini belum dijalankan. Bedanya, sebagian informasi akan tampil kosong
(mis. kode toko memakai fallback `TK-{id}`).

---

## 10. Aplikasi HR (desktop)

Aplikasi `AbsensiSales.HR` sekarang **hanya memakai API** — tidak lagi
membuka SQLite, tidak ada `BuatDataMock`, dan semua layar mengambil data
dari backend MySQL.

### 10a. Login HR

Gunakan akun HR yang sudah dibuat di database (hasil `03_seed_demo.sql` atau
akun buatan sendiri). Isi kredensial sesuai database masing-masing.

Login mengirim `POST login.php`. Kalau berhasil, token disimpan di
`ApiService` dan identitas HR (nama, email, role) disimpan di
`SessionService`. Akun dengan role selain `HR` akan ditolak.

### 10b. Data yang ditampilkan tiap halaman

| Halaman | Endpoint yang dipakai |
|---|---|
| Dashboard | `sales.php`, `kunjungan.php?tanggal=`, `permohonan.php?status=Pending` |
| Pemantauan absensi | `sales.php`, `kunjungan.php?tanggal=` |
| Laporan & detail kunjungan | `laporan.php`, `kunjungan.php?id_kunjungan=` |
| Perizinan / approval | `permohonan.php` (GET + POST approve/reject) |
| Data master | `sales.php`, `toko.php` (GET + POST) |
| Payroll | `penggajian.php` (GET + POST `process`) |
| Logout | `logout.php` |

### 10c. Mengelola akun sales dari HR

Form **Data Master → Sales** mengirim `POST sales.php`:

- `action=create` — field wajib: `nama`, `email`, `password` (min. 6 karakter),
  `nomor_sales`, dan `nomor_telepon`. `wilayah` opsional (butuh kolom 9b).
- `action=update` — `id_sales` + field di atas. `password` boleh dikosongkan
  bila tidak ingin mengubah kata sandi.
- `action=delete` — cukup `id_sales`. Akun pengguna ikut terhapus sehingga
  kunjungan, permohonan, dan penggajian sales tersebut terhapus lewat cascade.

Data Master → Toko memakai `toko.php` dengan pola yang sama
(`action=create|update|delete`). Menghapus toko juga menghapus kunjungan
yang tercatat di toko itu.

### 10d. Payroll

`penggajian.php` menghitung gaji dari:

- `tbl_pengaturan_gaji` (gaji pokok + insentif per kunjungan),
- kunjungan berstatus `Valid` pada periode tersebut,
- izin/cuti berstatus `Approved` sebagai potongan.

Nilai acuan: **22 hari kerja** dan **176 target kunjungan per bulan**.
Tombol **Setujui & simpan rekap** memanggil `POST penggajian.php`
`{action:"process"}`, yang menulis ke `tbl_penggajian`. Slip PDF memakai
nominal asli dari response API.

> Kalau `tbl_pengaturan_gaji` masih kosong, halaman payroll akan
> menampilkan pesan `PENGATURAN_GAJI_KOSONG`. Isi minimal satu baris
> pengaturan gaji lebih dulu.

### 10e. Bukti kunjungan di halaman detail

Detail kunjungan menampilkan data apa adanya dari `kunjungan.php?id_kunjungan=`:

- **Foto toko** diambil dari kolom `foto_bukti` lalu digabung dengan
  `ApiConfig.BaseUrl` (mis. `.../uploads/kunjungan_xxx.jpg`). Sebelum
  ditampilkan, aplikasi memeriksa dulu apakah URL benar-benar mengembalikan
  gambar; kalau kolom kosong atau berkasnya 404, layar menampilkan kotak
  "Foto belum tersedia", bukan gambar rusak/contoh.
- **Verifikasi wajah** hanya menampilkan skor (`skor_wajah`) karena aplikasi
  memang tidak menyimpan berkas foto wajah. Tidak ada kotak "WAJAH"/"TOKO"
  palsu lagi.
- Kalau backend belum mengirim `foto_bukti`, jalankan `upload.php` di hosting
  (lihat bagian 11c) lalu lakukan satu check-in dari Mobile agar foto tersimpan.

Tidak ada lagi nilai contoh di layar HR: nama akun, koordinat toko, jabatan,
dan foto semuanya berasal dari sesi login atau response API. Placeholder pada
form hanya petunjuk format input (mis. "nama@perusahaan.com"), bukan data.

Database yang sudah lama di-seed mungkin masih menyimpan nama berkas palsu
`contoh_bukti.jpg` (dari `03_seed_demo.sql` versi lama). Seed baru sudah
memakai `NULL`; untuk database yang sudah berjalan, jalankan sekali
`backend/database/06_perbaikan_bukti_contoh.sql` agar referensi itu dibersihkan.

---

## 11. Aplikasi Mobile

Aplikasi Mobile sekarang **sepenuhnya memakai API** sebagai sumber data. SQLite
lokal tidak lagi dibuka, tidak di-seed, dan repository lokal tidak didaftarkan
di DI (`MauiProgram.cs`). Semua daftar dan detail berasal dari MySQL melalui
endpoint PHP.

### 11a. Alur data tiap layar

| Layar | Endpoint yang dipakai |
|---|---|
| Login | `login.php` (email, nama lengkap, atau bagian sebelum `@`) |
| Registrasi | `register.php` |
| Absensi pertama | `kunjungan.php?tanggal=` + `Geolocation` perangkat |
| Pengisian data toko | `toko.php` + `Geolocation` perangkat untuk hitung jarak/radius |
| Verifikasi wajah | Model SFace lokal (lihat catatan di bawah) |
| Foto toko | Kamera perangkat, disimpan sebagai byte di alur |
| Verifikasi & simpan | `upload.php` lalu `absen.php` (`action=checkin`) |
| Laporan kunjungan | `kunjungan.php?tanggal=hari-ini` |
| Pengajuan izin | `permohonan.php` (POST) |
| Status permohonan | `permohonan.php` (GET) |

### 11b. GPS asli, bukan simulasi

`GpsService` memakai `Geolocation.Default.GetLocationAsync` dengan akurasi
tinggi. Kalau izin lokasi ditolak atau GPS mati, aplikasi menampilkan pesan dan
pengguna **tidak bisa melanjutkan** absensi. Tidak ada koordinat cadangan atau
posisi simulasi.

### 11c. Foto bukti asli

`FotoTokoViewModel` wajib mengambil foto dari kamera (tidak ada placeholder).
Saat verifikasi, `VerifikasiDataViewModel`:

1. mengirim byte foto ke `upload.php` sebagai Base64;
2. menerima nama file (`uploads/kunjungan_<id>_<waktu>_<acak>.jpg`);
3. mengirim nama file itu sebagai `foto_bukti` pada `absen.php`.

Pastikan folder `backend/api/uploads/` ada dan **writable** di hosting. Format
yang diterima: JPEG, PNG, WEBP, maksimal 5 MB.

### 11d. Verifikasi wajah

Verifikasi wajah tetap berjalan lokal memakai model SFace + foto referensi di
folder `reference_faces/`. Kalau model atau foto referensi belum tersedia,
`FaceVerificationService` memakai **mode simulasi** (dianggap cocok dengan skor
tinggi). Ini bukan data dummy dari database, tetapi perlu diperhatikan saat
sidang/demo: tanpa model, verifikasi tidak benar-benar membandingkan wajah.

---

## 12. Masalah umum di Android

- **`Cleartext HTTP traffic not permitted`**
  Manifest Mobile sudah diberi `android:usesCleartextTraffic="true"` untuk
  masa uji coba. Untuk produksi, pakai HTTPS lalu hapus atribut itu.
- **Emulator tidak bisa akses `localhost`**
  Kalau backend dijalankan di komputer sendiri, gunakan `10.0.2.2`, bukan
  `localhost`. Untuk backend InfinityFree (online), tidak masalah.
- **HP dan komputer harus punya internet** — backend online bisa diakses
  dari jaringan mana pun.

---

## 13. Checklist cepat

- [ ] Database dibuat di InfinityFree
- [ ] `01_schema.sql` di-import, 11 tabel muncul
- [ ] (Disarankan) `02_optional_columns.sql` di-import
- [ ] (Disarankan) `04_kolom_admin_hr.sql` di-import
- [ ] Folder `htdocs/api` berisi semua file PHP + `.htaccess`
- [ ] Folder `htdocs/api/uploads` ada dan writable
- [ ] `config.php` sudah diisi kredensial yang benar
- [ ] `https://DOMAIN/api/health.php` menampilkan `success: true`
- [ ] `ApiConfig.cs` diisi `https://DOMAIN/api`
- [ ] Tombol **Tes Koneksi Backend** di aplikasi Mobile menampilkan "Terhubung"
- [ ] Login Mobile dengan akun sales yang dibuat berhasil
- [ ] Login HR dengan akun HR yang dibuat berhasil
- [ ] Daftar toko, laporan, dan status izin Mobile berisi data dari database
- [ ] Check-in Mobile menghasilkan baris baru di `tbl_kunjungan` dan file di `uploads/`
- [ ] Setiap halaman HR menampilkan data nyata (bukan data contoh)
- [ ] Detail kunjungan HR menampilkan foto bukti asli dari `uploads/`; bila
      tidak ada, muncul "Foto belum tersedia" (bukan kotak gambar rusak)
