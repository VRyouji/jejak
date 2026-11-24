<?php
/**
 * absen.php — check-in / check-out kunjungan.
 *
 * POST body untuk check-in:
 * {
 *   "action": "checkin",
 *   "id_toko": 2,
 *   "latitude_checkin": -7.2854,
 *   "longitude_checkin": 112.7398,
 *   "foto_bukti": "foto_20261112.jpg",   <- opsional
 *   "jarak_meter": 8,                    <- opsional (dihitung server kalau kosong)
 *   "skor_wajah": 0.97,                  <- opsional (butuh 02_optional_columns.sql)
 *   "tujuan_kunjungan": "Restock",       <- opsional
 *   "catatan": "..."                     <- opsional
 * }
 *
 * POST body untuk check-out:
 * {
 *   "action": "checkout",
 *   "id_kunjungan": 10,
 *   "latitude_checkout": -7.2853,
 *   "longitude_checkout": 112.7399
 * }
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

wajibMethod('POST');
$pengguna = wajibLogin($pdo);
wajibRole($pengguna, 'Sales');

if ($pengguna['id_sales'] === null) {
    gagal('Profil sales tidak ditemukan untuk akun ini.', 'SALES_PROFILE_NOT_FOUND', 400);
}
$idSales = (int)$pengguna['id_sales'];

$data = inputJson();
$action = strtolower((string)(ambil($data, 'action') ?? 'checkin'));

/* ============================================================
 * CHECK-IN
 * ============================================================ */
if ($action === 'checkin') {
    $idToko = (int)(ambil($data, 'id_toko', 0));
    $lat = ambil($data, 'latitude_checkin');
    $lon = ambil($data, 'longitude_checkin');

    if ($idToko <= 0 || $lat === null || $lon === null || $lat === '' || $lon === '') {
        gagal(
            'id_toko, latitude_checkin, dan longitude_checkin wajib diisi.',
            'MISSING_CHECKIN_DATA',
            400
        );
    }

    $lat = (float)$lat;
    $lon = (float)$lon;

    $stmt = $pdo->prepare(
        'SELECT id_toko, nama_toko, latitude, longitude, radius FROM tbl_toko WHERE id_toko = ? LIMIT 1'
    );
    $stmt->execute([$idToko]);
    $toko = $stmt->fetch();

    if (!$toko) {
        gagal('Toko tidak ditemukan.', 'TOKO_NOT_FOUND', 404);
    }

    // Hitung jarak di server (lebih tepercaya daripada kiriman client).
    $jarak = null;
    $dalamRadius = null;
    if ($toko['latitude'] !== null && $toko['longitude'] !== null) {
        $jarak = hitungJarakMeter($lat, $lon, (float)$toko['latitude'], (float)$toko['longitude']);
        $radius = $toko['radius'] !== null ? (float)$toko['radius'] : 100.0;
        $dalamRadius = $jarak <= $radius;
    }

    // Cegah check-in ganda ke toko yang sama tanpa check-out.
    $stmt = $pdo->prepare(
        'SELECT id_kunjungan FROM tbl_kunjungan
         WHERE id_sales = ? AND id_toko = ?
           AND DATE(waktu_checkin) = CURDATE()
           AND waktu_checkout IS NULL
         LIMIT 1'
    );
    $stmt->execute([$idSales, $idToko]);
    if ($stmt->fetch()) {
        gagal(
            'Anda sudah check-in ke toko ini hari ini dan belum check-out.',
            'ALREADY_CHECKED_IN',
            409
        );
    }

    // Kolom opsional: hanya diisi kalau sudah ada di database.
    $kolom = kolomTabel($pdo, 'tbl_kunjungan');
    $field = ['id_sales', 'id_toko', 'waktu_checkin', 'latitude_checkin', 'longitude_checkin', 'status_verifikasi'];
    $values = [$idSales, $idToko, date('Y-m-d H:i:s'), $lat, $lon, 'Pending'];
    $placeholders = ['?', '?', '?', '?', '?', '?'];

    $foto = ambil($data, 'foto_bukti');
    if ($foto !== null && $foto !== '') {
        $field[] = 'foto_bukti';
        $values[] = (string)$foto;
        $placeholders[] = '?';
    }

    $opsional = [
        'jarak_meter'      => $jarak ?? ambil($data, 'jarak_meter'),
        'skor_wajah'       => ambil($data, 'skor_wajah'),
        'tujuan_kunjungan' => ambil($data, 'tujuan_kunjungan'),
        'catatan'          => ambil($data, 'catatan'),
    ];
    foreach ($opsional as $namaKolom => $nilai) {
        if ($nilai !== null && $nilai !== '' && in_array($namaKolom, $kolom, true)) {
            $field[] = $namaKolom;
            $values[] = $nilai;
            $placeholders[] = '?';
        }
    }

    try {
        $sql = 'INSERT INTO tbl_kunjungan (' . implode(', ', $field) . ')
                VALUES (' . implode(', ', $placeholders) . ')';
        $stmt = $pdo->prepare($sql);
        $stmt->execute($values);
        $idKunjungan = (int)$pdo->lastInsertId();
    } catch (PDOException $e) {
        error_log('[absen:checkin] ' . $e->getMessage());
        gagal('Gagal menyimpan check-in.', 'CHECKIN_FAILED', 500);
    }

    sukses([
        'id_kunjungan'      => $idKunjungan,
        'id_sales'          => $idSales,
        'id_toko'           => $idToko,
        'nama_toko'         => $toko['nama_toko'],
        'waktu_checkin'     => date('Y-m-d H:i:s'),
        'status_verifikasi' => 'Pending',
        'jarak_meter'       => $jarak !== null ? round($jarak, 2) : null,
        'dalam_radius'      => $dalamRadius,
    ], $dalamRadius === false
        ? 'Check-in tersimpan, tetapi lokasi Anda di luar radius toko.'
        : 'Check-in berhasil.',
        201);
}

