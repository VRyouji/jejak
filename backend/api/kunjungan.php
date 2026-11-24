<?php
/**
 * kunjungan.php — GET riwayat kunjungan.
 *
 * Query opsional:
 *   ?tanggal=2026-11-12   -> filter satu hari
 *   ?periode=2026-11      -> filter satu bulan
 *   ?status=Pending       -> Pending | Valid | Invalid
 *   ?id_sales=3           -> hanya HR yang boleh memfilter sales lain
 *   ?id_kunjungan=10      -> ambil satu kunjungan
 *
 * Sales otomatis hanya melihat kunjungan miliknya sendiri.
 * HR melihat semua sales.
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

$pengguna = wajibLogin($pdo);
wajibMethod('GET');

$role = (string)$pengguna['role'];
$idSalesLogin = $pengguna['id_sales'];

$where = [];
$params = [];

if (strcasecmp($role, 'Sales') === 0) {
    if ($idSalesLogin === null) {
        gagal('Profil sales tidak ditemukan untuk akun ini.', 'SALES_PROFILE_NOT_FOUND', 400);
    }
    $where[] = 'k.id_sales = ?';
    $params[] = (int)$idSalesLogin;
} elseif (isset($_GET['id_sales']) && (int)$_GET['id_sales'] > 0) {
    $where[] = 'k.id_sales = ?';
    $params[] = (int)$_GET['id_sales'];
}

if (isset($_GET['id_kunjungan']) && (int)$_GET['id_kunjungan'] > 0) {
    $where[] = 'k.id_kunjungan = ?';
    $params[] = (int)$_GET['id_kunjungan'];
}

if (!empty($_GET['tanggal'])) {
    $where[] = 'DATE(k.waktu_checkin) = ?';
    $params[] = (string)$_GET['tanggal'];
}

if (!empty($_GET['periode'])) {
    $where[] = "DATE_FORMAT(k.waktu_checkin, '%Y-%m') = ?";
    $params[] = (string)$_GET['periode'];
}

if (!empty($_GET['id_toko'])) {
    $where[] = 'k.id_toko = ?';
    $params[] = (int)$_GET['id_toko'];
}

if (!empty($_GET['status'])) {
    $where[] = 'k.status_verifikasi = ?';
    $params[] = (string)$_GET['status'];
}

// Kolom opsional: tetap jalan walau 02_optional_columns.sql belum dijalankan.
$kolomKunjungan = kolomTabel($pdo, 'tbl_kunjungan');
$kolomToko = kolomTabel($pdo, 'tbl_toko');
$kolomPengguna = kolomTabel($pdo, 'tbl_pengguna');

$selectJarak = in_array('jarak_meter', $kolomKunjungan, true)
    ? 'k.jarak_meter' : 'NULL AS jarak_meter';
$selectSkorWajah = in_array('skor_wajah', $kolomKunjungan, true)
    ? 'k.skor_wajah' : 'NULL AS skor_wajah';
$selectTujuan = in_array('tujuan_kunjungan', $kolomKunjungan, true)
    ? 'k.tujuan_kunjungan' : 'NULL AS tujuan_kunjungan';
$selectCatatan = in_array('catatan', $kolomKunjungan, true)
    ? 'k.catatan' : 'NULL AS catatan';

$selectKodeToko = in_array('kode_toko', $kolomToko, true)
    ? "COALESCE(NULLIF(t.kode_toko, ''), CONCAT('TK-', t.id_toko)) AS kode_toko"
    : "CONCAT('TK-', t.id_toko) AS kode_toko";

$selectWilayah = in_array('wilayah', $kolomPengguna, true)
    ? 'p.wilayah' : 'NULL AS wilayah';

$sql = "SELECT
            k.id_kunjungan,
            k.id_sales,
            k.id_toko,
            k.waktu_checkin,
            k.waktu_checkout,
            COALESCE(k.durasi_menit,
                     CASE WHEN k.waktu_checkout IS NOT NULL
                          THEN TIMESTAMPDIFF(MINUTE, k.waktu_checkin, k.waktu_checkout)
                          ELSE NULL END) AS durasi_menit,
            k.latitude_checkin,
            k.longitude_checkin,
            k.latitude_checkout,
            k.longitude_checkout,
            k.foto_bukti,
            k.status_verifikasi,
            {$selectJarak},
            {$selectSkorWajah},
            {$selectTujuan},
            {$selectCatatan},
            t.nama_toko,
            t.alamat AS alamat_toko,
            t.latitude AS latitude_toko,
            t.longitude AS longitude_toko,
            t.radius AS radius_toko,
            {$selectKodeToko},
            s.nomor_sales,
            p.nama AS nama_sales,
            {$selectWilayah}
        FROM tbl_kunjungan k
        JOIN tbl_toko t ON t.id_toko = k.id_toko
        JOIN tbl_sales s ON s.id_sales = k.id_sales
        JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna";

if ($where) {
    $sql .= ' WHERE ' . implode(' AND ', $where);
}
$sql .= ' ORDER BY k.waktu_checkin DESC LIMIT 500';

$stmt = $pdo->prepare($sql);
$stmt->execute($params);
$rows = $stmt->fetchAll();

foreach ($rows as &$row) {
    $row['id_kunjungan'] = (int)$row['id_kunjungan'];
    $row['id_sales'] = (int)$row['id_sales'];
    $row['id_toko'] = (int)$row['id_toko'];
    $row['durasi_menit'] = $row['durasi_menit'] !== null ? (int)$row['durasi_menit'] : null;
    $row['jarak_meter'] = $row['jarak_meter'] !== null ? (float)$row['jarak_meter'] : null;
    $row['skor_wajah'] = $row['skor_wajah'] !== null ? (float)$row['skor_wajah'] : null;
    $row['radius_toko'] = $row['radius_toko'] !== null ? (float)$row['radius_toko'] : null;
    $row['latitude_toko'] = $row['latitude_toko'] !== null ? (float)$row['latitude_toko'] : null;
    $row['longitude_toko'] = $row['longitude_toko'] !== null ? (float)$row['longitude_toko'] : null;
}

unset($row);

sukses($rows, 'Data kunjungan.');
