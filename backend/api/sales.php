<?php
/**
 * sales.php — daftar & tambah akun sales (khusus HR).
 *
 * GET  ?q=andi
 *   -> daftar sales + nama/email dari tbl_pengguna
 *
 * POST (create)
 * {
 *   "nama": "Andi Pratama",
 *   "email": "andi@example.com",
 *   "password": "rahasia123",
 *   "nomor_sales": "SLS-014",
 *   "nomor_telepon": "081234567890",
 *   "wilayah": "Surabaya Pusat",       <- opsional (butuh 04_kolom_admin_hr.sql)
 *   "face_data": "andi.jpg"            <- opsional
 * }
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

$pengguna = wajibLogin($pdo);
wajibRole($pengguna, 'HR');

$kolomPengguna = kolomTabel($pdo, 'tbl_pengguna');
$adaWilayah = in_array('wilayah', $kolomPengguna, true);
$selectWilayah = $adaWilayah ? 'p.wilayah' : 'NULL AS wilayah';

$method = strtoupper($_SERVER['REQUEST_METHOD'] ?? 'GET');

/* ============================================================
 * GET — daftar sales
 * ============================================================ */
if ($method === 'GET') {
    $q = isset($_GET['q']) ? trim((string)$_GET['q']) : '';

    $sql = "SELECT
                s.id_sales,
                s.id_pengguna,
                s.nomor_sales,
                s.nomor_telepon,
                s.face_data,
                p.nama,
                p.email,
                p.created_at,
                {$selectWilayah}
            FROM tbl_sales s
            JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna";

    $params = [];
    if ($q !== '') {
        $sql .= ' WHERE p.nama LIKE ? OR p.email LIKE ? OR s.nomor_sales LIKE ?';
        $like = '%' . $q . '%';
        $params = [$like, $like, $like];
    }

    $sql .= ' ORDER BY p.nama ASC';

    $stmt = $pdo->prepare($sql);
    $stmt->execute($params);
    $rows = $stmt->fetchAll();

    foreach ($rows as &$row) {
        $row['id_sales'] = (int)$row['id_sales'];
        $row['id_pengguna'] = (int)$row['id_pengguna'];
    }
    unset($row);

    sukses($rows, 'Daftar sales.');
}

/* ============================================================
 * POST — tambah / ubah / hapus sales
 * ============================================================ */
wajibMethod('POST');

$data = inputJson();
$action = strtolower(trim((string)(ambil($data, 'action') ?? 'create')));

if (!in_array($action, ['create', 'update', 'delete'], true)) {
    gagal("Aksi tidak valid. Gunakan 'create', 'update', atau 'delete'.", 'INVALID_ACTION', 400);
}

/* ------------------------------------------------------------
 * UPDATE
 * ------------------------------------------------------------ */
if ($action === 'update') {
    $idSales = (int)(ambil($data, 'id_sales', 0));
    if ($idSales <= 0) {
        gagal('id_sales wajib diisi.', 'MISSING_ID_SALES', 400);
    }

    $stmt = $pdo->prepare(
        "SELECT s.id_sales, s.id_pengguna, s.nomor_sales, s.nomor_telepon, s.face_data,
                p.nama, p.email, p.created_at, {$selectWilayah}
         FROM tbl_sales s
         JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna
         WHERE s.id_sales = ?
         LIMIT 1"
    );
    $stmt->execute([$idSales]);
    $lama = $stmt->fetch();

    if (!$lama) {
        gagal('Sales tidak ditemukan.', 'SALES_NOT_FOUND', 404);
    }

    $nama = trim((string)(ambil($data, 'nama') ?? ''));
    $email = strtolower(trim((string)(ambil($data, 'email') ?? '')));
    $password = (string)(ambil($data, 'password') ?? '');
    $nomorSales = trim((string)(ambil($data, 'nomor_sales') ?? ''));
    $nomorTelepon = trim((string)(ambil($data, 'nomor_telepon') ?? ''));
    $wilayah = trim((string)(ambil($data, 'wilayah') ?? ''));

    if ($nama === '' || $email === '' || $nomorSales === '' || $nomorTelepon === '') {
        gagal(
            'Nama, email, nomor_sales, dan nomor_telepon wajib diisi.',
            'MISSING_SALES_DATA',
            400
        );
    }

    if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
        gagal('Format email tidak valid.', 'INVALID_EMAIL', 400);
    }

    if ($password !== '' && strlen($password) < 6) {
        gagal('Password minimal 6 karakter.', 'PASSWORD_TOO_SHORT', 400);
    }

    try {
        $pdo->beginTransaction();

        $setPengguna = ['nama = ?', 'email = ?'];
        $nilaiPengguna = [$nama, $email];

        if ($adaWilayah) {
            $setPengguna[] = 'wilayah = ?';
            $nilaiPengguna[] = $wilayah !== '' ? $wilayah : null;
        }

        if ($password !== '') {
            $setPengguna[] = 'password_hash = ?';
            $nilaiPengguna[] = password_hash($password, PASSWORD_DEFAULT);
        }

        $nilaiPengguna[] = (int)$lama['id_pengguna'];

        $stmt = $pdo->prepare(
            'UPDATE tbl_pengguna SET ' . implode(', ', $setPengguna) . ' WHERE id_pengguna = ?'
        );
        $stmt->execute($nilaiPengguna);

        $stmt = $pdo->prepare(
            'UPDATE tbl_sales SET nomor_sales = ?, nomor_telepon = ? WHERE id_sales = ?'
        );
        $stmt->execute([$nomorSales, $nomorTelepon, $idSales]);

        $pdo->commit();
    } catch (PDOException $e) {
        if ($pdo->inTransaction()) {
            $pdo->rollBack();
        }

        if ($e->getCode() === '23000') {
            gagal(
                'Email, nomor sales, atau nomor telepon sudah dipakai sales lain.',
                'DATA_DUPLICATE',
                409
            );
        }

        error_log('[sales:update] ' . $e->getMessage());
        gagal('Data sales gagal diperbarui.', 'SALES_UPDATE_FAILED', 500);
    }

    $stmt = $pdo->prepare(
        "SELECT s.id_sales, s.id_pengguna, s.nomor_sales, s.nomor_telepon, s.face_data,
                p.nama, p.email, p.created_at, {$selectWilayah}
         FROM tbl_sales s
         JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna
         WHERE s.id_sales = ?
         LIMIT 1"
    );
    $stmt->execute([$idSales]);
    $row = $stmt->fetch();
    $row['id_sales'] = (int)$row['id_sales'];
    $row['id_pengguna'] = (int)$row['id_pengguna'];

    sukses($row, 'Akun sales berhasil diperbarui.');
}

