-- ============================================================
-- OPSIONAL — kolom tambahan untuk data yang dikirim aplikasi MAUI
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
