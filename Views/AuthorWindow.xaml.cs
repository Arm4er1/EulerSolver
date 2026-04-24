using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EulerSolver.Views
{
    public partial class AuthorWindow : Window
    {
        private const string GitHubUrl = "https://github.com/Arm4er1";

        public AuthorWindow()
        {
            InitializeComponent();
            githubLink.Text = GitHubUrl.Replace("https://", "");
            TryLoadPhoto();
        }

        private void TryLoadPhoto()
        {
            try
            {
                string photoPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Assets", "author.jpg");

                if (File.Exists(photoPath))
                {
                    // Загружаем фото
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(photoPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // Устанавливаем фото в ImageBrush
                    authorPhoto.ImageSource = bitmap;

                    // Скрываем заглушку
                    photoPlaceholder.Visibility = Visibility.Collapsed;
                    photoInitials.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Фото нет — показываем инициалы
                    authorPhoto.ImageSource = null;
                    photoPlaceholder.Visibility = Visibility.Visible;
                    photoInitials.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                // Ошибка — показываем инициалы
                authorPhoto.ImageSource = null;
                photoPlaceholder.Visibility = Visibility.Visible;
                photoInitials.Visibility = Visibility.Visible;

            }
        }

        private void GitHub_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GitHubUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть ссылку:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}