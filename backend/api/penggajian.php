<?php
/**
 * penggajian.php — rekap & proses penggajian (khusus HR).
 *
 * GET  ?periode=2026-11
 *   Menghitung gaji dari pengaturan gaji + kunjungan valid + izin disetujui.
 *
 * POST
 * {
 *   "action": "process",
 *   "periode": "2026-11"
 * }
 *   Menyimpan / memperbarui rekap ke tbl_penggajian.
 */

declare(strict_types=1);

require_once __DIR__ . '/auth.php';

const HARI_KERJA_BULANAN = 22;
const TARGET_KUNJUNGAN_BULANAN = 176;

/**
 * Hitung rekap gaji satu periode.
 * Mengembalikan null bila tbl_pengaturan_gaji belum diisi.
 */
function hitungRekapGaji(PDO $pdo, string $periode): ?array
{
    $awalPeriode = $periode . '-01';
    $akhirPeriode = date('Y-m-t', strtotime($awalPeriode));

    $stmt = $pdo->prepare(
        'SELECT id_pengaturan, gaji_pokok, insentif_per_kunjungan, periode_berlaku
         FROM tbl_pengaturan_gaji
         WHERE periode_berlaku <= ?
         ORDER BY periode_berlaku DESC
         LIMIT 1'
    );
    $stmt->execute([$akhirPeriode]);
    $pengaturan = $stmt->fetch();

    // Kalau belum ada yang berlaku sebelum periode ini, pakai pengaturan terbaru.
    if (!$pengaturan) {
        $pengaturan = $pdo->query(
            'SELECT id_pengaturan, gaji_pokok, insentif_per_kunjungan, periode_berlaku
             FROM tbl_pengaturan_gaji
             ORDER BY periode_berlaku DESC
             LIMIT 1'
        )->fetch();
    }

    if (!$pengaturan) {
        return null;
    }

    $gajiPokok = (float)$pengaturan['gaji_pokok'];
    $insentifPerKunjungan = (float)$pengaturan['insentif_per_kunjungan'];

    // Jumlah kunjungan yang sudah diverifikasi valid pada periode ini.
    $stmt = $pdo->prepare(
        "SELECT id_sales, COUNT(*) AS jumlah
         FROM tbl_kunjungan
         WHERE status_verifikasi = 'Valid'
           AND DATE_FORMAT(waktu_checkin, '%Y-%m') = ?
         GROUP BY id_sales"
    );
    $stmt->execute([$periode]);
    $kunjunganValid = [];
    foreach ($stmt->fetchAll() as $row) {
        $kunjunganValid[(int)$row['id_sales']] = (int)$row['jumlah'];
    }

    // Jumlah hari izin/cuti yang disetujui dan dimulai pada periode ini.
    $stmt = $pdo->prepare(
        "SELECT id_sales,
                COALESCE(SUM(GREATEST(DATEDIFF(tanggal_selesai, tanggal_mulai) + 1, 1)), 0) AS hari
         FROM tbl_permohonan
         WHERE status = 'Approved'
           AND DATE_FORMAT(tanggal_mulai, '%Y-%m') = ?
         GROUP BY id_sales"
    );
    $stmt->execute([$periode]);
    $hariIzin = [];
    foreach ($stmt->fetchAll() as $row) {
        $hariIzin[(int)$row['id_sales']] = (int)$row['hari'];
    }

    // Status proses yang sudah tersimpan.
    $stmt = $pdo->prepare(
        'SELECT id_sales, id_gaji, tanggal_proses
         FROM tbl_penggajian
         WHERE periode = ?'
    );
    $stmt->execute([$periode]);
    $tersimpan = [];
    foreach ($stmt->fetchAll() as $row) {
        $tersimpan[(int)$row['id_sales']] = $row;
    }

    $stmt = $pdo->query(
        'SELECT s.id_sales, s.nomor_sales, p.nama
         FROM tbl_sales s
         JOIN tbl_pengguna p ON p.id_pengguna = s.id_pengguna
         ORDER BY p.nama ASC'
    );
    $daftarSales = $stmt->fetchAll();

    $items = [];
    foreach ($daftarSales as $sales) {
        $idSales = (int)$sales['id_sales'];
        $jumlahKunjungan = $kunjunganValid[$idSales] ?? 0;
        $jumlahHariIzin = $hariIzin[$idSales] ?? 0;

        $hariKerja = max(0, HARI_KERJA_BULANAN - $jumlahHariIzin);
        $totalInsentif = $jumlahKunjungan * $insentifPerKunjungan;
        $potonganPerHari = $gajiPokok / HARI_KERJA_BULANAN;
        $totalPotongan = $jumlahHariIzin * $potonganPerHari;
        $totalGaji = max(0, $gajiPokok + $totalInsentif - $totalPotongan);

        $proses = $tersimpan[$idSales] ?? null;

        $items[] = [
            'id_sales'              => $idSales,
            'id_gaji'               => $proses ? (int)$proses['id_gaji'] : null,
            'nama_sales'            => $sales['nama'],
            'nomor_sales'           => $sales['nomor_sales'],
            'hari_kerja'            => $hariKerja,
            'kunjungan_valid'       => $jumlahKunjungan,
            'jumlah_izin'           => $jumlahHariIzin,
            'gaji_pokok'            => round($gajiPokok, 2),
            'insentif_per_kunjungan'=> round($insentifPerKunjungan, 2),
            'total_insentif'        => round($totalInsentif, 2),
            'total_potongan'        => round($totalPotongan, 2),
            'total_gaji'            => round($totalGaji, 2),
            'status_target'         => $jumlahKunjungan >= TARGET_KUNJUNGAN_BULANAN
                ? 'Target tercapai'
                : 'Belum tercapai',
            'sudah_diproses'        => $proses !== null,
            'tanggal_proses'        => $proses['tanggal_proses'] ?? null,
        ];
    }

    return [
        'periode'               => $periode,
        'pengaturan'            => [
            'id_pengaturan'        => (int)$pengaturan['id_pengaturan'],
            'gaji_pokok'           => round($gajiPokok, 2),
            'insentif_per_kunjungan' => round($insentifPerKunjungan, 2),
            'periode_berlaku'      => $pengaturan['periode_berlaku'],
        ],
        'hari_kerja_bulanan'    => HARI_KERJA_BULANAN,
        'target_kunjungan'      => TARGET_KUNJUNGAN_BULANAN,
        'items'                 => $items,
    ];
}

