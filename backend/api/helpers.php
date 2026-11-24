<?php
/**
 * helpers.php — header, CORS, dan fungsi response JSON.
 * Semua endpoint mengambil file ini (lewat db.php).
 */

declare(strict_types=1);

date_default_timezone_set('Asia/Jakarta');

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Headers: Content-Type, Authorization, X-Requested-With');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');

// Preflight request dari browser / WebView.
if (($_SERVER['REQUEST_METHOD'] ?? '') === 'OPTIONS') {
    http_response_code(204);
    exit;
}

/**
 * Tangkap error/exception yang tidak tertangani supaya klien menerima JSON,
 * bukan halaman 500 kosong. Sangat membantu saat tabel/kolom belum dibuat.
 */
set_exception_handler(static function (Throwable $e): void {
    error_log('[api] ' . $e->getMessage() . ' @ ' . $e->getFile() . ':' . $e->getLine());

    if (!headers_sent()) {
        header('Content-Type: application/json; charset=utf-8');
    }

    http_response_code(500);

    echo json_encode([
        'success' => false,
        'message' => 'Kesalahan server: ' . $e->getMessage(),
        'code'    => 'SERVER_ERROR',
    ], JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);

    exit;
});

/**
 * Kirim response JSON dengan bentuk yang selalu sama:
 *   sukses : { "success": true,  "message": "...", "data": ... }
 *   gagal  : { "success": false, "message": "...", "code": "..." }
 */
function kirimResponse(int $status, bool $sukses, string $pesan, $data = null, ?string $kode = null): void
{
    http_response_code($status);

    $body = [
        'success' => $sukses,
        'message' => $pesan,
    ];
    if ($data !== null) {
        $body['data'] = $data;
    }
    if (!$sukses && $kode !== null) {
        $body['code'] = $kode;
    }

    echo json_encode($body, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

function sukses($data = null, string $pesan = 'OK', int $status = 200): void
{
    kirimResponse($status, true, $pesan, $data);
}

function gagal(string $pesan, string $kode, int $status = 400): void
{
    kirimResponse($status, false, $pesan, null, $kode);
}

/** Baca body JSON dari aplikasi (fallback ke form-urlencoded). */
function inputJson(): array
{
    $raw = file_get_contents('php://input');
    if ($raw === false || trim($raw) === '') {
        return $_POST;
    }

    $data = json_decode($raw, true);
    if (is_array($data)) {
        return $data;
    }

    // Kalau body bukan JSON valid tapi ada form data, pakai itu.
    return $_POST;
}

/** Ambil nilai string dari input dengan default. */
function ambil(array $data, string $key, $default = null)
{
    if (!array_key_exists($key, $data)) {
        return $default;
    }
    $value = $data[$key];
    return is_string($value) ? trim($value) : $value;
}

/**
 * Ambil token Bearer dari header Authorization.
 * Beberapa server Apache (termasuk InfinityFree) membuang header ini,
 * jadi ada beberapa fallback.
 */
function bearerToken(): ?string
{
    $header = $_SERVER['HTTP_AUTHORIZATION']
        ?? $_SERVER['REDIRECT_HTTP_AUTHORIZATION']
        ?? '';

    if ($header === '' && function_exists('getallheaders')) {
        foreach (getallheaders() as $name => $value) {
            if (strcasecmp($name, 'Authorization') === 0) {
                $header = $value;
                break;
            }
        }
    }

    if (preg_match('/^Bearer\s+(.+)$/i', trim($header), $m)) {
        return trim($m[1]);
    }
    return null;
}

/** Daftar kolom sebuah tabel (dipakai untuk kolom opsional). */
function kolomTabel(PDO $pdo, string $tabel): array
{
    static $cache = [];
    if (isset($cache[$tabel])) {
        return $cache[$tabel];
    }

    $stmt = $pdo->query('SHOW COLUMNS FROM `' . str_replace('`', '', $tabel) . '`');
    $kolom = array_map(static fn(array $row): string => $row['Field'], $stmt->fetchAll());
    return $cache[$tabel] = $kolom;
}

/** Jarak dua koordinat dalam meter (haversine). */
function hitungJarakMeter(float $lat1, float $lon1, float $lat2, float $lon2): float
{
    $r = 6371000.0;
    $dLat = deg2rad($lat2 - $lat1);
    $dLon = deg2rad($lon2 - $lon1);
    $a = sin($dLat / 2) ** 2
        + cos(deg2rad($lat1)) * cos(deg2rad($lat2)) * sin($dLon / 2) ** 2;
    return $r * 2 * atan2(sqrt($a), sqrt(1 - $a));
}

/** Pastikan method HTTP sesuai. */
function wajibMethod(string $method): void
{
    if (strtoupper($_SERVER['REQUEST_METHOD'] ?? 'GET') !== strtoupper($method)) {
        header('Allow: ' . $method);
        gagal('Method tidak diizinkan. Gunakan ' . $method . '.', 'METHOD_NOT_ALLOWED', 405);
    }
}
