<?php
/**
 * auth.php — middleware token Bearer.
 *
 * Pakai di endpoint yang butuh login:
 *   require_once __DIR__ . '/auth.php';
 *   $pengguna = wajibLogin($pdo);
 *   wajibRole($pengguna, 'Sales');
 */

declare(strict_types=1);

require_once __DIR__ . '/db.php';

/**
 * Validasi token dan kembalikan data pengguna (+ profil sales/hr).
 * Kalau token tidak ada / kadaluarsa, langsung kirim 401 dan berhenti.
 */
function wajibLogin(PDO $pdo): array
{
    $token = bearerToken();

    if ($token === null || $token === '') {
        gagal('Token autentikasi diperlukan. Kirim header: Authorization: Bearer <token>.', 'TOKEN_REQUIRED', 401);
    }

    $stmt = $pdo->prepare(
        "SELECT p.id_pengguna, p.nama, p.email, p.role,
                s.id_sales, s.nomor_sales, s.nomor_telepon,
                h.id_hr
         FROM tbl_api_token t
         JOIN tbl_pengguna p ON p.id_pengguna = t.id_pengguna
         LEFT JOIN tbl_sales s ON s.id_pengguna = p.id_pengguna
         LEFT JOIN tbl_hr h ON h.id_pengguna = p.id_pengguna
         WHERE t.token = ? AND t.expires_at > NOW()
         LIMIT 1"
    );
    $stmt->execute([$token]);
    $row = $stmt->fetch();

    if (!$row) {
        gagal('Token tidak valid atau sudah kadaluarsa. Silakan login ulang.', 'TOKEN_INVALID', 401);
    }

    $row['id_pengguna'] = (int)$row['id_pengguna'];
    $row['id_sales'] = $row['id_sales'] !== null ? (int)$row['id_sales'] : null;
    $row['id_hr'] = $row['id_hr'] !== null ? (int)$row['id_hr'] : null;
    $row['token'] = $token;

    return $row;
}

/** Batasi endpoint ke role tertentu ('Sales' atau 'HR'). */
function wajibRole(array $pengguna, string $role): void
{
    if (strcasecmp((string)$pengguna['role'], $role) !== 0) {
        gagal(
            'Endpoint ini hanya untuk role ' . $role . '.',
            'FORBIDDEN_ROLE',
            403
        );
    }
}

/** Hapus token yang sudah kadaluarsa (dipanggil sesekali, murah). */
function bersihkanTokenKadaluarsa(PDO $pdo): void
{
    $pdo->exec("DELETE FROM tbl_api_token WHERE expires_at <= NOW()");
}
