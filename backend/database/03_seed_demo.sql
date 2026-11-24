-- ============================================================
-- Data contoh (opsional, tapi sangat membantu saat uji coba)
--
-- Jalankan di phpMyAdmin SETELAH 01_schema.sql.
-- Aman dijalankan lebih dari sekali (tidak menggandakan data).
--
-- Isi seed: akun demo, 8 toko contoh, dan pengaturan gaji awal.
-- Seed ini TIDAK membuat kunjungan/permohonan contoh.
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
