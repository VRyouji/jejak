<?php
/**
 * logout.php — POST, hapus token yang sedang dipakai.
 * Header: Authorization: Bearer <token>
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

wajibMethod('POST');
$pengguna = wajibLogin($pdo);

$stmt = $pdo->prepare('DELETE FROM tbl_api_token WHERE token = ?');
$stmt->execute([(string)$pengguna['token']]);

sukses(null, 'Logout berhasil.');
