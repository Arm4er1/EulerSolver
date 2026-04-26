using System.Windows;

namespace EulerSolver
{
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // Создаём MainWindow сразу как главное окно приложения
            // но НЕ показываем его — покажет SplashWindow
            var mainWindow = new Views.MainWindow();
            Application.Current.MainWindow = mainWindow;

            // Показываем сплеш
            var splash = new Views.SplashWindow();
            splash.Show();
        }
    }
}