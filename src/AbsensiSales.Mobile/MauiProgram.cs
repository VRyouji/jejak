using AbsensiSales.Core.Api;
using AbsensiSales.Core.Services;
using AbsensiSales.Mobile.Services;
using AbsensiSales.Mobile.ViewModels;
using AbsensiSales.Mobile.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace AbsensiSales.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Seluruh data Mobile berasal dari backend API.
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<ApiService>();

        // Layanan aplikasi
        builder.Services.AddSingleton<AbsensiFlowState>();
        builder.Services.AddSingleton<GpsService>();
        builder.Services.AddSingleton<FaceService>();
        builder.Services.AddSingleton<NotificationService>();

        // Shell (di-inject agar bisa lewat DI)
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<AbsensiPertamaViewModel>();
        builder.Services.AddTransient<PengisianTokoViewModel>();
        builder.Services.AddTransient<VerifikasiWajahViewModel>();
        builder.Services.AddTransient<FotoTokoViewModel>();
        builder.Services.AddTransient<VerifikasiDataViewModel>();
        builder.Services.AddTransient<LaporanViewModel>();
        builder.Services.AddTransient<PengajuanIzinViewModel>();
        builder.Services.AddTransient<StatusPermohonanViewModel>();

        // Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<AbsensiPertamaPage>();
        builder.Services.AddTransient<PengisianTokoPage>();
        builder.Services.AddTransient<VerifikasiWajahPage>();
        builder.Services.AddTransient<FotoTokoPage>();
        builder.Services.AddTransient<VerifikasiDataPage>();
        builder.Services.AddTransient<LaporanPage>();
        builder.Services.AddTransient<PengajuanIzinPage>();
        builder.Services.AddTransient<StatusPermohonanPage>();

        return builder.Build();
    }
}
