<?php
/**
 * register.php — POST pendaftaran akun Sales.
 *
 * Body minimal:
 * {
 *   "nama": "Andi Pratama",
 *   "password": "rahasia123",
 *   "nomor_sales": "SLS-014",
 *   "nomor_telepon": "081234567890",
 *   "email": "andi@example.com"        <- opsional; kalau kosong dibuat dari username
 *   "username": "andi.pratama"         <- opsional, dipakai sebagai bagian email
 * }
 *
 * Membuat 2 baris: tbl_pengguna (role Sales) + tbl_sales.
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

wajibMethod('POST');

$data = inputJson();

$nama = (string)(ambil($data, 'nama') ?? '');
$password = (string)(ambil($data, 'password') ?? '');
$nomorSales = (string)(ambil($data, 'nomor_sales') ?? ambil($data, 'nip') ?? '');
$nomorTelepon = (string)(ambil($data, 'nomor_telepon') ?? ambil($data, 'telepon') ?? '');
$email = (string)(ambil($data, 'email') ?? '');
$username = (string)(ambil($data, 'username') ?? '');

if ($nama === '' || $password === '' || $nomorSales === '') {
    gagal('Nama, password, dan nomor_sales wajib diisi.', 'MISSING_REGISTER_DATA', 400);
}

if (strlen($password) < 6) {
    gagal('Password minimal 6 karakter.', 'PASSWORD_TOO_SHORT', 400);
}

if ($email === '') {
    $dasar = $username !== '' ? $username : preg_replace('/[^a-z0-9]+/i', '.', strtolower($nomorSales));
    $email = strtolower(trim((string)$dasar, '.')) . '@jejaksales.local';
}

if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
    gagal('Format email tidak valid.', 'INVALID_EMAIL', 400);
}

if ($nomorTelepon === '') {
    // ERD mewajibkan nomor_telepon unik. Kalau aplikasi belum mengirimnya,
    // buat nomor placeholder unik supaya pendaftaran tetap bisa jalan.
    $nomorTelepon = '08' . str_pad((string)(time() % 100000000), 8, '0', STR_PAD_LEFT)
        . substr((string)random_int(100, 999), 0, 2);
    $nomorTelepon = substr($nomorTelepon, 0, 15);
}

try {
    $pdo->beginTransaction();

    $hash = password_hash($password, PASSWORD_DEFAULT);

    $stmt = $pdo->prepare(
        "INSERT INTO tbl_pengguna (nama, email, password_hash, role)
         VALUES (?, ?, ?, 'Sales')"
    );
    $stmt->execute([$nama, $email, $hash]);
    $idPengguna = (int)$pdo->lastInsertId();

    $stmt = $pdo->prepare(
        'INSERT INTO tbl_sales (id_pengguna, nomor_sales, nomor_telepon, face_data)
         VALUES (?, ?, ?, NULL)'
    );
    $stmt->execute([$idPengguna, $nomorSales, $nomorTelepon]);
    $idSales = (int)$pdo->lastInsertId();

    $pdo->commit();
} catch (PDOException $e) {
    if ($pdo->inTransaction()) {
        $pdo->rollBack();
    }

    if ($e->getCode() === '23000') {
        gagal(
            'Email, nomor_sales, atau nomor_telepon sudah terdaftar. Gunakan data lain.',
            'DATA_DUPLICATE',
            409
        );
    }

    error_log('[register] ' . $e->getMessage());
    gagal('Pendaftaran gagal disimpan. Coba lagi.', 'REGISTER_FAILED', 500);
}

sukses([
    'pengguna' => [
        'id_pengguna' => $idPengguna,
        'nama'        => $nama,
        'email'       => $email,
        'role'        => 'Sales',
    ],
    'sales' => [
        'id_sales'      => $idSales,
        'nomor_sales'   => $nomorSales,
        'nomor_telepon' => $nomorTelepon,
    ],
], 'Pendaftaran berhasil. Silakan login.', 201);
