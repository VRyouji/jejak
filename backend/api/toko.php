<?php
/**
 * toko.php — daftar & kelola toko mitra.
 *
 * GET  ?q=makmur          -> cari toko
 * GET  ?id_toko=3         -> detail satu toko
 *
 * POST (khusus HR)
 * { "action": "create", "nama_toko": "...", "alamat": "...",
 *   "kode_toko": "TK-090", "pemilik": "...", "telepon": "...",
 *   "latitude": -7.25, "longitude": 112.75, "radius": 100 }
 * { "action": "update", "id_toko": 3, ... }
 * { "action": "delete", "id_toko": 3 }
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

$pengguna = wajibLogin($pdo);
$kolomToko = kolomTabel($pdo, 'tbl_toko');

/** Susun daftar kolom SELECT, memakai fallback bila kolom opsional belum ada. */
function ekspresiToko(array $kolom): string
{
    $bagian = [
        'id_toko',
        'nama_toko',
        'alamat',
        'latitude',
        'longitude',
        'radius',
        'created_at',
    ];

    $bagian[] = in_array('kode_toko', $kolom, true)
        ? "COALESCE(NULLIF(kode_toko, ''), CONCAT('TK-', id_toko)) AS kode_toko"
        : "CONCAT('TK-', id_toko) AS kode_toko";

    $bagian[] = in_array('pemilik', $kolom, true)
        ? "COALESCE(pemilik, '') AS pemilik"
        : "'' AS pemilik";

    $bagian[] = in_array('telepon', $kolom, true)
        ? "COALESCE(telepon, '') AS telepon"
        : "'' AS telepon";

    return implode(', ', $bagian);
}

function rapikanToko(array $row): array
{
    $row['id_toko'] = (int)$row['id_toko'];
    $row['latitude'] = $row['latitude'] !== null ? (float)$row['latitude'] : null;
    $row['longitude'] = $row['longitude'] !== null ? (float)$row['longitude'] : null;
    $row['radius'] = $row['radius'] !== null ? (float)$row['radius'] : 100.0;
    $row['radius_meter'] = $row['radius'];
    return $row;
}

function ambilToko(PDO $pdo, int $idToko, array $kolom): array
{
    $stmt = $pdo->prepare('SELECT ' . ekspresiToko($kolom) . ' FROM tbl_toko WHERE id_toko = ? LIMIT 1');
    $stmt->execute([$idToko]);
    $row = $stmt->fetch();
    if (!$row) {
        gagal('Toko tidak ditemukan.', 'TOKO_NOT_FOUND', 404);
    }
    return rapikanToko($row);
}

$method = strtoupper($_SERVER['REQUEST_METHOD'] ?? 'GET');

/* ============================================================
 * GET — daftar / detail toko
 * ============================================================ */
if ($method === 'GET') {
    $idToko = isset($_GET['id_toko']) ? (int)$_GET['id_toko'] : 0;
    $q = isset($_GET['q']) ? trim((string)$_GET['q']) : '';

    if ($idToko > 0) {
        sukses(ambilToko($pdo, $idToko, $kolomToko), 'Detail toko.');
    }

    $sql = 'SELECT ' . ekspresiToko($kolomToko) . ' FROM tbl_toko';
    $params = [];

    if ($q !== '') {
        $sql .= ' WHERE nama_toko LIKE ? OR alamat LIKE ?';
        $like = '%' . $q . '%';
        $params = [$like, $like];
    }

    $sql .= ' ORDER BY nama_toko ASC';

    $stmt = $pdo->prepare($sql);
    $stmt->execute($params);
    $rows = array_map('rapikanToko', $stmt->fetchAll());

    sukses($rows, 'Daftar toko.');
}

/* ============================================================
 * POST — create / update / delete (HR)
 * ============================================================ */
wajibMethod('POST');
wajibRole($pengguna, 'HR');

$data = inputJson();
$action = strtolower((string)(ambil($data, 'action') ?? 'create'));

