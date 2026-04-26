using System;
using System.Windows;
using System.Windows.Threading;

namespace EulerSolver.Views
{
    public partial class SplashWindow : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private int _secondsLeft = 60;

        public SplashWindow()
        {
            InitializeComponent();
            StartTimer();
        }

        private void StartTimer()
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _secondsLeft--;
            timerText.Text = $"Автозакрытие через {_secondsLeft} сек.";
            timerBar.Value = _secondsLeft;

            if (_secondsLeft <= 0)
            {
                _timer.Stop();
                OpenMainWindow();
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            OpenMainWindow();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Application.Current.Shutdown();
        }

        private void OpenMainWindow()
        {
            // MainWindow уже создан в App_Startup — просто показываем
            Application.Current.MainWindow.Show();
            Close();
        }
    }
}