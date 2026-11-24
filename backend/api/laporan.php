<?php
/**
 * laporan.php — laporan untuk HR (butuh token role HR).
 *
 * Query:
 *   ?jenis=kunjungan   (default)
 *   ?jenis=permohonan
 *   ?jenis=sales
 *   &periode=2026-11
 *   &tanggal=2026-11-12   (khusus kunjungan)
 *
 * Response:
 * {
 *   "success": true,
 *   "data": {
 *     "periode": "2026-11",
 *     "ringkasan": { ... },
 *     "items": [ ... ]
 *   }
 * }
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

$pengguna = wajibLogin($pdo);
wajibRole($pengguna, 'HR');
wajibMethod('GET');

if ($pengguna['id_hr'] === null) {
    gagal('Profil HR tidak ditemukan untuk akun ini.', 'HR_PROFILE_NOT_FOUND', 400);
}

$jenis = strtolower(trim((string)($_GET['jenis'] ?? 'kunjungan')));
$periode = trim((string)($_GET['periode'] ?? date('Y-m')));
$tanggal = trim((string)($_GET['tanggal'] ?? ''));

/* ============================================================
 * Laporan kunjungan
 * ============================================================ */
if ($jenis === 'kunjungan') {
    $where = [];
    $params = [];

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

    if ($tanggal !== '') {
        $where[] = 'DATE(k.waktu_checkin) = ?';
        $params[] = $tanggal;
        $label = $tanggal;
    } else {
        $where[] = "DATE_FORMAT(k.waktu_checkin, '%Y-%m') = ?";
        $params[] = $periode;
        $label = $periode;
    }

    $sql = "SELECT
                k.id_kunjungan, k.id_sales, k.id_toko,
                s.nomor_sales, p.nama AS nama_sales,
                t.nama_toko, t.alamat AS alamat_toko,
                k.waktu_checkin, k.waktu_checkout,
                COALESCE(k.durasi_menit,
                         CASE WHEN k.waktu_checkout IS NOT NULL
                              THEN TIMESTAMPDIFF(MINUTE, k.waktu_checkin, k.waktu_checkout)
                              ELSE NULL END) AS durasi_menit,
                k.latitude_checkin, k.longitude_checkin,
                k.latitude_checkout, k.longitude_checkout,
                k.foto_bukti, k.status_verifikasi,
                {$selectJarak},
                {$selectSkorWajah},
                {$selectTujuan},
                {$selectCatatan},
                {$selectKodeToko},
                {$selectWilayah}
            FROM tbl_kunjungan k
            JOIN tbl_sales s ON s.id_sales = k.id_sales
            JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna
            JOIN tbl_toko t ON t.id_toko = k.id_toko
            WHERE " . implode(' AND ', $where) . "
            ORDER BY k.waktu_checkin DESC
            LIMIT 2000";

    $stmt = $pdo->prepare($sql);
    $stmt->execute($params);
    $items = $stmt->fetchAll();

    foreach ($items as &$row) {
        $row['id_kunjungan'] = (int)$row['id_kunjungan'];
        $row['id_sales'] = (int)$row['id_sales'];
        $row['id_toko'] = (int)$row['id_toko'];
        $row['durasi_menit'] = $row['durasi_menit'] !== null ? (int)$row['durasi_menit'] : null;
        $row['jarak_meter'] = $row['jarak_meter'] !== null ? (float)$row['jarak_meter'] : null;
        $row['skor_wajah'] = $row['skor_wajah'] !== null ? (float)$row['skor_wajah'] : null;
    }
    unset($row);

    $total = count($items);
    $valid = 0;
    $pending = 0;
    $invalid = 0;
    foreach ($items as $row) {
        if ($row['status_verifikasi'] === 'Valid') {
            $valid++;
        } elseif ($row['status_verifikasi'] === 'Invalid') {
            $invalid++;
        } else {
            $pending++;
        }
    }

    sukses([
        'periode'   => $label,
        'ringkasan' => [
            'total_kunjungan' => $total,
            'valid'           => $valid,
            'pending'         => $pending,
            'invalid'         => $invalid,
        ],
        'items' => $items,
    ], 'Laporan kunjungan.');
}

