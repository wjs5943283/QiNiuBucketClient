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

using Qiniu.Storage;

namespace QiNiuClient
{
    /// <summary>
    /// RenameWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RenameWindow : Window
    {
        public RenameWindow()
        {
            InitializeComponent();
        }

        public string FileName { get; set; }
        public BucketManager BucketManager { get; set; }

        public string Bucket;

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            if (BucketManager != null && !string.IsNullOrWhiteSpace(Bucket) && !string.IsNullOrWhiteSpace(FileName) &&
                !string.IsNullOrWhiteSpace(txtRename.Text.Trim()))
            {
                QiNiuHelper.Move(BucketManager, Bucket, FileName, Bucket, txtRename.Text.Trim());
            }
        this.Close();
          
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtRename.Text = FileName;
        }
    }
}
