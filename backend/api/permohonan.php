<?php
/**
 * permohonan.php — izin / cuti.
 *
 * GET  ?status=Pending&periode=2026-11
 *      Sales: hanya miliknya. HR: semua.
 *
 * POST (Sales) membuat permohonan:
 * {
 *   "jenis": "Cuti",                     <- "Cuti" | "Izin Sakit"
 *   "tanggal_mulai": "2026-11-20",
 *   "tanggal_selesai": "2026-11-21",
 *   "alasan": "Acara keluarga"
 * }
 *
 * POST (HR) menyetujui / menolak:
 * {
 *   "action": "approve",                 <- "approve" | "reject"
 *   "id_permohonan": 5,
 *   "catatan": "Disetujui."              <- opsional
 * }
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

$pengguna = wajibLogin($pdo);
$role = (string)$pengguna['role'];
$method = strtoupper($_SERVER['REQUEST_METHOD'] ?? 'GET');

/* ============================================================
 * GET — daftar permohonan
 * ============================================================ */
if ($method === 'GET') {
    $where = [];
    $params = [];

    $kolomPermohonan = kolomTabel($pdo, 'tbl_permohonan');
    $selectLampiran = in_array('lampiran', $kolomPermohonan, true)
        ? 'pm.lampiran' : 'NULL AS lampiran';
    $selectCatatanHr = in_array('catatan_hr', $kolomPermohonan, true)
        ? 'pm.catatan_hr' : 'NULL AS catatan_hr';

    if (strcasecmp($role, 'Sales') === 0) {
        if ($pengguna['id_sales'] === null) {
            gagal('Profil sales tidak ditemukan untuk akun ini.', 'SALES_PROFILE_NOT_FOUND', 400);
        }
        $where[] = 'pm.id_sales = ?';
        $params[] = (int)$pengguna['id_sales'];
    } elseif (!empty($_GET['id_sales'])) {
        $where[] = 'pm.id_sales = ?';
        $params[] = (int)$_GET['id_sales'];
    }

    if (!empty($_GET['status'])) {
        $where[] = 'pm.status = ?';
        $params[] = (string)$_GET['status'];
    }

    if (!empty($_GET['periode'])) {
        $where[] = "DATE_FORMAT(pm.tanggal_mulai, '%Y-%m') = ?";
        $params[] = (string)$_GET['periode'];
    }

    if (!empty($_GET['id_permohonan'])) {
        $where[] = 'pm.id_permohonan = ?';
        $params[] = (int)$_GET['id_permohonan'];
    }

    $sql = "SELECT
                pm.id_permohonan,
                pm.id_sales,
                s.nomor_sales,
                p.nama AS nama_sales,
                pm.jenis,
                pm.tanggal_mulai,
                pm.tanggal_selesai,
                pm.alasan,
                pm.status,
                pm.id_hr_approver,
                pm.created_at,
                {$selectLampiran},
                {$selectCatatanHr}
            FROM tbl_permohonan pm
            JOIN tbl_sales s ON s.id_sales = pm.id_sales
            JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna";

    if ($where) {
        $sql .= ' WHERE ' . implode(' AND ', $where);
    }
    $sql .= ' ORDER BY pm.created_at DESC, pm.id_permohonan DESC LIMIT 500';

    $stmt = $pdo->prepare($sql);
    $stmt->execute($params);
    $rows = $stmt->fetchAll();

    foreach ($rows as &$row) {
        $row['id_permohonan'] = (int)$row['id_permohonan'];
        $row['id_sales'] = (int)$row['id_sales'];
    }
    unset($row);

    sukses($rows, 'Data permohonan.');
}

/* ============================================================
 * POST — buat (Sales) / approve-reject (HR)
 * ============================================================ */
wajibMethod('POST');
$data = inputJson();
$action = strtolower((string)(ambil($data, 'action') ?? 'create'));

