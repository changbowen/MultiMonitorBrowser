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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MultiMonitorBrowser
{
    /// <summary>
    /// Interaction logic for EncryptWindow.xaml
    /// </summary>
    public partial class EncryptWindow : Window
    {
        public EncryptWindow()
        {
            InitializeComponent();
        }

        private void Btn_Generate_Click(object sender, RoutedEventArgs e)
        {
            TB_Encrypt.Text = App.Encrypt(TB_Encrypt.Text);
            TB_Encrypt.Focus();
            TB_Encrypt.SelectAll();
            Clipboard.SetText(TB_Encrypt.Text);

            //animation
            T_Copied.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1)) {
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
            });
        }

        private void Btn_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EncryptWin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void EncryptWin_Loaded(object sender, RoutedEventArgs e)
        {
            TB_Encrypt.Focus();
            TB_Encrypt.SelectAll();
        }
    }
}