/* ============================================================
 * Laporan permohonan
 * ============================================================ */
if ($jenis === 'permohonan') {
    $kolomPermohonan = kolomTabel($pdo, 'tbl_permohonan');
    $selectLampiran = in_array('lampiran', $kolomPermohonan, true)
        ? 'pm.lampiran' : 'NULL AS lampiran';
    $selectCatatanHr = in_array('catatan_hr', $kolomPermohonan, true)
        ? 'pm.catatan_hr' : 'NULL AS catatan_hr';

    $stmt = $pdo->prepare(
        "SELECT pm.id_permohonan, pm.id_sales,
                s.nomor_sales, p.nama AS nama_sales,
                pm.jenis, pm.tanggal_mulai, pm.tanggal_selesai,
                pm.alasan, pm.status, pm.id_hr_approver,
                pm.created_at,
                {$selectLampiran},
                {$selectCatatanHr}
         FROM tbl_permohonan pm
         JOIN tbl_sales s ON s.id_sales = pm.id_sales
         JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna
         WHERE DATE_FORMAT(pm.tanggal_mulai, '%Y-%m') = ?
         ORDER BY pm.created_at DESC
         LIMIT 2000"
    );
    $stmt->execute([$periode]);
    $items = $stmt->fetchAll();

    $ringkasan = ['total' => count($items), 'pending' => 0, 'approved' => 0, 'rejected' => 0];
    foreach ($items as $row) {
        $key = strtolower((string)$row['status']);
        if (array_key_exists($key, $ringkasan)) {
            $ringkasan[$key]++;
        }
    }

    sukses([
        'periode'   => $periode,
        'ringkasan' => $ringkasan,
        'items'     => $items,
    ], 'Laporan permohonan.');
}

/* ============================================================
 * Rekap per sales
 * ============================================================ */
if ($jenis === 'sales') {
    $stmt = $pdo->prepare(
        "SELECT
            s.id_sales,
            s.nomor_sales,
            p.nama AS nama_sales,
            p.email,
            SUM(CASE WHEN DATE_FORMAT(k.waktu_checkin, '%Y-%m') = ?
                      AND k.status_verifikasi = 'Valid' THEN 1 ELSE 0 END) AS kunjungan_valid,
            SUM(CASE WHEN DATE_FORMAT(k.waktu_checkin, '%Y-%m') = ? THEN 1 ELSE 0 END) AS total_kunjungan,
            (SELECT COUNT(*) FROM tbl_permohonan pm
              WHERE pm.id_sales = s.id_sales AND pm.status = 'Pending') AS permohonan_pending
         FROM tbl_sales s
         JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna
         LEFT JOIN tbl_kunjungan k ON k.id_sales = s.id_sales
         GROUP BY s.id_sales, s.nomor_sales, p.nama, p.email
         ORDER BY kunjungan_valid DESC, p.nama ASC"
    );
    $stmt->execute([$periode, $periode]);
    $items = $stmt->fetchAll();

    foreach ($items as &$row) {
        $row['id_sales'] = (int)$row['id_sales'];
        $row['kunjungan_valid'] = (int)$row['kunjungan_valid'];
        $row['total_kunjungan'] = (int)$row['total_kunjungan'];
        $row['permohonan_pending'] = (int)$row['permohonan_pending'];
    }
    unset($row);

    sukses([
        'periode'   => $periode,
        'ringkasan' => ['jumlah_sales' => count($items)],
        'items'     => $items,
    ], 'Rekap sales.');
}

gagal(
    "Jenis laporan tidak valid. Gunakan 'kunjungan', 'permohonan', atau 'sales'.",
    'INVALID_REPORT_TYPE',
    400
);
