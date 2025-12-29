using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UI_MODERNA
{
    /// <summary>
    /// Lógica de interacción para TecladoVirtual.xaml
    /// </summary>
    public partial class TecladoVirtual : Window
    {
        private readonly TextBox inputTarget;

        public TecladoVirtual(TextBox target)
        {
            InitializeComponent();
            inputTarget = target;
        }


        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NotificarActividad()
        {
            if (this.Owner is MainWindow main)
            {
                // Llama directamente al método público de MainWindow
                main.ResetInactivityTimer();
            }
        }


        private void Key_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && inputTarget != null)
            {
                inputTarget.Text += btn.Content.ToString();
                inputTarget.CaretIndex = inputTarget.Text.Length;
                ((MainWindow)Owner).ResetInactivityTimer();
            }
            NotificarActividad();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (inputTarget != null && inputTarget.Text.Length > 0)
            {
                inputTarget.Text = inputTarget.Text[..^1];
                inputTarget.CaretIndex = inputTarget.Text.Length;
                ((MainWindow)Owner).ResetInactivityTimer();
            }
            NotificarActividad();
        }


    }
}