/* ============================================================
 * CHECK-OUT
 * ============================================================ */
if ($action === 'checkout') {
    $idKunjungan = (int)(ambil($data, 'id_kunjungan', 0));
    $lat = ambil($data, 'latitude_checkout');
    $lon = ambil($data, 'longitude_checkout');

    if ($idKunjungan <= 0 || $lat === null || $lon === null || $lat === '' || $lon === '') {
        gagal(
            'id_kunjungan, latitude_checkout, dan longitude_checkout wajib diisi.',
            'MISSING_CHECKOUT_DATA',
            400
        );
    }

    $stmt = $pdo->prepare(
        'SELECT k.*, t.nama_toko
         FROM tbl_kunjungan k
         JOIN tbl_toko t ON t.id_toko = k.id_toko
         WHERE k.id_kunjungan = ? AND k.id_sales = ?
         LIMIT 1'
    );
    $stmt->execute([$idKunjungan, $idSales]);
    $kunjungan = $stmt->fetch();

    if (!$kunjungan) {
        gagal('Kunjungan tidak ditemukan atau bukan milik Anda.', 'KUNJUNGAN_NOT_FOUND', 404);
    }

    if ($kunjungan['waktu_checkout'] !== null) {
        gagal('Kunjungan ini sudah check-out.', 'ALREADY_CHECKED_OUT', 409);
    }

    $durasi = max(0, (int)round((time() - strtotime((string)$kunjungan['waktu_checkin'])) / 60));

    try {
        $stmt = $pdo->prepare(
            'UPDATE tbl_kunjungan
             SET waktu_checkout = NOW(),
                 durasi_menit = ?,
                 latitude_checkout = ?,
                 longitude_checkout = ?
             WHERE id_kunjungan = ? AND id_sales = ?'
        );
        $stmt->execute([$durasi, (float)$lat, (float)$lon, $idKunjungan, $idSales]);
    } catch (PDOException $e) {
        error_log('[absen:checkout] ' . $e->getMessage());
        gagal('Gagal menyimpan check-out.', 'CHECKOUT_FAILED', 500);
    }

    sukses([
        'id_kunjungan'      => $idKunjungan,
        'nama_toko'         => $kunjungan['nama_toko'],
        'waktu_checkout'    => date('Y-m-d H:i:s'),
        'durasi_menit'      => $durasi,
        'status_verifikasi' => $kunjungan['status_verifikasi'],
    ], 'Check-out berhasil.');
}

gagal("Aksi tidak valid. Gunakan 'checkin' atau 'checkout'.", 'INVALID_ACTION', 400);
