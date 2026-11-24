<?php
/**
 * diagnostik.php — TARUH DI FOLDER ROOT WEB (htdocs), BUKAN di dalam api/.
 *
 * Upload file ini ke:  htdocs/diagnostik.php
 * Lalu buka:           https://DOMAIN-KAMU/diagnostik.php
 *
 * File ini memberi tahu:
 *   - folder fisik tempat file ini berada
 *   - apakah folder "api" dan file health.php terdeteksi
 *   - URL yang seharusnya dipakai
 *
 * HAPUS file ini setelah selesai, karena menampilkan path server.
 */

declare(strict_types=1);

header('Content-Type: application/json; charset=utf-8');

$root = __DIR__;

$cek = [
    'api/health.php'          => file_exists($root . '/api/health.php'),
    'api/login.php'           => file_exists($root . '/api/login.php'),
    'api/config.php'          => file_exists($root . '/api/config.php'),
    'api/api/health.php'      => file_exists($root . '/api/api/health.php'),
    'health.php (di root)'    => file_exists($root . '/health.php'),
    'public_html/api/health.php' => file_exists($root . '/public_html/api/health.php'),
];

$folder = array_values(array_filter(
    scandir($root) ?: [],
    static fn(string $n): bool => $n !== '.' && $n !== '..'
));

$apiAda = is_dir($root . '/api');
$healthAda = file_exists($root . '/api/health.php');
$host = $_SERVER['HTTP_HOST'] ?? 'DOMAIN-KAMU';

$urlBenar = 'https://' . $host . '/api/health.php';

$pesan = [];
if ($healthAda) {
    $pesan[] = 'BAGUS: api/health.php ditemukan. Buka ' . $urlBenar;
} elseif (file_exists($root . '/api/api/health.php')) {
    $pesan[] = 'SALAH FOLDER: file berada di api/api/. Pindahkan isi api/api/ ke api/.';
} elseif (file_exists($root . '/health.php')) {
    $pesan[] = 'SALAH FOLDER: file PHP berada di root, bukan di folder api/. Buat folder api lalu pindahkan ke sana.';
} elseif (file_exists($root . '/public_html/api/health.php')) {
    $pesan[] = 'SALAH FOLDER: kamu memakai public_html. Di InfinityFree folder web adalah htdocs.';
} elseif (!$apiAda) {
    $pesan[] = 'Folder api/ tidak ada di folder ini. Berarti file di-upload ke folder yang salah.';
} else {
    $pesan[] = 'Folder api/ ada, tetapi health.php tidak ditemukan. Cek nama file/ekstensi (.php, bukan .php.txt).';
}

echo json_encode([
    'folder_fisik_file_ini' => $root,
    'document_root'         => $_SERVER['DOCUMENT_ROOT'] ?? null,
    'url_file_ini'          => 'https://' . $host . ($_SERVER['REQUEST_URI'] ?? '/diagnostik.php'),
    'nama_folder_benar'     => basename($root),
    'isi_folder_ini'        => $folder,
    'pemeriksaan'           => $cek,
    'url_health_yang_benar' => $urlBenar,
    'diagnosis'             => $pesan,
], JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
