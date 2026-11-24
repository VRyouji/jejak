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
--   HR    : hr@example.com    / ********
--   Sales : andi@example.com  / ********
--   Sales : sales@example.com / ********
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
