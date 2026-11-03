using AbsensiSales.HR.ViewModels;

namespace AbsensiSales.HR.Views;

public partial class HrDashboardPage : ContentPage
{
    private readonly HrDashboardViewModel _viewModel;
    private bool _sudahDimuat;

    public HrDashboardPage(HrDashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_sudahDimuat)
            return;

        _sudahDimuat = true;
        await _viewModel.MuatAsync();
    }
}
