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
