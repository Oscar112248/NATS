using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace UI_MODERNA
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer inactivityTimer;
        private bool isScreensaverActive = false;
        private bool allowUserInput = true;
        private TecladoVirtual tecladoActual; // ⬅️ guarda el teclado flotante


        public MainWindow()
        {
            InitializeComponent();
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowState = WindowState.Maximized;
            this.Topmost = true; // Hace que esté encima incluso de la barra de tareas


            SetupInactivityTimer();

            this.MouseMove += DetectActivity;
            this.KeyDown += DetectActivity;
            this.MouseDown += DetectActivity;
        }

        private void SetupInactivityTimer()
        {
            inactivityTimer = new DispatcherTimer();
            inactivityTimer.Interval = TimeSpan.FromSeconds(5); // Tiempo de inactividad antes de mostrar video
            inactivityTimer.Tick += (s, e) =>
            {
                inactivityTimer.Stop();
                ShowVideoScreensaver();
            };
            inactivityTimer.Start();
        }

        private void DetectActivity(object sender, EventArgs e)
        {
            if (isScreensaverActive && allowUserInput)
            {
                EndScreensaver();
            }
            else if (!isScreensaverActive)
            {
                // Reinicia el temporizador si aún no está activo el screensaver
                inactivityTimer.Stop();
                inactivityTimer.Start();
            }
        }

        private void ShowVideoScreensaver()
        {
            isScreensaverActive = true;
            allowUserInput = false;
            if (tecladoActual != null)
                tecladoActual.Visibility = Visibility.Collapsed;
            NormalContent.Visibility = Visibility.Collapsed;
            VideoScreensaver.Visibility = Visibility.Visible;
            VideoScreensaver.Opacity = 0;
            VideoScreensaver.Position = TimeSpan.Zero;
            VideoScreensaver.Play();

            var fadeIn = (Storyboard)this.Resources["FadeInVideo"];
            fadeIn.Begin();

            if (tecladoActual != null && tecladoActual.IsLoaded)
            {
                tecladoActual.Hide();
            }

            // Espera 1 segundo antes de permitir interacción
            DispatcherTimer delay = new DispatcherTimer();
            delay.Interval = TimeSpan.FromSeconds(1);
            delay.Tick += (s, e) =>
            {
                delay.Stop();
                allowUserInput = true;
            };
            delay.Start();
        }

        private void EndScreensaver()
        {
            allowUserInput = false;

            var fadeOut = (Storyboard)this.Resources["FadeOutVideo"];
            fadeOut.Completed += (s, e) =>
            {
                VideoScreensaver.Stop();
                VideoScreensaver.Visibility = Visibility.Collapsed;
                NormalContent.Visibility = Visibility.Visible;
                isScreensaverActive = false;

                // ✅ Mostrar nuevamente el teclado si estaba oculto
                if (tecladoActual != null && tecladoActual.IsLoaded)
                {
                    tecladoActual.Show();
                }

                inactivityTimer.Stop();
                inactivityTimer.Start();
            };
            fadeOut.Begin();
        }
        private void VideoScreensaver_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (isScreensaverActive)
            {
                VideoScreensaver.Position = TimeSpan.Zero;
                VideoScreensaver.Play();
            }
        }

        private void TxtNombres_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tecladoActual?.Close(); // Cierra anterior si existe

                tecladoActual = new TecladoVirtual(tb)
                {
                    Owner = this
                };

                var punto = tb.PointToScreen(new Point(0, tb.ActualHeight));
                tecladoActual.Left = punto.X;
                tecladoActual.Top = punto.Y;
                tecladoActual.Show();
            }
        }

        public void ResetInactivityTimer()
        {
            if (isScreensaverActive && allowUserInput)
            {
                EndScreensaver();
            }
            else if (!isScreensaverActive)
            {
                inactivityTimer.Stop();
                inactivityTimer.Start();
            }
        }


    }
}
