using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QiNiuClient
{
    /// <summary>
    /// TextWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TextWindow : Window
    {
        public TextWindow()
        {
            InitializeComponent();
        }

        public string TxtContent { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtContent))
            {
                txtBox.Text = TxtContent;
            }
        }
    }
}
