<?php
/**
 * health.php — CEK KONEKSI BACKEND + DATABASE
 *
 * Buka di browser:
 *   https://DOMAIN-KAMU/api/health.php
 *
 * Kalau berhasil, muncul JSON:
 *   {
 *     "success": true,
 *     "message": "Backend dan database terhubung.",
 *     "data": { "status": "ok", "database": "connected", ... }
 *   }
 *
 * Endpoint ini TIDAK butuh token, jadi paling enak dipakai untuk
 * memastikan URL, upload file, dan config.php sudah benar.
 */

declare(strict_types=1);

require_once __DIR__ . '/db.php';

$tabelWajib = [
    'tbl_pengguna',
    'tbl_toko',
    'tbl_sales',
    'tbl_hr',
    'tbl_kunjungan',
    'tbl_permohonan',
    'tbl_api_token',
];

$statusTabel = [];
$semuaAda = true;

foreach ($tabelWajib as $tabel) {
    $stmt = $pdo->prepare(
        "SELECT COUNT(*) AS jumlah
         FROM information_schema.tables
         WHERE table_schema = DATABASE() AND table_name = ?"
    );
    $stmt->execute([$tabel]);
    $ada = (int)$stmt->fetchColumn() > 0;

    $statusTabel[$tabel] = $ada;
    if (!$ada) {
        $semuaAda = false;
    }
}

$versiMysql = (string)$pdo->query('SELECT VERSION()')->fetchColumn();

sukses([
    'status'       => 'ok',
    'database'     => 'connected',
    'server_time'  => date('Y-m-d H:i:s'),
    'timezone'     => date_default_timezone_get(),
    'php_version'  => PHP_VERSION,
    'mysql_version'=> $versiMysql,
    'tabel'        => $statusTabel,
    'tabel_lengkap'=> $semuaAda,
], $semuaAda
    ? 'Backend dan database terhubung.'
    : 'Database terhubung, tetapi ada tabel yang belum dibuat. Import 01_schema.sql.'
);
