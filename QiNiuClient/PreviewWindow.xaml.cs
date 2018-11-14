using System;
using System.Collections.Generic;
using System.IO;
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
    /// PreviewWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PreviewWindow : Window
    {
        public PreviewWindow()
        {
            InitializeComponent();
        }

       public string PreviewFilePath { get; set; }

       // public ImageSource ImageSource { get; set; }


        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            ImageBox.Source=new BitmapImage(new Uri("no-prev.png",UriKind.Relative));
            if (!string.IsNullOrWhiteSpace(PreviewFilePath))
            {
                try
                {
               
                   ImageBox.Source = new BitmapImage(new Uri(PreviewFilePath));
                }
                catch (Exception)
                {
                    ImageBox.Source = new BitmapImage(new Uri("no-prev.png", UriKind.Relative));
                }
               

            }
        }
    }
}