if ($action === 'create') {
    $namaToko = trim((string)(ambil($data, 'nama_toko') ?? ''));
    if ($namaToko === '') {
        gagal('Nama toko wajib diisi.', 'MISSING_NAMA_TOKO', 400);
    }

    $kodeToko = trim((string)(ambil($data, 'kode_toko') ?? ''));
    if ($kodeToko !== '' && in_array('kode_toko', $kolomToko, true)) {
        $stmt = $pdo->prepare('SELECT id_toko FROM tbl_toko WHERE kode_toko = ? LIMIT 1');
        $stmt->execute([$kodeToko]);
        if ($stmt->fetch()) {
            gagal('Kode toko tersebut sudah digunakan.', 'KODE_TOKO_DUPLICATE', 409);
        }
    }

    $field = ['nama_toko'];
    $values = [$namaToko];

    $alamat = ambil($data, 'alamat');
    if ($alamat !== null) {
        $field[] = 'alamat';
        $values[] = (string)$alamat;
    }

    $latitude = ambil($data, 'latitude');
    if ($latitude !== null && $latitude !== '' && is_numeric($latitude)) {
        $field[] = 'latitude';
        $values[] = (float)$latitude;
    }

    $longitude = ambil($data, 'longitude');
    if ($longitude !== null && $longitude !== '' && is_numeric($longitude)) {
        $field[] = 'longitude';
        $values[] = (float)$longitude;
    }

    $radius = ambil($data, 'radius', ambil($data, 'radius_meter', 100));
    if ($radius !== null && $radius !== '' && is_numeric($radius)) {
        $field[] = 'radius';
        $values[] = (float)$radius;
    }

    $opsional = [
        'kode_toko' => $kodeToko !== '' ? $kodeToko : null,
        'pemilik'   => ambil($data, 'pemilik'),
        'telepon'   => ambil($data, 'telepon'),
    ];
    foreach ($opsional as $namaKolom => $nilai) {
        if ($nilai !== null && $nilai !== '' && in_array($namaKolom, $kolomToko, true)) {
            $field[] = $namaKolom;
            $values[] = (string)$nilai;
        }
    }

    try {
        $stmt = $pdo->prepare(
            'INSERT INTO tbl_toko (' . implode(', ', $field) . ')
             VALUES (' . implode(', ', array_fill(0, count($field), '?')) . ')'
        );
        $stmt->execute($values);
        $idToko = (int)$pdo->lastInsertId();
    } catch (PDOException $e) {
        error_log('[toko:create] ' . $e->getMessage());
        gagal('Data toko gagal disimpan.', 'TOKO_CREATE_FAILED', 500);
    }

    sukses(ambilToko($pdo, $idToko, $kolomToko), 'Toko berhasil ditambahkan.', 201);
}

if ($action === 'update') {
    $idToko = (int)(ambil($data, 'id_toko', 0));
    if ($idToko <= 0) {
        gagal('id_toko wajib diisi.', 'MISSING_ID_TOKO', 400);
    }

    $set = [];
    $values = [];

    foreach (['nama_toko', 'alamat', 'pemilik', 'telepon', 'kode_toko'] as $namaKolom) {
        $nilai = ambil($data, $namaKolom);
        if ($nilai === null || !in_array($namaKolom, $kolomToko, true)) {
            continue;
        }
        $set[] = $namaKolom . ' = ?';
        $values[] = (string)$nilai;
    }

    foreach (['latitude', 'longitude', 'radius'] as $namaKolom) {
        $nilai = ambil($data, $namaKolom);
        if ($nilai === null || $nilai === '' || !is_numeric($nilai)) {
            continue;
        }
        $set[] = $namaKolom . ' = ?';
        $values[] = (float)$nilai;
    }

    if (!$set) {
        gagal('Tidak ada kolom yang diubah.', 'TIDAK_ADA_PERUBAHAN', 400);
    }

    $values[] = $idToko;

    try {
        $stmt = $pdo->prepare('UPDATE tbl_toko SET ' . implode(', ', $set) . ' WHERE id_toko = ?');
        $stmt->execute($values);
    } catch (PDOException $e) {
        error_log('[toko:update] ' . $e->getMessage());
        gagal('Data toko gagal diperbarui.', 'TOKO_UPDATE_FAILED', 500);
    }

    sukses(ambilToko($pdo, $idToko, $kolomToko), 'Toko berhasil diperbarui.');
}

if ($action === 'delete') {
    $idToko = (int)(ambil($data, 'id_toko', 0));
    if ($idToko <= 0) {
        gagal('id_toko wajib diisi.', 'MISSING_ID_TOKO', 400);
    }

    $stmt = $pdo->prepare('DELETE FROM tbl_toko WHERE id_toko = ?');
    $stmt->execute([$idToko]);

    if ($stmt->rowCount() === 0) {
        gagal('Toko tidak ditemukan.', 'TOKO_NOT_FOUND', 404);
    }

    sukses(['id_toko' => $idToko], 'Toko berhasil dihapus.');
}

gagal("Aksi tidak valid. Gunakan 'create', 'update', atau 'delete'.", 'INVALID_ACTION', 400);
