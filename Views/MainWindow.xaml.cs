using EulerSolver.Services;
using EulerSolver.ViewModels;
using System.Windows;

namespace EulerSolver.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;

            if (!vm.HasResults || vm.LastResult == null)
            {
                MessageBox.Show("Сначала решите уравнение.",
                    "Нет данных", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SetStatus("Открываю Excel...");
                var service = new ExcelExportService();
                service.Export(vm.LastResult);
                SetStatus("✔ Excel открыт");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("✘ Ошибка экспорта в Excel");
            }
        }

        private void MenuExportWord_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;

            if (!vm.HasResults || vm.LastResult == null)
            {
                MessageBox.Show("Сначала решите уравнение.",
                    "Нет данных", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                SetStatus("Открываю Word...");
                var service = new WordExportService();
                service.Export(vm.LastResult);
                SetStatus("✔ Word открыт");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("✘ Ошибка экспорта в Word");
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Открывает окно "О программе"
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var window = new AboutWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        // Открывает окно "Об авторе"
        private void MenuAuthor_Click(object sender, RoutedEventArgs e)
        {
            var window = new AuthorWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp();
        }

        private void OpenHelp()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string helpPath = System.IO.Path.Combine(baseDir, "EulerSolverHelp.chm");

                if (!System.IO.File.Exists(helpPath))
                    helpPath = System.IO.Path.Combine(baseDir, "Help", "EulerSolverHelp.chm");

                if (!System.IO.File.Exists(helpPath))
                {
                    MessageBox.Show(
                        "Файл справки не найден:\n" + helpPath,
                        "Справка недоступна",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Копируем CHM во временную папку на диске C:
                // и снимаем блокировку через alternate data stream
                string tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), "EulerSolverHelp.chm");

                System.IO.File.Copy(helpPath, tempPath, overwrite: true);

                // Снимаем блокировку — удаляем Zone.Identifier stream
                UnblockFile(tempPath);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "hh.exe",
                        Arguments = "\"" + tempPath + "\"",
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось открыть справку:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Снимает блокировку с файла удаляя Zone.Identifier
        /// (alternate data stream который Windows добавляет к загруженным файлам)
        /// </summary>
        private void UnblockFile(string filePath)
        {
            try
            {
                // Путь к Zone.Identifier stream
                string zoneIdentifier = filePath + ":Zone.Identifier";

                // Удаляем через команду
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c echo.> \"{zoneIdentifier}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false
                };

                var proc = System.Diagnostics.Process.Start(psi);
                proc?.WaitForExit(2000);
            }
            catch
            {
                // Не критично — продолжаем даже если не удалось снять блокировку
            }
        }

        private void SetStatus(string text)
        {
            var vm = (MainViewModel)DataContext;
            vm.StatusText = text;
        }
    }
}