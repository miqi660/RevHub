using System.Windows;
using RevHub.Views;

namespace RevHub
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 以 LauncherWindow 作为应用入口
            var launcher = new LauncherWindow();
            launcher.Show();

            ShutdownMode = ShutdownMode.OnLastWindowClose;
        }
    }
}
