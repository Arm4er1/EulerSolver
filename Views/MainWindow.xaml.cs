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

        private void SetStatus(string text)
        {
            var vm = (MainViewModel)DataContext;
            vm.StatusText = text;
        }
    }
}