/* ------------------------------------------------------------
 * DELETE
 * ------------------------------------------------------------ */
if ($action === 'delete') {
    $idSales = (int)(ambil($data, 'id_sales', 0));
    if ($idSales <= 0) {
        gagal('id_sales wajib diisi.', 'MISSING_ID_SALES', 400);
    }

    $stmt = $pdo->prepare('SELECT id_pengguna FROM tbl_sales WHERE id_sales = ? LIMIT 1');
    $stmt->execute([$idSales]);
    $row = $stmt->fetch();

    if (!$row) {
        gagal('Sales tidak ditemukan.', 'SALES_NOT_FOUND', 404);
    }

    // Menghapus akun pengguna ikut menghapus profil sales beserta riwayat
    // kunjungan, permohonan, dan penggajian lewat ON DELETE CASCADE.
    try {
        $stmt = $pdo->prepare('DELETE FROM tbl_pengguna WHERE id_pengguna = ?');
        $stmt->execute([(int)$row['id_pengguna']]);
    } catch (PDOException $e) {
        error_log('[sales:delete] ' . $e->getMessage());
        gagal('Akun sales gagal dihapus.', 'SALES_DELETE_FAILED', 500);
    }

    sukses(['id_sales' => $idSales], 'Akun sales berhasil dihapus.');
}

/* ------------------------------------------------------------
 * CREATE
 * ------------------------------------------------------------ */
$nama = trim((string)(ambil($data, 'nama') ?? ''));
$email = strtolower(trim((string)(ambil($data, 'email') ?? '')));
$password = (string)(ambil($data, 'password') ?? '');
$nomorSales = trim((string)(ambil($data, 'nomor_sales') ?? ambil($data, 'nip') ?? ''));
$nomorTelepon = trim((string)(ambil($data, 'nomor_telepon') ?? ambil($data, 'telepon') ?? ''));
$wilayah = trim((string)(ambil($data, 'wilayah') ?? ''));
$faceData = ambil($data, 'face_data');

if ($nama === '' || $email === '' || $password === '' || $nomorSales === '' || $nomorTelepon === '') {
    gagal(
        'Nama, email, password, nomor_sales, dan nomor_telepon wajib diisi.',
        'MISSING_SALES_DATA',
        400
    );
}

if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
    gagal('Format email tidak valid.', 'INVALID_EMAIL', 400);
}

if (strlen($password) < 6) {
    gagal('Password minimal 6 karakter.', 'PASSWORD_TOO_SHORT', 400);
}

try {
    $pdo->beginTransaction();

    $hash = password_hash($password, PASSWORD_DEFAULT);

    $fieldPengguna = ['nama', 'email', 'password_hash', 'role'];
    $nilaiPengguna = [$nama, $email, $hash, 'Sales'];
    if ($adaWilayah && $wilayah !== '') {
        $fieldPengguna[] = 'wilayah';
        $nilaiPengguna[] = $wilayah;
    }

    $stmt = $pdo->prepare(
        'INSERT INTO tbl_pengguna (' . implode(', ', $fieldPengguna) . ')
         VALUES (' . implode(', ', array_fill(0, count($fieldPengguna), '?')) . ')'
    );
    $stmt->execute($nilaiPengguna);
    $idPengguna = (int)$pdo->lastInsertId();

    $stmt = $pdo->prepare(
        'INSERT INTO tbl_sales (id_pengguna, nomor_sales, nomor_telepon, face_data)
         VALUES (?, ?, ?, ?)'
    );
    $stmt->execute([
        $idPengguna,
        $nomorSales,
        $nomorTelepon,
        $faceData !== null && $faceData !== '' ? (string)$faceData : null,
    ]);
    $idSales = (int)$pdo->lastInsertId();

    $pdo->commit();
} catch (PDOException $e) {
    if ($pdo->inTransaction()) {
        $pdo->rollBack();
    }

    if ($e->getCode() === '23000') {
        gagal(
            'Email, nomor sales, atau nomor telepon sudah terdaftar. Gunakan data lain.',
            'DATA_DUPLICATE',
            409
        );
    }

    error_log('[sales:create] ' . $e->getMessage());
    gagal('Data sales gagal disimpan.', 'SALES_CREATE_FAILED', 500);
}

sukses([
    'id_sales'         => $idSales,
    'id_pengguna'      => $idPengguna,
    'nama'             => $nama,
    'email'            => $email,
    'nomor_sales'      => $nomorSales,
    'nomor_telepon'    => $nomorTelepon,
    'wilayah'          => $adaWilayah ? $wilayah : null,
], 'Akun sales berhasil dibuat.', 201);