function periodeValid(string $periode): bool
{
    return (bool)preg_match('/^\d{4}-(0[1-9]|1[0-2])$/', $periode);
}

$pengguna = wajibLogin($pdo);
wajibRole($pengguna, 'HR');

if ($pengguna['id_hr'] === null) {
    gagal('Profil HR tidak ditemukan untuk akun ini.', 'HR_PROFILE_NOT_FOUND', 400);
}

$method = strtoupper($_SERVER['REQUEST_METHOD'] ?? 'GET');

/* ============================================================
 * GET — rekap gaji terhitung
 * ============================================================ */
if ($method === 'GET') {
    $periode = trim((string)($_GET['periode'] ?? date('Y-m')));
    if (!periodeValid($periode)) {
        gagal("Format periode harus YYYY-MM, mis. '2026-11'.", 'INVALID_PERIODE', 400);
    }

    $rekap = hitungRekapGaji($pdo, $periode);
    if ($rekap === null) {
        gagal(
            'Pengaturan gaji belum ada. Isi tbl_pengaturan_gaji terlebih dahulu.',
            'PENGATURAN_GAJI_KOSONG',
            409
        );
    }

    sukses($rekap, 'Rekap penggajian.');
}

/* ============================================================
 * POST — proses & simpan rekap
 * ============================================================ */
wajibMethod('POST');

$data = inputJson();
$action = strtolower((string)(ambil($data, 'action') ?? 'process'));
$periode = trim((string)(ambil($data, 'periode') ?? date('Y-m')));

if ($action !== 'process') {
    gagal("Aksi tidak valid. Gunakan 'process'.", 'INVALID_ACTION', 400);
}

if (!periodeValid($periode)) {
    gagal("Format periode harus YYYY-MM, mis. '2026-11'.", 'INVALID_PERIODE', 400);
}

$rekap = hitungRekapGaji($pdo, $periode);
if ($rekap === null) {
    gagal(
        'Pengaturan gaji belum ada. Isi tbl_pengaturan_gaji terlebih dahulu.',
        'PENGATURAN_GAJI_KOSONG',
        409
    );
}

$idHr = (int)$pengguna['id_hr'];
$idPengaturan = (int)$rekap['pengaturan']['id_pengaturan'];

$stmt = $pdo->prepare(
    'INSERT INTO tbl_penggajian
        (id_sales, id_hr, id_pengaturan, periode, total_kunjungan_valid,
         gaji_pokok, total_insentif, total_potongan, total_gaji, tanggal_proses)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, NOW())
     ON DUPLICATE KEY UPDATE
        id_hr = VALUES(id_hr),
        id_pengaturan = VALUES(id_pengaturan),
        total_kunjungan_valid = VALUES(total_kunjungan_valid),
        gaji_pokok = VALUES(gaji_pokok),
        total_insentif = VALUES(total_insentif),
        total_potongan = VALUES(total_potongan),
        total_gaji = VALUES(total_gaji),
        tanggal_proses = NOW()'
);

try {
    $pdo->beginTransaction();

    foreach ($rekap['items'] as $item) {
        $stmt->execute([
            (int)$item['id_sales'],
            $idHr,
            $idPengaturan,
            $periode,
            (int)$item['kunjungan_valid'],
            (float)$item['gaji_pokok'],
            (float)$item['total_insentif'],
            (float)$item['total_potongan'],
            (float)$item['total_gaji'],
        ]);
    }

    $pdo->commit();
} catch (PDOException $e) {
    if ($pdo->inTransaction()) {
        $pdo->rollBack();
    }
    error_log('[penggajian:process] ' . $e->getMessage());
    gagal('Rekap penggajian gagal disimpan.', 'PENGGAJIAN_FAILED', 500);
}

$rekap = hitungRekapGaji($pdo, $periode) ?? $rekap;

sukses($rekap, 'Rekap penggajian periode ' . $periode . ' berhasil disimpan.');
