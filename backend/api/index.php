<?php
/**
 * index.php — halaman info API.
 * Buka https://DOMAIN-KAMU/api/ di browser untuk memastikan folder ter-upload.
 */

declare(strict_types=1);

require_once __DIR__ . '/helpers.php';

sukses([
    'nama'      => 'AbsensiSales API',
    'versi'     => '1.0',
    'cek_koneksi' => 'health.php',
    'endpoint'  => [
        'GET  health.php'   => 'Cek koneksi backend + database (tanpa token)',
        'POST login.php'    => 'Login, dapat token',
        'POST register.php' => 'Daftar akun sales',
        'POST logout.php'   => 'Hapus token',
        'GET  toko.php'     => 'Daftar toko',
        'POST absen.php'    => 'Check-in / check-out',
        'GET  kunjungan.php'=> 'Riwayat kunjungan',
        'GET/POST permohonan.php' => 'Izin / cuti',
        'GET  laporan.php'  => 'Laporan HR',
    ],
], 'AbsensiSales API aktif.');
