using Newtonsoft.Json;
using Qiniu.Http;
using Qiniu.Storage;
using Qiniu.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;





namespace QiNiuClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; //居中
            InitializeComponent();
        }

        private Mac mac;
        private Config config;
        private BucketManager bucketManager;
        private string marker;
        private string fileSaveDir;
        private DomainsResult domainsResult;
        private string bucket;
        private QiNiuClientCfg qiNiuClientCfg;
        private string[] fileUploadFiles;
        private PutPolicy putPolicy;
        private StringBuilder uploadResult;
        private List<QiNiuFileInfo> qiNiuFileInfoList;

        private delegate void SetProgressBarHandle(int value);

        private void SetProgressBar(int val)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new SetProgressBarHandle(this.SetProgressBar), val);
            }
            else
            {
                pb1.Value = val;
            }
        }



        // private OpenDialogView openDialog ;
        /// <summary>
        /// 启动加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            pb1.Visibility = Visibility.Hidden;
            pb1.Maximum = 100;
            pb1.Value = 1;

            //加载区域列表
            AreaComboBox.ItemsSource = QiniuArea.GetList();
            AreaComboBox.DisplayMemberPath = "Name";
            AreaComboBox.SelectedValuePath = "ZoneValue";
            AreaComboBox.SelectedIndex = 0;

            //从配置文件中载入Ak和Sk（QiNiuClientCfg.Json）
            if (File.Exists("QiNiuClientCfg.Json"))
            {
                string json = File.ReadAllText("QiNiuClientCfg.Json");
                qiNiuClientCfg = JsonConvert.DeserializeObject<QiNiuClientCfg>(json);
                if (qiNiuClientCfg != null)
                {
                    TxtAK.Text = qiNiuClientCfg.Ak;
                    TxtSk.Text = qiNiuClientCfg.Sk;
                    if (qiNiuClientCfg.DeleteAfterDays.HasValue)
                    {
                        txtDelAfDays.Text = qiNiuClientCfg.DeleteAfterDays.Value.ToString();
                    }
                }
            }
            else
            {
                qiNiuClientCfg = new QiNiuClientCfg {DeleteAfterDays = 365};
                TxtAK.Text = "";
                TxtSk.Text = "";
                txtDelAfDays.Text = "365";


            }

            //#region 居中显示

            //double screeHeight = SystemParameters.FullPrimaryScreenHeight;
            //double screeWidth = SystemParameters.FullPrimaryScreenWidth;
            //Top = (screeHeight - this.Height) / 2;
            //Left = (screeWidth - this.Width) / 2;

            //#endregion

            marker = "";
          
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnConnet_Click(object sender, RoutedEventArgs e)
        {
            //Zone zone = (Zone) AreaComboBox.SelectedValue;
            //if (zone != null)
            //{
            //    MessageBox.Show(zone.ApiHost);
            //}
            //return;
            if (SyncTargetBucketsComboBox.Items.Count > 0)
            {
                SyncTargetBucketsComboBox.ItemsSource = null;
                SyncTargetBucketsComboBox.Items.Clear();
            }


            if (string.IsNullOrWhiteSpace(TxtAK.Text) || string.IsNullOrWhiteSpace(TxtSk.Text))
            {
                MessageBox.Show("Access Key 和Secret Key 不能为空！");
                return;
            }
            //根据AK和SK连接七牛云存储，1.获得存储空间列表；2.若成功本机保存Ak和SK
            Qiniu.Storage.Config.DefaultRsHost = "rs.qiniu.com";
            mac = new Mac(TxtAK.Text.Trim(), TxtSk.Text.Trim());
            config = new Config {Zone = Zone.ZONE_CN_East};
            Zone zone = (Zone) AreaComboBox.SelectedValue;
            if (zone != null)
            {
                config.Zone = zone;
            }
            bucketManager = new BucketManager(mac, config);
            qiNiuClientCfg.Ak = TxtAK.Text;
            qiNiuClientCfg.Sk = TxtSk.Text;
            
            this.SyncTargetBucketsComboBox.ItemsSource = null;
            BtnConnet.IsEnabled = false;
          
            // new Thread(this.reloadBuckets).Start();
            //使用线程池
            ThreadPool.QueueUserWorkItem((state) =>
            {
               
                reloadBuckets();
            });

            LoadProgressBar();

            Thread.Sleep(10);
        }
        
        /// <summary>
        /// 加载进度条
        /// </summary>
        private void LoadProgressBar()
        {
            pb1.Visibility = Visibility.Visible;

            ThreadPool.QueueUserWorkItem((state) =>
            {
                int i = 1;
                while (true)
                {
                    
                    i++;
                    if (i == 100)
                    {
                        i = 1;
                    }
                    SetProgressBar(i);
                    Thread.Sleep(100);
                }

            });

        }


        private void reloadBuckets()
        {
            BucketsResult bucketsResult = bucketManager.Buckets(true);
            if (bucketsResult.Code == 200)
            {
                //todo:保存ak&sk
              
                if (File.Exists("QiNiuClientCfg.Json"))
                {
                    File.Delete("QiNiuClientCfg.Json");
                    
                }
                
                 

                string json = JsonConvert.SerializeObject(qiNiuClientCfg);
                File.WriteAllText("QiNiuClientCfg.Json", json);


                List<string> buckets = bucketsResult.Result;

                Dispatcher.Invoke(new Action(() =>
                {
                    this.SyncTargetBucketsComboBox.ItemsSource = buckets;
                    BtnConnet.IsEnabled = true;
                    pb1.Visibility = Visibility.Hidden;
                    MessageBox.Show("连接成功！");
                }));
            }
        }

        private int num = 1;

        /// <summary>
        /// 查询按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private  void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            //根据空间名查询空间内容
         
            Search();
           

        }
        /// <summary>
        /// 查询
        /// </summary>
        private void Search()
        {
            if (string.IsNullOrWhiteSpace(marker))
            {
                num = 1;
                btnSearch.Content = "查询";
            }
            else
            {
                btnSearch.Content = "加载更多";
            }
            bucket = SyncTargetBucketsComboBox.Text;
            ListResult listResult = bucketManager.ListFiles(bucket, txtStartWith.Text, marker, 1000, "");
            // DomainsResult domainsResult = bucketManager.Domains(SyncTargetBucketsComboBox.Text);
            domainsResult = bucketManager.Domains(bucket);

            if (listResult.Result.Items.Count >= 1000)
            {
                btnSearch.Content = "加载更多";
            }
            else
            {
                num = 1;
                marker = "";
                btnSearch.Content = "查询";
            }

            if (qiNiuFileInfoList == null || num==1)
            {
                qiNiuFileInfoList = new List<QiNiuFileInfo>();
            }

          

            foreach (ListItem item in listResult.Result.Items)
            {
                // item.EndUser
                QiNiuFileInfo f = new QiNiuFileInfo
                {
                    Num = num,
                    FileName = item.Key,
                    FileType = item.MimeType,
                    StorageType = QiNiuHelper.GetStorageType(item.FileType),
                    FileSize = QiNiuHelper.GetFileSize(item.Fsize),
                    EndUser = item.EndUser,
                    CreateDate = QiNiuHelper.GetDataTime(item.PutTime)
                };
                qiNiuFileInfoList.Add(f);
                num++;
            }
            marker = listResult.Result.Marker;
           
          
            if (qiNiuFileInfoList.Count > 0)
            {
                dgResult.ItemsSource = !string.IsNullOrWhiteSpace(txtEndWith.Text)
                    ? qiNiuFileInfoList.Where(f => f.FileName.EndsWith(txtEndWith.Text.Trim()))
                    : qiNiuFileInfoList;
            }
            else
            {
                dgResult.ItemsSource = new List<QiNiuFileInfo>();
            }
        }




        private void SyncTargetBucketsComboBox_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            num = 1;
            marker = "";
            btnSearch.Content = "查询";
            btnSearch.IsEnabled = true;
            btnUpload.IsEnabled = true;
            btnBatchDel.IsEnabled = true;
            btnBatchDownload.IsEnabled = true;
            if(SyncTargetBucketsComboBox.SelectedValue!=null)
            bucket = SyncTargetBucketsComboBox.SelectedValue.ToString();
            qiNiuFileInfoList = new List<QiNiuFileInfo>();

        }

        /// <summary>
        /// 下载（含批量）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param> 
        private void btnBatchDownload_Click(object sender, RoutedEventArgs e)
        {
            //1.获得表中选中的数据
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }
            //2.选择下载保存的路径

            var sfd = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = @"选择保存位置",
                ShowNewFolderButton = true
            };


            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }


            fileSaveDir = sfd.SelectedPath;
            

          List <QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo) item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
          
            if (list.Count > 0)
            {
                //执行批量下载方法
                //使用线程池
                btnBatchDownload.IsEnabled = false;
               
                ThreadPool.QueueUserWorkItem(state =>
                {
                    batchDownLoad(list);
                });
                LoadProgressBar();
                Thread.Sleep(10);
            }
            pb1.Visibility = Visibility.Hidden;

            btnBatchDownload.IsEnabled = true;
        }

        /// <summary>
        /// 批量下载
        /// </summary>
        /// <param name="qiNiuFileInfolist"></param>
        private void batchDownLoad(IEnumerable<QiNiuFileInfo> qiNiuFileInfolist)
        {


            if (domainsResult.Result.Count > 0)
            {

                string domain = domainsResult.Result[0];
                //string key = "hello/world/七牛/test.png";
                //string privateUrl = DownloadManager.CreatePrivateUrl(mac, domain, key, 3600);

                domain = config.UseHttps ? "https://" + domain : "http://" + domain;

               var rresult = new StringBuilder();
                
                    foreach (QiNiuFileInfo info in qiNiuFileInfolist)
                    {
                        string pubfile = DownloadManager.CreatePublishUrl(domain, info.FileName);
                        string saveFile = Path.Combine(fileSaveDir, info.FileName.Replace('/', '-'));
                        if (File.Exists(saveFile))
                        {
                            saveFile = Path.Combine(fileSaveDir,
                                Path.GetFileNameWithoutExtension(info.FileName.Replace('/', '-')) + Guid.NewGuid() +
                                Path.GetExtension(info.FileName));
                        }
                        HttpResult result = DownloadManager.Download(pubfile, saveFile);
                        if (result.Code != 200)
                        {
                            result = DownloadManager.Download(
                                DownloadManager.CreatePrivateUrl(mac, domain, info.FileName, 3600), saveFile);
                            if (result.Code != 200)
                            {
                                rresult.AppendLine(info.FileName + ":下载失败！");
                                return;
                            }
                        }

                    }
                if (string.IsNullOrWhiteSpace(rresult.ToString()))
                {
                    MessageBox.Show("下载结束！");
                }
                else
                {
                    MessageBox.Show(rresult.ToString());
                }
                   
               
            }
        }

        private void btnBatchDel_Click(object sender, RoutedEventArgs e)
        {
            //1.获得表中选中的数据
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo) item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                //执行批量删除
                List<string> ops = new List<string>();
                foreach (var key in list)
                {
                    string op = bucketManager.DeleteOp(bucket, key.FileName);
                    ops.Add(op);
                }

                BatchResult ret = bucketManager.Batch(ops);

                StringBuilder sb = new StringBuilder();

                if (ret.Code / 100 != 2)
                {
                    MessageBox.Show("批量删除error: " + ret.ToString());
                    return;
                }
                MessageBox.Show("批量删除成功！");
                
                Search();
                Thread.Sleep(10);
            }

        }


        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private  void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog
            {
                Multiselect = true,
                Title = @"选择上传文件"
            };
            if (cbResume.IsChecked == true)
            {
                ofd.Multiselect = false;
            }
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            qiNiuClientCfg.DeleteAfterDays = Convert.ToInt32(txtDelAfDays.Text.Trim());

            string json = JsonConvert.SerializeObject(qiNiuClientCfg);
            File.WriteAllText("QiNiuClientCfg.Json", json);


            putPolicy =new PutPolicy();
            uploadResult = new StringBuilder();

             fileUploadFiles = ofd.FileNames;
             
            if(fileUploadFiles.Length<=0)return;


            bool? result;
            if (cbResume.IsChecked == true)
            {
                result = null;
                //续点上传
                btnUpload.Content = "正在上传......";
                btnUpload.IsEnabled = false;
                if (cbOverlay.IsChecked == true)
                {
                    LoadProgressBar();

                   
                    Dispatcher.Invoke(new Action(() =>
                    {
                        result = UploadFileOverlay(fileUploadFiles[0], true);
                        pb1.Visibility = Visibility.Hidden;
                       
                      
                    }));


                }
                else
                {

                    LoadProgressBar();
                    Dispatcher.Invoke(new Action(() =>
                    {
                        result = UploadFileOverlay(fileUploadFiles[0]);
                        pb1.Visibility = Visibility.Hidden;

                    }));




                }
                if (result == true)
                {
                    MessageBox.Show("上传成功！");
                }
                else if(result==false)
                {
                    MessageBox.Show(uploadResult.ToString());
                } 
            
                btnUpload.Content = "上传";
                btnUpload.IsEnabled = true;
                uploadResult = new StringBuilder();
                Search();
            }
            else
            {

                foreach (string file in fileUploadFiles)
                {
                    var fileInfo = new  System.IO.FileInfo(file);

                    if (fileInfo.Length > 1024 * 1024 * 100)
                    {
                        MessageBox.Show("单个文件大小不得大于100M");
                        return;
                    }
                }
                if (fileUploadFiles.Length > 100)
                {
                    MessageBox.Show("每次上传文件不得大于100个");
                    return;
                }


                btnUpload.Content = "正在上传......";
                btnUpload.IsEnabled = false;
                result = true;

                //普通上传
                if (cbOverlay.IsChecked == true)
                {
                    //覆盖上传
                    LoadProgressBar();
                    Dispatcher.Invoke(new Action(() =>
                    {

                   
                    foreach (string file in fileUploadFiles)
                        {
                            result = result & UploadFile(file, true);
                        }
                        pb1.Visibility = Visibility.Hidden;
                    }));

                   

                }
                else
                {
                    //不覆盖上传，文件若存在就跳过
                    LoadProgressBar();
                    Dispatcher.Invoke(new Action(() =>
                    {

                  
                    foreach (string file in fileUploadFiles)
                        {
                            result = result & UploadFile(file);
                        }
                        pb1.Visibility = Visibility.Hidden;
                    }));

                  
                  
                 
                }
               
                MessageBox.Show(result == false ? uploadResult.ToString() : "上传成功！");
               
                Search();
                uploadResult = new StringBuilder();
                btnUpload.Content = "上传";
                btnUpload.IsEnabled = true;
            }


        }

        private  bool UploadFileOverlay(string file,bool overLay=false)
        {
            //  string filePath = LocalFile;
            if (uploadResult == null)
            {
                uploadResult = new StringBuilder();
            }
            if (putPolicy != null)
            {
                string key = Path.GetFileName(file);
                System.IO.Stream fs = System.IO.File.OpenRead(file);
                if (overLay)
                {
                    putPolicy.Scope = bucket + ":" + key;
                }
                else
                {
                    putPolicy.Scope = bucket;
                }
                putPolicy.SetExpires(3600);
                putPolicy.DeleteAfterDays = qiNiuClientCfg.DeleteAfterDays ?? 365;
                string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());
             
                ResumableUploader target = new ResumableUploader(config);
                PutExtra extra = new PutExtra {ResumeRecordFile = ResumeHelper.GetDefaultRecordKey(file, key)};
                //设置断点续传进度记录文件
               
                uploadResult.AppendLine("record file:" + extra.ResumeRecordFile);
                // extra.ResumeRecordFile = "test.progress";
                //todo:未实现上传进度
               HttpResult result = target.UploadStream(fs, key, token, extra);
              
                if (result.Code == 200)
                {
                    uploadResult.AppendLine("上传成功！ " );
                    return true;
                }
                else
                {
                    string s = $"Code={result.Code},Text={result.Text}";
                    uploadResult.AppendLine("uploadResult:" + s);
                    return false;
                }
               
               
            }
               uploadResult.AppendLine("成员变量putPolicy为空！");
                return false;
            

           

        }

        private bool UploadFile(string file,bool overlay=false)
        {
            if (uploadResult == null)
            {
                uploadResult = new StringBuilder();
            }
            if (putPolicy != null)
            {
                string key = Path.GetFileName(file);
                //上传限制10M
                putPolicy.FsizeLimit = 1024 * 1024 * 10;
                if (overlay)
                {
                    putPolicy.Scope = bucket + ":" + key;
                }
                else
                {
                    putPolicy.Scope = bucket;
                }
                putPolicy.SetExpires(3600);
                putPolicy.DeleteAfterDays = qiNiuClientCfg.DeleteAfterDays ?? 365;
                string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());
                UploadManager um = new UploadManager(config);
                HttpResult result = um.UploadFile(file, key, token, null);

                if (result.Code == (int) HttpCode.OK)
                {
                    return true;
                }
                else
                {
                    
                    uploadResult.AppendLine(key + ":上传失败！");
                     return false;
                }
               
            }
            uploadResult.AppendLine("成员变量putPolicy为空！");
            return false;
        }

    }
}
  
