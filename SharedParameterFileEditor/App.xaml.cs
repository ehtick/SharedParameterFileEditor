using Serilog;
using Serilog.Sinks.GoogleAnalytics;
using SharedParameterFileEditor.Views;
using System.Globalization;
using System.Windows;

namespace SharedParameterFileEditor;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    void App_Startup(object sender, StartupEventArgs e)
    {
        //register the syncfusion license
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("##SyncfusionLicense##");

        var cultureInfo = Thread.CurrentThread.CurrentCulture;
        var regionInfo = new RegionInfo(cultureInfo.LCID);
        var clientId = ClientIdProvider.GetOrCreateClientId();

        var loggerConfig = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Debug();

#if RELEASE
        loggerConfig = loggerConfig
                .WriteTo.GoogleAnalytics(opts =>
                {
                    opts.MeasurementId = "##MEASUREMENTID##";
                    opts.ApiSecret = "##APISECRET##";
                    opts.ClientId = clientId;

                    opts.FlushPeriod = TimeSpan.FromSeconds(1);
                    opts.BatchSizeLimit = 1;
                    opts.MaxEventsPerRequest = 1;
                    //opts.IncludePredicate = e => e.Properties.ContainsKey("UsageTracking");

                    opts.GlobalParams["app_version"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();

                    opts.CountryId = regionInfo.TwoLetterISORegionName;
                });
#endif

        Log.Logger = loggerConfig.CreateLogger();

        MainView mainView = new();
        mainView.Show();
        return;
    }
}
