using AbsensiFaceAppToken.Services;

namespace AbsensiFaceAppToken;

public partial class MainPage : ContentPage
{
    private readonly FaceService _face = new();

    public MainPage()
    {
        InitializeComponent();
    }

    // ---------------- Pendaftaran: foto -> token ----------------

    private async void OnEnrollGallery(object? sender, EventArgs e) => await EnrollAsync(fromCamera: false);
    private async void OnEnrollCamera(object? sender, EventArgs e) => await EnrollAsync(fromCamera: true);

    private async Task EnrollAsync(bool fromCamera)
    {
        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            SetStatus("Isi nama dulu.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            var photo = await GetPhotoAsync(fromCamera);
            if (photo == null) { SetStatus("Dibatalkan."); return; }

            var result = await ExtractFromPhotoAsync(photo);
            if (result.Embedding == null) { SetStatus(result.Error ?? "Gagal memproses foto."); return; }

            var token = TokenStore.ToToken(result.Embedding);
            _face.Store.Save(name, token);

            SetStatus($"[OK] Token '{name}' tersimpan ({token.Length} karakter). " +
                      $"Wajah terdeteksi: {result.FaceCount}. Foto tidak disimpan oleh aplikasi.");
        });
    }

    // ---------------- Verifikasi: foto kamera -> cocokkan token ----------------

    private async void OnVerify(object? sender, EventArgs e)
    {
        if (_face.Store.LoadAll().Count == 0)
        {
            SetStatus("Belum ada token. Daftarkan wajah dulu.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            var photo = await GetPhotoAsync(fromCamera: true);
            if (photo == null) { SetStatus("Dibatalkan."); return; }

            var result = await ExtractFromPhotoAsync(photo);
            if (result.Embedding == null) { SetStatus(result.Error ?? "Gagal memproses foto."); return; }

            var (name, score) = _face.FindBestMatch(result.Embedding);
            bool match = score >= FaceService.MatchThreshold;

            SetStatus(match
                ? $"[COCOK] {name} (skor {score:F2}, batas {FaceService.MatchThreshold})"
                : $"[TIDAK COCOK] skor tertinggi {score:F2} dengan '{name}' (batas {FaceService.MatchThreshold})");
        });
    }

    // ---------------- Lihat / hapus token ----------------

    private void OnListTokens(object? sender, EventArgs e)
    {
        var all = _face.Store.LoadAll();
        if (all.Count == 0) { SetStatus("Belum ada token tersimpan."); return; }

        var lines = all.Select(t =>
        {
            var preview = t.Token.Length > 30 ? t.Token.Substring(0, 30) + "..." : t.Token;
            return $"- {t.Name} | {t.Token.Length} karakter | {preview}";
        });
        SetStatus(string.Join("\n", lines));
    }

    private async void OnClearTokens(object? sender, EventArgs e)
    {
        bool yes = await DisplayAlert("Hapus token", "Hapus semua token tersimpan?", "Ya", "Batal");
        if (!yes) return;
        _face.Store.Clear();
        SetStatus("Semua token dihapus.");
    }

    // ---------------- Helper ----------------

    // Izin kamera diminta di sini (kode bersama), bukan di folder Platforms
    private async Task<FileResult?> GetPhotoAsync(bool fromCamera)
    {
        if (!fromCamera)
            return await MediaPicker.Default.PickPhotoAsync();

        if (!MediaPicker.Default.IsCaptureSupported)
        {
            SetStatus("Perangkat ini tidak mendukung pengambilan foto langsung dari kamera.");
            return null;
        }

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            SetStatus("Izin kamera ditolak. Aktifkan lewat pengaturan perangkat.");
            return null;
        }

        return await MediaPicker.Default.CapturePhotoAsync();
    }

    private async Task<FaceResult> ExtractFromPhotoAsync(FileResult photo)
    {
        byte[] bytes;
        using (var stream = await photo.OpenReadAsync())
        using (var ms = new MemoryStream())
        {
            await stream.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        Preview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        SetStatus("Memproses wajah...");
        return await _face.ExtractAsync(new MemoryStream(bytes));
    }

    private async Task RunSafeAsync(Func<Task> work)
    {
        Busy.IsRunning = true;
        try
        {
            if (!_face.IsReady)
            {
                SetStatus("Memuat model (pertama kali agak lama)...");
                await _face.InitAsync();
            }
            await work();
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            Busy.IsRunning = false;
        }
    }

    private void SetStatus(string text) => StatusLabel.Text = text;
}
