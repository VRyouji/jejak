using AbsensiSales.HR.ViewModels;

namespace AbsensiSales.HR.Views;

public partial class HrPerizinanPage : ContentPage
{
    private readonly HrPerizinanViewModel _viewModel;
    private bool _sudahDimuat;

    public HrPerizinanPage(HrPerizinanViewModel viewModel)
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
