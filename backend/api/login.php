<?php
/**
 * login.php — POST { "email": "...", "password": "..." }
 *
 * Body yang diterima:
 *   email     (wajib) — boleh juga dikirim sebagai "username"/"identifier"
 *   password  (wajib)
 *
 * Response sukses:
 * {
 *   "success": true,
 *   "message": "Login berhasil",
 *   "data": {
 *     "token": "64 karakter",
 *     "expires_at": "2026-11-13 08:00:00",
 *     "pengguna": { "id_pengguna": 1, "nama": "...", "email": "...", "role": "Sales" },
 *     "sales": { "id_sales": 1, "nomor_sales": "SLS-014", "nomor_telepon": "0812..." },
 *     "hr": null
 *   }
 * }
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

wajibMethod('POST');

$data = inputJson();

$identifier = ambil($data, 'email')
    ?? ambil($data, 'username')
    ?? ambil($data, 'identifier');
$password = ambil($data, 'password');

if ($identifier === null || $identifier === '' || $password === null || $password === '') {
    gagal('Email/username dan password wajib diisi.', 'MISSING_CREDENTIALS', 400);
}

$kolomPengguna = kolomTabel($pdo, 'tbl_pengguna');
$selectWilayah = in_array('wilayah', $kolomPengguna, true) ? 'wilayah' : 'NULL AS wilayah';

$stmt = $pdo->prepare(
    "SELECT id_pengguna, nama, email, password_hash, role, {$selectWilayah}
     FROM tbl_pengguna
     WHERE email = ? OR nama = ? OR SUBSTRING_INDEX(email, '@', 1) = ?
     LIMIT 1"
);
$stmt->execute([$identifier, $identifier, $identifier]);
$user = $stmt->fetch();

if (!$user || !password_verify((string)$password, (string)$user['password_hash'])) {
    gagal('Email/username atau password salah.', 'INVALID_CREDENTIALS', 401);
}

$idPengguna = (int)$user['id_pengguna'];

// Ambil profil sesuai role (kalau ada).
$sales = null;
$stmtSales = $pdo->prepare(
    "SELECT id_sales, nomor_sales, nomor_telepon, face_data
     FROM tbl_sales WHERE id_pengguna = ? LIMIT 1"
);
$stmtSales->execute([$idPengguna]);
if ($row = $stmtSales->fetch()) {
    $row['id_sales'] = (int)$row['id_sales'];
    $sales = $row;
}

$hr = null;
$stmtHr = $pdo->prepare("SELECT id_hr FROM tbl_hr WHERE id_pengguna = ? LIMIT 1");
$stmtHr->execute([$idPengguna]);
if ($row = $stmtHr->fetch()) {
    $row['id_hr'] = (int)$row['id_hr'];
    $hr = $row;
}

$lifetime = (int)($config['token_lifetime_hours'] ?? 24);
$token = bin2hex(random_bytes(32));
$expiresAt = date('Y-m-d H:i:s', strtotime('+' . $lifetime . ' hours'));

try {
    $pdo->beginTransaction();

    // Satu sesi aktif per pengguna: hapus token lama.
    $pdo->prepare('DELETE FROM tbl_api_token WHERE id_pengguna = ?')->execute([$idPengguna]);
    $pdo->prepare(
        'INSERT INTO tbl_api_token (id_pengguna, token, expires_at) VALUES (?, ?, ?)'
    )->execute([$idPengguna, $token, $expiresAt]);

    $pdo->commit();
} catch (PDOException $e) {
    $pdo->rollBack();
    error_log('[login] ' . $e->getMessage());
    gagal('Login gagal membuat sesi. Coba lagi.', 'TOKEN_CREATE_FAILED', 500);
}

sukses([
    'token'      => $token,
    'expires_at' => $expiresAt,
    'pengguna'   => [
        'id_pengguna' => $idPengguna,
        'nama'        => $user['nama'],
        'email'       => $user['email'],
        'role'        => $user['role'],
        'wilayah'     => $user['wilayah'] ?? null,
    ],
    'sales' => $sales,
    'hr'    => $hr,
], 'Login berhasil.');
