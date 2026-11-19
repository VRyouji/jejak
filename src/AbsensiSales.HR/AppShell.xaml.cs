namespace AbsensiSales.HR;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("workspace", typeof(Views.WorkspacePage));
        Routing.RegisterRoute("hr-dashboard", typeof(Views.HrDashboardPage));
        Routing.RegisterRoute("hr-perizinan", typeof(Views.HrPerizinanPage));
    }
}
