<?php
/**
 * db.php — koneksi database (PDO).
 * Di-include oleh semua endpoint lewat auth.php.
 *
 * Jangan ubah file ini. Ubah kredensial di config.php.
 */

declare(strict_types=1);

require_once __DIR__ . '/helpers.php';

$config = require __DIR__ . '/config.php';

$dsn = sprintf(
    'mysql:host=%s;dbname=%s;charset=utf8mb4',
    $config['host'],
    $config['database']
);

try {
    $pdo = new PDO($dsn, $config['username'], $config['password'], [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
        PDO::ATTR_EMULATE_PREPARES => false,
    ]);
} catch (PDOException $e) {
    // Jangan bocorkan password / detail koneksi ke client.
    error_log('[db] ' . $e->getMessage());
    gagal(
        'Koneksi database gagal. Periksa host, nama database, username, dan password di config.php.',
        'DB_CONNECTION_ERROR',
        500
    );
}

// Timezone sesi MySQL, supaya NOW() sama dengan jam WIB.
$pdo->exec("SET time_zone = '+07:00'");
