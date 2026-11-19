using AbsensiSales.Mobile.ViewModels;

namespace AbsensiSales.Mobile.Views;

public partial class VerifikasiWajahPage : ContentPage
{
    private const string NamaAnimasiGaris = "GarisPindai";

    private readonly VerifikasiWajahViewModel _viewModel;

    public VerifikasiWajahPage(VerifikasiWajahViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Window is not null)
        {
            Window.Resumed += OnWindowResumed;
        }

        MulaiAnimasiGaris();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        if (Window is not null)
        {
            Window.Resumed -= OnWindowResumed;
        }

        this.AbortAnimation(NamaAnimasiGaris);
        _viewModel.OnDisappearing();
        base.OnDisappearing();
    }

    /// <summary>
    /// Saat pengguna kembali dari pengaturan izin kamera, pemindaian dicoba lagi
    /// otomatis tanpa perlu menekan tombol apa pun.
    /// </summary>
    private void OnWindowResumed(object? sender, EventArgs e)
    {
        MulaiAnimasiGaris();
        _viewModel.OnAppearing();
    }

    private void MulaiAnimasiGaris()
    {
        this.AbortAnimation(NamaAnimasiGaris);

        var keBawah = new Animation(v => GarisPindai.TranslationY = v, -50, 50, Easing.SinInOut);
        var keAtas = new Animation(v => GarisPindai.TranslationY = v, 50, -50, Easing.SinInOut);

        var animasi = new Animation();
        animasi.Add(0, 0.5, keBawah);
        animasi.Add(0.5, 1, keAtas);

        animasi.Commit(this, NamaAnimasiGaris, 16, 2000, repeat: () => true);
    }
}
