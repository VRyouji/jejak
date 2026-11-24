-- ============================================================
-- SETUP SEMUA (satu kali import)
--
-- Jalankan file INI SAJA di phpMyAdmin (tab Import / SQL).
-- Isinya gabungan:
--   01_schema.sql            -> membuat tabel
--   02_optional_columns.sql  -> kolom tambahan kunjungan
--   04_kolom_admin_hr.sql    -> kolom tambahan panel HR
--   03_seed_demo.sql         -> akun demo, 8 toko, pengaturan gaji
--   05_akun_login.sql        -> akun demo HR + Sales (password diperbarui)
--
-- Akun setelah import:
--   HR    : hr@example.com    / hr123456
--   Sales : andi@example.com  / demo123
--   Sales : sales@example.com / sales123
--
-- Aman dijalankan berulang.
-- ============================================================

-- ============================================================
-- ISI: 01_schema.sql
-- ============================================================

-- ============================================================
-- Skema database AbsensiSales (sesuai ERD tbl_*)
-- Jalankan di phpMyAdmin InfinityFree, SETELAH database dibuat
-- dari panel (InfinityFree tidak mengizinkan CREATE DATABASE).
--
-- Cara pakai:
--   1. Buka phpMyAdmin -> pilih database kamu
--   2. Tab "Import" -> pilih file ini -> Go
--   (atau copy-paste isi file ke tab SQL)
-- ============================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- 1. Pengguna (akun login)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_pengguna (
    id_pengguna INT AUTO_INCREMENT PRIMARY KEY,
    nama VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role ENUM('Sales', 'HR') NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 2. Toko
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_toko (
    id_toko INT AUTO_INCREMENT PRIMARY KEY,
    nama_toko VARCHAR(255) NOT NULL,
    alamat TEXT,
    latitude DOUBLE,
    longitude DOUBLE,
    radius DOUBLE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 3. Pengaturan gaji
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_pengaturan_gaji (
    id_pengaturan INT AUTO_INCREMENT PRIMARY KEY,
    gaji_pokok DECIMAL(15,2) NOT NULL,
    insentif_per_kunjungan DECIMAL(15,2) NOT NULL,
    periode_berlaku DATE UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 4. Profil sales (terkait tbl_pengguna)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_sales (
    id_sales INT AUTO_INCREMENT PRIMARY KEY,
    id_pengguna INT UNIQUE NOT NULL,
    nomor_sales VARCHAR(50) UNIQUE NOT NULL,
    nomor_telepon VARCHAR(20) UNIQUE NOT NULL,
    face_data TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_sales_pengguna FOREIGN KEY (id_pengguna)
        REFERENCES tbl_pengguna(id_pengguna) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 5. Profil HR (terkait tbl_pengguna)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_hr (
    id_hr INT AUTO_INCREMENT PRIMARY KEY,
    id_pengguna INT UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_hr_pengguna FOREIGN KEY (id_pengguna)
        REFERENCES tbl_pengguna(id_pengguna) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 6. Kunjungan / absensi
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_kunjungan (
    id_kunjungan INT AUTO_INCREMENT PRIMARY KEY,
    id_sales INT NOT NULL,
    id_toko INT NOT NULL,
    waktu_checkin DATETIME NOT NULL,
    waktu_checkout DATETIME NULL,
    durasi_menit INT NULL,
    latitude_checkin DOUBLE,
    longitude_checkin DOUBLE,
    latitude_checkout DOUBLE,
    longitude_checkout DOUBLE,
    foto_bukti VARCHAR(255),
    status_verifikasi ENUM('Pending', 'Valid', 'Invalid') NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_kunjungan_sales FOREIGN KEY (id_sales)
        REFERENCES tbl_sales(id_sales) ON DELETE CASCADE,
    CONSTRAINT fk_kunjungan_toko FOREIGN KEY (id_toko)
        REFERENCES tbl_toko(id_toko) ON DELETE CASCADE,
    INDEX idx_kunjungan_sales (id_sales),
    INDEX idx_kunjungan_toko (id_toko),
    INDEX idx_kunjungan_checkin (waktu_checkin)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 7. Permohonan (izin / cuti)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_permohonan (
    id_permohonan INT AUTO_INCREMENT PRIMARY KEY,
    id_sales INT NOT NULL,
    jenis ENUM('Cuti', 'Izin Sakit') NOT NULL,
    tanggal_mulai DATE NOT NULL,
    tanggal_selesai DATE NOT NULL,
    alasan TEXT,
    status ENUM('Pending', 'Approved', 'Rejected') NOT NULL DEFAULT 'Pending',
    id_hr_approver INT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_permohonan_sales FOREIGN KEY (id_sales)
        REFERENCES tbl_sales(id_sales) ON DELETE CASCADE,
    CONSTRAINT fk_permohonan_hr FOREIGN KEY (id_hr_approver)
        REFERENCES tbl_hr(id_hr) ON DELETE SET NULL,
    INDEX idx_permohonan_sales (id_sales),
    INDEX idx_permohonan_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 8. Penggajian
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_penggajian (
    id_gaji INT AUTO_INCREMENT PRIMARY KEY,
    id_sales INT NOT NULL,
    id_hr INT NOT NULL,
    id_pengaturan INT NOT NULL,
    periode VARCHAR(10) NOT NULL,
    total_kunjungan_valid INT NOT NULL DEFAULT 0,
    gaji_pokok DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    total_insentif DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    total_potongan DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    total_gaji DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    tanggal_proses DATETIME NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_gaji_sales FOREIGN KEY (id_sales)
        REFERENCES tbl_sales(id_sales) ON DELETE CASCADE,
    CONSTRAINT fk_gaji_hr FOREIGN KEY (id_hr)
        REFERENCES tbl_hr(id_hr) ON DELETE RESTRICT,
    CONSTRAINT fk_gaji_pengaturan FOREIGN KEY (id_pengaturan)
        REFERENCES tbl_pengaturan_gaji(id_pengaturan) ON DELETE RESTRICT,
    UNIQUE KEY uk_gaji_sales_periode (id_sales, periode),
    INDEX idx_gaji_periode (periode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 9. Laporan
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_laporan (
    id_laporan INT AUTO_INCREMENT PRIMARY KEY,
    id_hr INT NOT NULL,
    jenis_laporan VARCHAR(100) NOT NULL,
    periode VARCHAR(10) NOT NULL,
    tanggal_cetak DATETIME NULL,
    file_laporan VARCHAR(255) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_laporan_hr FOREIGN KEY (id_hr)
        REFERENCES tbl_hr(id_hr) ON DELETE CASCADE,
    INDEX idx_laporan_hr (id_hr)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 10. Log verifikasi
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_log_verifikasi (
    id_log INT AUTO_INCREMENT PRIMARY KEY,
    id_kunjungan INT NOT NULL,
    id_hr INT NOT NULL,
    status_verifikasi ENUM('Pending', 'Valid', 'Invalid') NOT NULL,
    catatan TEXT,
    tanggal_verifikasi DATETIME NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_log_kunjungan FOREIGN KEY (id_kunjungan)
        REFERENCES tbl_kunjungan(id_kunjungan) ON DELETE CASCADE,
    CONSTRAINT fk_log_hr FOREIGN KEY (id_hr)
        REFERENCES tbl_hr(id_hr) ON DELETE CASCADE,
    INDEX idx_log_kunjungan (id_kunjungan),
    INDEX idx_log_hr (id_hr)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ------------------------------------------------------------
-- 11. Token API (tambahan, dibutuhkan login)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tbl_api_token (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_pengguna INT NOT NULL,
    token CHAR(64) NOT NULL UNIQUE,
    expires_at DATETIME NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_token_pengguna FOREIGN KEY (id_pengguna)
        REFERENCES tbl_pengguna(id_pengguna) ON DELETE CASCADE,
    INDEX idx_token_value (token)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;


-- ============================================================
-- ISI: 02_optional_columns.sql
-- ============================================================

-- ============================================================
-- OPSIONAL â€” kolom tambahan untuk data yang dikirim aplikasi MAUI
--
-- ERD hanya menyimpan check-in/check-out. Aplikasi MAUI juga
-- mengirim jarak GPS, skor wajah, tujuan kunjungan, dan catatan.
-- Jalankan file ini kalau kamu ingin data tersebut ikut tersimpan.
--
-- File absen.php otomatis mendeteksi kolom ini: kalau belum ada,
-- data tambahan diabaikan (tidak error). Jadi file ini opsional.
-- ============================================================

ALTER TABLE tbl_kunjungan
    ADD COLUMN IF NOT EXISTS jarak_meter DECIMAL(10,2) NULL AFTER longitude_checkin,
    ADD COLUMN IF NOT EXISTS skor_wajah DECIMAL(5,2) NULL AFTER jarak_meter,
    ADD COLUMN IF NOT EXISTS tujuan_kunjungan VARCHAR(255) NULL AFTER skor_wajah,
    ADD COLUMN IF NOT EXISTS catatan TEXT NULL AFTER tujuan_kunjungan;


-- ============================================================
-- ISI: 04_kolom_admin_hr.sql
-- ============================================================

-- ============================================================
-- Kolom tambahan untuk panel admin HR (opsional tapi disarankan).
--
-- ERD hanya memuat kolom inti. Panel HR menampilkan/mengirim beberapa
-- data tambahan (kode toko, pemilik, telepon, lampiran izin, catatan HR,
-- wilayah sales). Tanpa kolom ini aplikasi tetap jalan, tetapi sebagian
-- informasi akan kosong.
--
-- Jalankan SEKALI di phpMyAdmin (MariaDB mendukung IF NOT EXISTS).
-- ============================================================

-- Identitas tambahan toko
ALTER TABLE tbl_toko
    ADD COLUMN IF NOT EXISTS kode_toko VARCHAR(20) NULL AFTER id_toko,
    ADD COLUMN IF NOT EXISTS pemilik VARCHAR(100) NULL AFTER alamat,
    ADD COLUMN IF NOT EXISTS telepon VARCHAR(20) NULL AFTER pemilik;

-- Lampiran & catatan HR pada permohonan
ALTER TABLE tbl_permohonan
    ADD COLUMN IF NOT EXISTS lampiran VARCHAR(255) NULL AFTER alasan,
    ADD COLUMN IF NOT EXISTS catatan_hr TEXT NULL AFTER status;

-- Wilayah kerja sales
ALTER TABLE tbl_pengguna
    ADD COLUMN IF NOT EXISTS wilayah VARCHAR(100) NULL AFTER role;


-- ============================================================
-- ISI: 03_seed_demo.sql
-- ============================================================

-- ============================================================
-- Data contoh (opsional, tapi sangat membantu saat uji coba)
--
-- Jalankan di phpMyAdmin SETELAH 01_schema.sql.
-- Aman dijalankan lebih dari sekali (tidak menggandakan data).
--
-- Akun hasil seed:
--   Sales : andi@example.com / demo123
--   HR    : hr@example.com   / hr123456
-- ============================================================

-- ------------------------------------------------------------
-- Akun Sales: Andi Pratama
-- ------------------------------------------------------------
INSERT INTO tbl_pengguna (nama, email, password_hash, role)
VALUES (
    'Andi Pratama',
    'andi@example.com',
    '$2y$10$Tqu9lKZddwuc42BlHFC3JeiZprZQG1Ei4xHQuFvivoWoGfUwroCGm',
    'Sales'
)
ON DUPLICATE KEY UPDATE id_pengguna = LAST_INSERT_ID(id_pengguna);
SET @id_pengguna_andi = LAST_INSERT_ID();

INSERT INTO tbl_sales (id_pengguna, nomor_sales, nomor_telepon)
VALUES (@id_pengguna_andi, 'SLS-014', '081234567801')
ON DUPLICATE KEY UPDATE id_sales = LAST_INSERT_ID(id_sales);
SET @id_sales_andi = LAST_INSERT_ID();

-- ------------------------------------------------------------
-- Akun HR
-- ------------------------------------------------------------
INSERT INTO tbl_pengguna (nama, email, password_hash, role)
VALUES (
    'Rina HR',
    'hr@example.com',
    '$2y$10$P21EKnbczHSGZAHhD/2X3OUFoiA5RaXWn74VHUtTJiXl9z/1TgeR6',
    'HR'
)
ON DUPLICATE KEY UPDATE id_pengguna = LAST_INSERT_ID(id_pengguna);
SET @id_pengguna_hr = LAST_INSERT_ID();

INSERT INTO tbl_hr (id_pengguna)
VALUES (@id_pengguna_hr)
ON DUPLICATE KEY UPDATE id_hr = LAST_INSERT_ID(id_hr);
SET @id_hr = LAST_INSERT_ID();

-- ------------------------------------------------------------
-- Toko contoh (koordinat Surabaya)
-- ------------------------------------------------------------
SET @jumlah_toko = (SELECT COUNT(*) FROM tbl_toko);

INSERT INTO tbl_toko (nama_toko, alamat, latitude, longitude, radius)
SELECT * FROM (
    SELECT 'Toko Sumber Jaya'     AS nama_toko, 'Jl. Raya Darmo No. 24, Surabaya' AS alamat, -7.285400 AS latitude, 112.739800 AS longitude, 100.0 AS radius
    UNION ALL SELECT 'Toko Makmur',         'Jl. Diponegoro No. 18, Surabaya', -7.271000, 112.749000, 100.0
    UNION ALL SELECT 'Toko Berkah',         'Jl. Rungkut Asri Tengah No. 5, Surabaya', -7.334000, 112.790000, 100.0
    UNION ALL SELECT 'Toko Sentosa',        'Jl. Kenanga No. 3, Surabaya', -7.258000, 112.728000, 100.0
    UNION ALL SELECT 'Toko Sejahtera',      'Jl. Margorejo No. 88, Surabaya', -7.321000, 112.765000, 100.0
    UNION ALL SELECT 'Toko Maju Bersama',   'Jl. Kartini No. 7, Surabaya', -7.249000, 112.743000, 100.0
    UNION ALL SELECT 'Toko Barokah',        'Jl. Dharmahusada No. 41, Surabaya', -7.295000, 112.781000, 100.0
    UNION ALL SELECT 'Toko Wahyu Cell',     'Jl. Petemon Pandean No. 12, Surabaya', -7.240000, 112.712000, 100.0
) AS seed
WHERE @jumlah_toko = 0;

-- ------------------------------------------------------------
-- Pengaturan gaji awal
-- ------------------------------------------------------------
SET @jumlah_pengaturan = (SELECT COUNT(*) FROM tbl_pengaturan_gaji);

INSERT INTO tbl_pengaturan_gaji (gaji_pokok, insentif_per_kunjungan, periode_berlaku)
SELECT 4500000.00, 25000.00, '2026-01-01'
FROM DUAL
WHERE @jumlah_pengaturan = 0;

-- ------------------------------------------------------------
-- Catatan
-- ------------------------------------------------------------
-- Seed ini sengaja TIDAK membuat kunjungan atau permohonan contoh.
-- Data absensi & perizinan harus berasal dari aplikasi/backoffice,
-- supaya layar HR hanya menampilkan data nyata.


-- ============================================================
-- ISI: 05_akun_login.sql
-- ============================================================

-- ============================================================
-- Perbaikan akun login (aman dijalankan berulang)
--
-- Jalankan di phpMyAdmin SETELAH:
--   1. 01_schema.sql
--   2. 02_optional_columns.sql (kalau belum)
--   3. 04_kolom_admin_hr.sql
--
-- Kenapa file ini ada?
--   03_seed_demo.sql memakai ON DUPLICATE KEY UPDATE yang TIDAK mengubah
--   password kalau email sudah pernah ada. Akibatnya akun hr@example.com
--   bisa tetap memakai password lama, dan aplikasi menampilkan
--   "Email atau password salah" (INVALID_CREDENTIALS).
--
-- File ini memaksa nama, password, dan role ikut diperbarui.
--
-- Akun setelah menjalankan file ini:
--   HR    : hr@example.com    / hr123456
--   Sales : andi@example.com  / demo123
--   Sales : sales@example.com / sales123
-- ============================================================

SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- Akun HR
-- ------------------------------------------------------------
INSERT INTO tbl_pengguna (nama, email, password_hash, role)
VALUES (
    'Rina HR',
    'hr@example.com',
    '$2y$10$P21EKnbczHSGZAHhD/2X3OUFoiA5RaXWn74VHUtTJiXl9z/1TgeR6',
    'HR'
)
ON DUPLICATE KEY UPDATE
    nama          = VALUES(nama),
    password_hash = VALUES(password_hash),
    role          = VALUES(role);

SET @id_pengguna_hr := (
    SELECT id_pengguna FROM tbl_pengguna WHERE email = 'hr@example.com' LIMIT 1
);

INSERT INTO tbl_hr (id_pengguna)
VALUES (@id_pengguna_hr)
ON DUPLICATE KEY UPDATE id_hr = LAST_INSERT_ID(id_hr);

-- ------------------------------------------------------------
-- Akun Sales demo
-- ------------------------------------------------------------
INSERT INTO tbl_pengguna (nama, email, password_hash, role)
VALUES (
    'Andi Pratama',
    'andi@example.com',
    '$2y$10$Tqu9lKZddwuc42BlHFC3JeiZprZQG1Ei4xHQuFvivoWoGfUwroCGm',
    'Sales'
)
ON DUPLICATE KEY UPDATE
    nama          = VALUES(nama),
    password_hash = VALUES(password_hash),
    role          = VALUES(role);

SET @id_pengguna_andi := (
    SELECT id_pengguna FROM tbl_pengguna WHERE email = 'andi@example.com' LIMIT 1
);

INSERT INTO tbl_sales (id_pengguna, nomor_sales, nomor_telepon)
VALUES (@id_pengguna_andi, 'SLS-014', '081234567801')
ON DUPLICATE KEY UPDATE
    nomor_sales   = VALUES(nomor_sales),
    nomor_telepon = VALUES(nomor_telepon);

-- ------------------------------------------------------------
-- Akun Sales tambahan (untuk uji login sisi Sales)
-- ------------------------------------------------------------
INSERT INTO tbl_pengguna (nama, email, password_hash, role)
VALUES (
    'Sales Satu',
    'sales@example.com',
    '$2y$10$TuUNDosgGSAAbqES2UbOzey60H4NCX7/FFPZoaF5tKMZsgAFkekS6',
    'Sales'
)
ON DUPLICATE KEY UPDATE
    nama          = VALUES(nama),
    password_hash = VALUES(password_hash),
    role          = VALUES(role);

SET @id_pengguna_sales := (
    SELECT id_pengguna FROM tbl_pengguna WHERE email = 'sales@example.com' LIMIT 1
);

INSERT INTO tbl_sales (id_pengguna, nomor_sales, nomor_telepon)
VALUES (@id_pengguna_sales, 'SLS-101', '081234567810')
ON DUPLICATE KEY UPDATE
    nomor_sales   = VALUES(nomor_sales),
    nomor_telepon = VALUES(nomor_telepon);

-- ------------------------------------------------------------
-- Verifikasi akun demo
-- ------------------------------------------------------------
SELECT
    p.id_pengguna,
    p.nama,
    p.email,
    p.role,
    (h.id_hr IS NOT NULL)    AS punya_profil_hr,
    (s.id_sales IS NOT NULL) AS punya_profil_sales
FROM tbl_pengguna p
LEFT JOIN tbl_hr h ON h.id_pengguna = p.id_pengguna
LEFT JOIN tbl_sales s ON s.id_pengguna = p.id_pengguna
WHERE p.email IN ('hr@example.com', 'andi@example.com', 'sales@example.com')
ORDER BY p.role, p.email;
