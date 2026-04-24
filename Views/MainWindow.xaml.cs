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
                StatusText("Открываю Excel...");
                var service = new ExcelExportService();
                service.Export(vm.LastResult);
                StatusText("✔ Excel открыт");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText("✘ Ошибка экспорта в Excel");
            }
        }

        private void MenuExportWord_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Экспорт в Word будет добавлен в следующем этапе.",
                "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void StatusText(string text)
        {
            var vm = (MainViewModel)DataContext;
            vm.StatusText = text;
        }
    }
}