if ($action === 'approve' || $action === 'reject') {
    wajibRole($pengguna, 'HR');

    if ($pengguna['id_hr'] === null) {
        gagal('Profil HR tidak ditemukan untuk akun ini.', 'HR_PROFILE_NOT_FOUND', 400);
    }

    $idPermohonan = (int)(ambil($data, 'id_permohonan', 0));
    $catatan = ambil($data, 'catatan');
    $statusBaru = $action === 'approve' ? 'Approved' : 'Rejected';

    if ($idPermohonan <= 0) {
        gagal('id_permohonan wajib diisi.', 'MISSING_ID_PERMOHONAN', 400);
    }

    $stmt = $pdo->prepare('SELECT id_permohonan, status FROM tbl_permohonan WHERE id_permohonan = ? LIMIT 1');
    $stmt->execute([$idPermohonan]);
    $permohonan = $stmt->fetch();

    if (!$permohonan) {
        gagal('Permohonan tidak ditemukan.', 'PERMOHONAN_NOT_FOUND', 404);
    }

    // Kolom catatan_hr opsional (04_kolom_admin_hr.sql). Tanpa kolom itu
    // catatan tetap dikembalikan di response, tetapi tidak dipersist.
    $kolomPermohonan = kolomTabel($pdo, 'tbl_permohonan');
    $adaCatatanHr = in_array('catatan_hr', $kolomPermohonan, true);

    if ($adaCatatanHr) {
        $stmt = $pdo->prepare(
            'UPDATE tbl_permohonan
             SET status = ?, id_hr_approver = ?, catatan_hr = ?
             WHERE id_permohonan = ?'
        );
        $stmt->execute([
            $statusBaru,
            (int)$pengguna['id_hr'],
            $catatan !== null && $catatan !== '' ? (string)$catatan : null,
            $idPermohonan,
        ]);
    } else {
        $stmt = $pdo->prepare(
            'UPDATE tbl_permohonan
             SET status = ?, id_hr_approver = ?
             WHERE id_permohonan = ?'
        );
        $stmt->execute([$statusBaru, (int)$pengguna['id_hr'], $idPermohonan]);
    }

    sukses([
        'id_permohonan'  => $idPermohonan,
        'status'         => $statusBaru,
        'id_hr_approver' => (int)$pengguna['id_hr'],
        'catatan'        => $catatan,
        'catatan_hr'     => $adaCatatanHr ? $catatan : null,
    ], $action === 'approve' ? 'Permohonan disetujui.' : 'Permohonan ditolak.');
}

// Default: Sales membuat permohonan baru.
wajibRole($pengguna, 'Sales');

if ($pengguna['id_sales'] === null) {
    gagal('Profil sales tidak ditemukan untuk akun ini.', 'SALES_PROFILE_NOT_FOUND', 400);
}

$jenis = (string)(ambil($data, 'jenis') ?? '');
$tanggalMulai = (string)(ambil($data, 'tanggal_mulai') ?? '');
$tanggalSelesai = (string)(ambil($data, 'tanggal_selesai') ?? $tanggalMulai);
$alasan = (string)(ambil($data, 'alasan') ?? '');
$lampiran = (string)(ambil($data, 'lampiran') ?? '');

if (!in_array($jenis, ['Cuti', 'Izin Sakit'], true)) {
    gagal("Jenis permohonan harus 'Cuti' atau 'Izin Sakit'.", 'INVALID_JENIS', 400);
}

if (!preg_match('/^\d{4}-\d{2}-\d{2}$/', $tanggalMulai)
    || !preg_match('/^\d{4}-\d{2}-\d{2}$/', $tanggalSelesai)) {
    gagal('Tanggal harus berformat YYYY-MM-DD.', 'INVALID_DATE', 400);
}

if ($tanggalSelesai < $tanggalMulai) {
    gagal('Tanggal selesai tidak boleh sebelum tanggal mulai.', 'INVALID_DATE_RANGE', 400);
}

if ($alasan === '') {
    gagal('Alasan wajib diisi.', 'MISSING_ALASAN', 400);
}

$kolomPermohonan = kolomTabel($pdo, 'tbl_permohonan');
$adaLampiran = in_array('lampiran', $kolomPermohonan, true);

try {
    if ($adaLampiran) {
        $stmt = $pdo->prepare(
            "INSERT INTO tbl_permohonan
                (id_sales, jenis, tanggal_mulai, tanggal_selesai, alasan, lampiran, status)
             VALUES (?, ?, ?, ?, ?, ?, 'Pending')"
        );
        $stmt->execute([
            (int)$pengguna['id_sales'],
            $jenis,
            $tanggalMulai,
            $tanggalSelesai,
            $alasan,
            $lampiran !== '' ? $lampiran : null,
        ]);
    } else {
        $stmt = $pdo->prepare(
            "INSERT INTO tbl_permohonan
                (id_sales, jenis, tanggal_mulai, tanggal_selesai, alasan, status)
             VALUES (?, ?, ?, ?, ?, 'Pending')"
        );
        $stmt->execute([
            (int)$pengguna['id_sales'],
            $jenis,
            $tanggalMulai,
            $tanggalSelesai,
            $alasan,
        ]);
    }

    $idPermohonan = (int)$pdo->lastInsertId();
} catch (PDOException $e) {
    error_log('[permohonan:create] ' . $e->getMessage());
    gagal('Permohonan gagal disimpan.', 'PERMOHONAN_FAILED', 500);
}

sukses([
    'id_permohonan'  => $idPermohonan,
    'id_sales'       => (int)$pengguna['id_sales'],
    'jenis'          => $jenis,
    'tanggal_mulai'  => $tanggalMulai,
    'tanggal_selesai'=> $tanggalSelesai,
    'alasan'         => $alasan,
    'lampiran'       => $adaLampiran && $lampiran !== '' ? $lampiran : null,
    'status'         => 'Pending',
], 'Permohonan berhasil dikirim.', 201);
