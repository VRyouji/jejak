<?php
/**
 * upload.php — simpan foto bukti kunjungan (khusus Sales).
 *
 * POST { "data": "<base64 JPEG/PNG>" }
 *
 * Response sukses:
 * {
 *   "success": true,
 *   "message": "Foto bukti tersimpan.",
 *   "data": { "file": "uploads/kunjungan_1_20261119_081500.jpg",
 *             "url": "https://domain/uploads/..." }
 * }
 *
 * Nama file hasil response dikirim ke absen.php sebagai "foto_bukti".
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

wajibMethod('POST');
$pengguna = wajibLogin($pdo);
wajibRole($pengguna, 'Sales');

if ($pengguna['id_sales'] === null) {
    gagal('Profil sales tidak ditemukan untuk akun ini.', 'SALES_PROFILE_NOT_FOUND', 400);
}

$data = inputJson();
$base64 = (string)(ambil($data, 'data') ?? '');

if ($base64 === '') {
    gagal('Data foto wajib dikirim.', 'MISSING_PHOTO', 400);
}

// Buang prefix data URI kalau ada ("data:image/jpeg;base64,...").
if (str_contains($base64, ',')) {
    $base64 = substr($base64, strpos($base64, ',') + 1);
}

$biner = base64_decode($base64, true);
if ($biner === false || $biner === '') {
    gagal('Data foto bukan Base64 yang valid.', 'INVALID_PHOTO_DATA', 400);
}

$maksByte = 5 * 1024 * 1024;
if (strlen($biner) > $maksByte) {
    gagal('Ukuran foto terlalu besar (maksimal 5 MB).', 'PHOTO_TOO_LARGE', 413);
}

$info = @getimagesizefromstring($biner);
if ($info === false) {
    gagal('Berkas yang dikirim bukan gambar yang valid.', 'INVALID_IMAGE', 400);
}

$mime = (string)($info['mime'] ?? '');
$ekstensi = match ($mime) {
    'image/jpeg' => 'jpg',
    'image/png'  => 'png',
    'image/webp' => 'webp',
    default      => null,
};

if ($ekstensi === null) {
    gagal('Format foto harus JPEG, PNG, atau WEBP.', 'UNSUPPORTED_IMAGE', 400);
}

$folder = __DIR__ . '/uploads';
if (!is_dir($folder) && !@mkdir($folder, 0755, true) && !is_dir($folder)) {
    gagal('Folder penyimpanan foto tidak dapat dibuat.', 'UPLOAD_DIR_FAILED', 500);
}

$namaFile = sprintf(
    'kunjungan_%d_%s_%s.%s',
    (int)$pengguna['id_sales'],
    date('Ymd_His'),
    bin2hex(random_bytes(4)),
    $ekstensi
);

$tujuan = $folder . '/' . $namaFile;
if (@file_put_contents($tujuan, $biner, LOCK_EX) === false) {
    gagal('Foto gagal disimpan di server.', 'UPLOAD_FAILED', 500);
}

// URL absolut (berguna untuk ditampilkan di panel HR).
$skema = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
$host = $_SERVER['HTTP_HOST'] ?? 'localhost';
$folderUrl = rtrim(str_replace('\\', '/', dirname($_SERVER['SCRIPT_NAME'] ?? '/api')), '/');

sukses([
    'file' => 'uploads/' . $namaFile,
    'url'  => $skema . '://' . $host . $folderUrl . '/uploads/' . $namaFile,
], 'Foto bukti tersimpan.', 201);
