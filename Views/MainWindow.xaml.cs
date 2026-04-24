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
            MessageBox.Show("Экспорт в Excel будет добавлен в следующем этапе.",
                "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}