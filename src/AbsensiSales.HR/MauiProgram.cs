using AbsensiSales.Core.Data;
using AbsensiSales.Core.Repositories;
using AbsensiSales.Core.Services;
using AbsensiSales.HR.ViewModels;
using AbsensiSales.HR.Views;
using Microsoft.Extensions.Logging;

namespace AbsensiSales.HR;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Data & sesi - folder database sama dengan aplikasi Mobile
        // sehingga kedua aplikasi berbagi data SQLite yang sama.
        builder.Services.AddSingleton<AppDatabase>();
        builder.Services.AddSingleton<SalesRepository>();
        builder.Services.AddSingleton<TokoRepository>();
        builder.Services.AddSingleton<KunjunganRepository>();
        builder.Services.AddSingleton<PermohonanRepository>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<AbsensiSales.HR.Services.HrNavigationService>();
        builder.Services.AddSingleton<AbsensiSales.HR.Services.PdfService>();

        // Shell
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddTransient<HrLoginViewModel>();
        builder.Services.AddTransient<HrRegisterViewModel>();
        builder.Services.AddTransient<MonitoringViewModel>();
        builder.Services.AddTransient<LaporanHrViewModel>();
        builder.Services.AddTransient<DetailKunjunganViewModel>();
        builder.Services.AddTransient<ApprovalViewModel>();
        builder.Services.AddTransient<HrDashboardViewModel>();
        builder.Services.AddTransient<HrPerizinanViewModel>();
        builder.Services.AddTransient<MasterDataViewModel>();
        builder.Services.AddTransient<PayrollViewModel>();

        // Views
        builder.Services.AddTransient<HrLoginPage>();
        builder.Services.AddTransient<HrRegisterPage>();
        builder.Services.AddTransient<WorkspacePage>();
        builder.Services.AddTransient<MonitoringPage>();
        builder.Services.AddTransient<LaporanHrPage>();
        builder.Services.AddTransient<DetailKunjunganPage>();
        builder.Services.AddTransient<ApprovalPage>();
        builder.Services.AddTransient<HrDashboardPage>();
        builder.Services.AddTransient<HrPerizinanPage>();
        builder.Services.AddTransient<MasterDataPage>();
        builder.Services.AddTransient<PayrollPage>();

        return builder.Build();
    }
}
