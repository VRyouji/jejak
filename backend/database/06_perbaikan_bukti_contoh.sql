-- ------------------------------------------------------------
-- 06_perbaikan_bukti_contoh.sql
--
-- Seed lama menyimpan nama berkas contoh ('contoh_bukti.jpg') yang
-- tidak pernah ada di folder uploads, sehingga halaman detail HR
-- menampilkan kotak foto kosong. Skrip ini membersihkan referensi
-- palsu tersebut pada database yang sudah terlanjur di-seed.
--
-- Jalankan sekali lewat phpMyAdmin / MySQL client. Aman diulang.
-- ------------------------------------------------------------

UPDATE tbl_kunjungan
SET foto_bukti = NULL
WHERE foto_bukti = 'contoh_bukti.jpg';

-- Cek hasilnya:
-- SELECT id_kunjungan, nama_toko, foto_bukti
-- FROM tbl_kunjungan
-- ORDER BY id_kunjungan;
