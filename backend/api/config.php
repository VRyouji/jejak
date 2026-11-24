<?php
/**
 * config.php — PUSAT KONFIGURASI BACKEND
 *
 * GANTI 4 nilai di bawah dengan data dari panel InfinityFree:
 *   Client Area -> Manage -> MySQL Databases
 *
 * Contoh yang tampil di InfinityFree:
 *   Hostname : sqlxxx.infinityfree.com
 *   Database : if0_xxxxxxx_absensi_sales
 *   Username : if0_xxxxxxx
 *   Password : (password yang kamu buat saat membuat database)
 */

return [
    'host'     => 'sqlxxx.infinityfree.com',
    'database' => 'if0_xxxxxxx_absensi_sales',
    'username' => 'if0_xxxxxxx',
    'password' => 'GANTI_PASSWORD_DATABASE',

    // Nama tabel token (biasanya tidak perlu diubah)
    'token_table' => 'tbl_api_token',

    // Lama token login berlaku (jam)
    'token_lifetime_hours' => 24,
];
