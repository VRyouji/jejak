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
