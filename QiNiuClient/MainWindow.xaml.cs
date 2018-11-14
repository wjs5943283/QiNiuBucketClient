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
        private string startWith;
        private bool progressbarNeedStop;

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
                qiNiuClientCfg = new QiNiuClientCfg { DeleteAfterDays = 365 };
                TxtAK.Text = "";
                TxtSk.Text = "";
                txtDelAfDays.Text = "365";


            }
            if (!string.IsNullOrWhiteSpace(TxtAK.Text) && !string.IsNullOrWhiteSpace(TxtSk.Text))
            {
                ConnectServer();
            }


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
            ConnectServer();
        }

        private void ConnectServer()
        {
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
            if (!TxtAK.Text.Contains("*") && !TxtSk.Text.Contains("*"))
            {
                qiNiuClientCfg.Ak = TxtAK.Text.Trim();
                qiNiuClientCfg.Sk = TxtSk.Text.Trim();
            }
            mac = new Mac(qiNiuClientCfg.Ak, qiNiuClientCfg.Sk);
            config = new Config { Zone = Zone.ZONE_CN_East };
            Zone zone = (Zone)AreaComboBox.SelectedValue;
            if (zone != null)
            {
                config.Zone = zone;
            }
            bucketManager = new BucketManager(mac, config);



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
            progressbarNeedStop = false;
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
                    Thread.Sleep(200);
                    if (progressbarNeedStop)
                    {
                        return;

                    }
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
                    if (!string.IsNullOrWhiteSpace(TxtAK.Text.Trim()))
                    {
                        string ak = TxtAK.Text.Trim();
                        if (ak.Length >= 40 && !ak.Contains("*"))
                        {
                            TxtAK.Text = ak.Substring(0, 4) + "********************************" + ak.Substring(ak.Length - 5, 4);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(TxtSk.Text.Trim()))
                    {
                        string sk = TxtSk.Text.Trim();
                        if (sk.Length >= 40 && !sk.Contains("*"))
                        {
                            TxtSk.Text = sk.Substring(0, 4) + "********************************" + sk.Substring(sk.Length - 5, 4);
                        }
                    }
                    MessageBox.Show("连接成功！");
                }));
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {

                    BtnConnet.IsEnabled = true;
                    pb1.Visibility = Visibility.Hidden;

                    MessageBox.Show("连接失败！");
                }));
            }
            progressbarNeedStop = true;
        }

        private int num = 1;

        /// <summary>
        /// 查询按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            //根据空间名查询空间内容

            Search();


        }
        /// <summary>
        /// 查询
        /// </summary>
        private void Search()
        {
            if (btnSearch.IsEnabled == false)
            {
                return;
            }

            btnSearch.IsEnabled = false;



            if (string.IsNullOrWhiteSpace(marker))
            {
                num = 1;
                qiNiuFileInfoList = new List<QiNiuFileInfo>();

            }

            bucket = SyncTargetBucketsComboBox.Text;
            startWith = txtStartWith.Text.Trim();
            ThreadPool.QueueUserWorkItem((state) =>
            {
                ListResult listResult = bucketManager.ListFiles(bucket, startWith, marker, 5000, "");
                Dispatcher.Invoke(new Action(() =>
                {
                    if (listResult != null && listResult.Result != null && listResult.Result.Marker != null)
                    {
                        marker = listResult.Result.Marker;
                    }
                    else
                    {

                        marker = string.Empty;
                    }
                    if (listResult?.Result?.Items != null)
                    {

                        foreach (ListItem item in listResult.Result.Items)
                        {
                            // item.EndUser
                            QiNiuFileInfo f = new QiNiuFileInfo
                            {

                                FileName = item.Key,
                                FileType = item.MimeType,
                                StorageType = QiNiuHelper.GetStorageType(item.FileType),
                                FileSize = QiNiuHelper.GetFileSize(item.Fsize),
                                EndUser = item.EndUser,
                                CreateDate = QiNiuHelper.GetDataTime(item.PutTime)
                            };
                            qiNiuFileInfoList.Add(f);

                        }

                        if (qiNiuFileInfoList.Count > 0)
                        {
                            //dgResult.ItemsSource = !string.IsNullOrWhiteSpace(txtEndWith.Text)
                            //    ? qiNiuFileInfoList.Where(f => f.FileName.EndsWith(txtEndWith.Text.Trim()))
                            //    : qiNiuFileInfoList;
                            var list = qiNiuFileInfoList;


                            if (!string.IsNullOrWhiteSpace(txtEndWith.Text))
                            {
                                list = qiNiuFileInfoList.Where(f => f.FileName.EndsWith(txtEndWith.Text.Trim())).ToList();

                            }
                            if (list.Count > 0)
                            {
                                // dgResult.ItemsSource = list.OrderBy(t => t.CreateDate).ToList();
                                num = 1;
                                list = list.OrderByDescending(t => t.CreateDate).ToList();
                                foreach (var s in list)
                                {
                                    s.Num = num++;
                                }
                                dgResult.ItemsSource = list;
                            }
                            else
                            {
                                dgResult.ItemsSource = new List<QiNiuFileInfo>();
                            }
                            //  dgResult.ItemsSource = list;

                        }
                        else
                        {
                            dgResult.ItemsSource = new List<QiNiuFileInfo>();


                        }
                    }
                    else
                    {
                        MessageBox.Show("未能加载数据！");

                    }
                    btnSearch.IsEnabled = true;
                }));
            });











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
            if (SyncTargetBucketsComboBox.SelectedValue != null)
            {
                bucket = SyncTargetBucketsComboBox.SelectedValue.ToString();
                DomainsComboBox.Items.Clear();

                //多线程处理
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    domainsResult = bucketManager.Domains(bucket);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        if (domainsResult.Result.Count > 0)
                        {

                            foreach (string domain in domainsResult.Result)
                            {
                                DomainsComboBox.Items.Add(domain);
                            }
                            DomainsComboBox.SelectedValue = DomainsComboBox.Items[0];
                        }

                    }));
                });



                //domainsResult = bucketManager.Domains(bucket);
                //if (domainsResult.Result.Count > 0)
                //{

                //    foreach (string domain in domainsResult.Result)
                //    {
                //        DomainsComboBox.Items.Add(domain);
                //    }
                //    DomainsComboBox.SelectedValue = DomainsComboBox.Items[0];
                //}
            }

            qiNiuFileInfoList = new List<QiNiuFileInfo>();

        }

        /// <summary>
        /// 下载（含批量）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param> 
        private void btnBatchDownload_Click(object sender, RoutedEventArgs e)
        {
            DownLoad();
        }

        private void DownLoad()
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


            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo)item;
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
                    // string pubfile = DownloadManager.CreatePublishUrl(domain, info.FileName);

                    string pubfile = GetPublishUrl(info.FileName);
                    if (string.IsNullOrWhiteSpace(pubfile))
                    {
                        return;
                    }

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
                MessageBox.Show(string.IsNullOrWhiteSpace(rresult.ToString()) ? "下载结束！" : rresult.ToString());
                progressbarNeedStop = true;

            }
            else
            {
                MessageBox.Show("无法获得空间的域名");
            }

        }

        //批量删除
        private void btnBatchDel_Click(object sender, RoutedEventArgs e)
        {
            Delete();

        }

        private void Delete()
        {
            //1.获得表中选中的数据
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                string msg = string.Join(",\r\n", list.Select(q => q.FileName));
                MessageBoxResult confirmToDel = MessageBox.Show("确认要删除所选行吗？\r\n" + msg, "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirmToDel != MessageBoxResult.Yes)
                {
                    return;
                }


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


        private void MiDownload_Click(object sender, RoutedEventArgs e)
        {
            DownLoad();
        }

        //MiDelete_Click
        private void MiDelete_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void MISelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }
            dgResult.SelectAll();
            //  dgResult.Items.Refresh();

        }

        private void MICopyFileName_Click(object sender, RoutedEventArgs e)
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (QiNiuFileInfo qiNiuFileInfo in list)
                {
                    sb.AppendLine(qiNiuFileInfo.FileName);
                }
                if (!string.IsNullOrWhiteSpace(sb.ToString()))
                {
                    Clipboard.SetText(sb.ToString());
                }
            }

        }

        /// <summary>
        /// 复制下载地址 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MICopyNetAddress_Click(object sender, RoutedEventArgs e)
        {
            GetNetAddress();

        }

        private void MICopyNetAddressWithToken_Click(object sender, RoutedEventArgs e)
        {
            GetNetAddress(true);

        }

        //MIRefreshNetAddress_Click

        private void MIRefreshNetAddress_Click(object sender, RoutedEventArgs e)
        {
            RefreshNetAddress();

        }
        /// <summary>
        /// MIEditDeleteAfterDays_Click
        /// </summary>
        private void MIEditDeleteAfterDays_Click(object sender, RoutedEventArgs e)
        {
            EditDeleteAfterDays();

        }

        /// <summary>
        /// 修改DeleteAfterDays
        /// </summary>
        private void EditDeleteAfterDays()
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                string[] urls = new string[list.Count];
                qiNiuClientCfg.DeleteAfterDays = Convert.ToInt32(txtDelAfDays.Text.Trim());
                LoadProgressBar();
                System.Threading.ThreadPool.QueueUserWorkItem((state) =>
                {

                  
                    StringBuilder sb = new StringBuilder();
                    foreach (QiNiuFileInfo qiNiuFileInfo in list)
                    {
                        HttpResult expireRet = bucketManager.DeleteAfterDays(bucket, qiNiuFileInfo.FileName, qiNiuClientCfg.DeleteAfterDays.Value);
                        sb.AppendLine(expireRet.Code != (int)HttpCode.OK
                            ? $"{qiNiuFileInfo.FileName}:修改删除时间失败！"
                            : $"{qiNiuFileInfo.FileName}:修改删除时间成功！");
                    }

                    Dispatcher.Invoke(new Action(() =>
                    {
                        pb1.Visibility = Visibility.Hidden;
                        MessageBox.Show(sb.ToString());
                    }));
                });




              


            }
        }

        private void RefreshNetAddress()
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                string[] urls = new string[list.Count];
                for (var i = 0; i < list.Count; i++)
                {
                    QiNiuFileInfo qiNiuFileInfo = list[i];

                    urls[i] = GetPublishUrl(qiNiuFileInfo.FileName);
                    if (string.IsNullOrWhiteSpace(urls[i]))
                    {
                        return;
                    }
                }
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    bool result = QiNiuHelper.RefreshUrls(mac, urls);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show(result ? "文件刷新成功" : "文件刷新失败");
                    }));
                });



            }
        }





        private void GetNetAddress(bool isPrivate = false)
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (QiNiuFileInfo qiNiuFileInfo in list)
                {
                    //根据文件名获得下载地址
                    sb.AppendLine(isPrivate
                        ? GetPrivateUrl(qiNiuFileInfo.FileName)
                        : GetPublishUrl(qiNiuFileInfo.FileName));
                }

                if (!string.IsNullOrWhiteSpace(sb.ToString()))
                {
                    Clipboard.SetText(sb.ToString());
                }
            }
        }

        /// <summary>
        /// 根据文件名获得下载地址
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        private string GetPublishUrl(string fileName)
        {
            if (domainsResult.Result.Count > 0)
            {

                string domain = domainsResult.Result[0];

                if (domain.StartsWith(".") && !string.IsNullOrWhiteSpace(bucket))
                {
                    domain = bucket + domain;
                }
                string domainUrl = config.UseHttps ? "https://" + domain : "http://" + domain;

                return DownloadManager.CreatePublishUrl(domainUrl, fileName);
            }
            else
            {
                MessageBox.Show("无法获得空间的域名");
                return string.Empty;
            }


        }



        private string GetPrivateUrl(string fileName)
        {
            if (domainsResult?.Result?.Count > 0)
            {

                string domain = domainsResult.Result[0];
                if (domain.StartsWith(".") && !string.IsNullOrWhiteSpace(bucket))
                {
                    domain = bucket + domain;
                }

                domain = config.UseHttps ? "https://" + domain : "http://" + domain;
                return DownloadManager.CreatePrivateUrl(mac, domain, fileName, 3600);

            }
            else
            {
                MessageBox.Show("无法获得空间的域名");
                return string.Empty;
            }


        }



        //MiRefresh_Click
        private void MiRefresh_Click(object sender, RoutedEventArgs e)
        {
            Search();

        }
        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (cbDelete.IsChecked == true)
            {
                try
                {
                    qiNiuClientCfg.DeleteAfterDays = Convert.ToInt32(txtDelAfDays.Text.Trim());
                }
                catch (Exception)
                {
                    qiNiuClientCfg.DeleteAfterDays = null;
                }

            }
            else
            {
                qiNiuClientCfg.DeleteAfterDays = null;
            }



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


            string json = JsonConvert.SerializeObject(qiNiuClientCfg);
            File.WriteAllText("QiNiuClientCfg.Json", json);


            putPolicy = new PutPolicy();
            uploadResult = new StringBuilder();

            fileUploadFiles = ofd.FileNames;

            if (fileUploadFiles.Length <= 0) return;


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
                else if (result == false)
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
                    var fileInfo = new System.IO.FileInfo(file);

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

        private bool UploadFileOverlay(string file, bool overLay = false)
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

                putPolicy.DeleteAfterDays = qiNiuClientCfg.DeleteAfterDays;


                string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());

                ResumableUploader target = new ResumableUploader(config);
                PutExtra extra = new PutExtra { ResumeRecordFile = ResumeHelper.GetDefaultRecordKey(file, key) };
                //设置断点续传进度记录文件

                uploadResult.AppendLine("record file:" + extra.ResumeRecordFile);
                // extra.ResumeRecordFile = "test.progress";
                //todo:未实现上传进度
                HttpResult result = target.UploadStream(fs, key, token, extra);

                if (result.Code == 200)
                {
                    uploadResult.AppendLine("上传成功！ ");
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

        private bool UploadFile(string file, bool overlay = false)
        {
            if (uploadResult == null)
            {
                uploadResult = new StringBuilder();
            }
            if (putPolicy != null)
            {
                string key = Path.GetFileName(file);
                //上传限制10M
                putPolicy.FsizeLimit = 1024 * 1024 * 100;
                if (overlay)
                {
                    putPolicy.Scope = bucket + ":" + key;
                }
                else
                {
                    putPolicy.Scope = bucket;
                }
                putPolicy.SetExpires(3600);

                putPolicy.DeleteAfterDays = qiNiuClientCfg.DeleteAfterDays;


                string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());
                UploadManager um = new UploadManager(config);
                HttpResult result = um.UploadFile(file, key, token, null);

                if (result.Code == (int)HttpCode.OK)
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

        private void btnRefreshUrlsCdn_Click(object sender, RoutedEventArgs e)
        {
            string s = txtRefreshCdn.Text;
            if (string.IsNullOrWhiteSpace(s))
            {
                MessageBox.Show("请输入要刷新的地址");
                txtRefreshCdn.Focus();
                return;
            }
            string[] urls = s.Split(new char[] { ',', '，', ' ', '\t', '\r', '\n', ';', '；' },
                StringSplitOptions.RemoveEmptyEntries);
            if (urls.Length <= 0)
            {
                MessageBox.Show("请输入要刷新的地址");
                txtRefreshCdn.Focus();
                return;
            }
            MessageBox.Show(QiNiuHelper.RefreshUrls(new Mac(TxtAK.Text.Trim(), TxtSk.Text.Trim()), urls)
                ? "刷新成功！"
                : "刷新失败！");

        }

        private void btnPrefetchUrlsCdn_Click(object sender, RoutedEventArgs e)
        {
            string s = txtRefreshCdn.Text;
            if (string.IsNullOrWhiteSpace(s))
            {
                MessageBox.Show("请输入要预取的地址");
                txtRefreshCdn.Focus();
                return;
            }
            string[] urls = s.Split(new char[] { ',', '，', ' ', '\t', '\r', '\n', ';', '；' },
                StringSplitOptions.RemoveEmptyEntries);
            if (urls.Length <= 0)
            {
                MessageBox.Show("请输入要预取的地址");
                txtRefreshCdn.Focus();
                return;
            }
            string res;
            MessageBox.Show(QiNiuHelper.PrefetchUrls(new Mac(TxtAK.Text.Trim(), TxtSk.Text.Trim()), urls, out res)
                ? "文件预取成功！"
                : "文件预取失败！");
            if (!string.IsNullOrWhiteSpace(res))
            {
                txtResultCdn.Text = res;
            }


        }

        private void btnRefreshDirsCdn_Click(object sender, RoutedEventArgs e)
        {
            string s = txtRefreshCdn.Text;
            if (string.IsNullOrWhiteSpace(s))
            {
                MessageBox.Show("请输入要刷新的地址");
                txtRefreshCdn.Focus();
                return;
            }
            string[] urls = s.Split(new char[] { ',', '，', ' ', '\t', '\r', '\n', ';', '；' },
                StringSplitOptions.RemoveEmptyEntries);
            if (urls.Length <= 0)
            {
                MessageBox.Show("请输入要刷新的地址");
                txtRefreshCdn.Focus();
                return;
            }
            MessageBox.Show(QiNiuHelper.RefreshDirs(new Mac(TxtAK.Text.Trim(), TxtSk.Text.Trim()), urls)
                ? "刷新成功！"
                : "刷新失败！");
        }

        /// <summary>
        /// 预览（右击）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiPreview_Click(object sender, RoutedEventArgs e)
        {
            Preview();

        }
        /// <summary>
        /// 预览
        /// </summary>
        private void Preview()
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                string address = string.Empty;

                if (list[0].FileType.StartsWith("text") || list[0].FileType.StartsWith("application"))
                {
                    address = GetPrivateUrl(list[0].FileName);
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        return;
                    }
                    string tempfile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), list[0].FileName);

                    System.Threading.ThreadPool.QueueUserWorkItem((state) =>
                    {
                        System.Net.WebClient wc = new System.Net.WebClient();

                        wc.DownloadFile(address, tempfile);

                        Dispatcher.Invoke(new Action(() =>
                        {
                            System.Diagnostics.Process.Start(tempfile);
                        }));
                    });

                    return;
                }
                //if (list[0].FileType.StartsWith("text"))
                // {
                //     address = GetPrivateUrl(list[0].FileName);
                //     if (string.IsNullOrWhiteSpace(address))
                //     {
                //         return;
                //     }

                //     System.Threading.ThreadPool.QueueUserWorkItem((state) =>
                //     {
                //         System.Net.WebClient wc = new System.Net.WebClient();
                //         string s = wc.DownloadString(address);

                //         Dispatcher.Invoke(new Action(() =>
                //         {
                //             new TextWindow
                //             {
                //                 WindowStartupLocation = WindowStartupLocation.CenterOwner,
                //                 Owner = this,
                //                 TxtContent = s
                //             }.ShowDialog();
                //         }));
                //     });

                //     return;
                // }
                if (list[0].FileType.StartsWith("image"))
                {
                    address = GetPrivateUrl(list[0].FileName + "?imageView2/2/w/600/h/400/interlace/1/q/100");
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        return;
                    }

                }

                new PreviewWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    PreviewFilePath = address
                }.ShowDialog();




            }
        }



        private void cbDelete_Click(object sender, RoutedEventArgs e)
        {
            txtDelAfDays.IsEnabled = cbDelete.IsChecked == true;
        }

        private void btnGetNowVersion_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://raw.githubusercontent.com/wjs5943283/QiNiuBucketClient/master/QiNiuClient.zip");
        }

        private void btnOpenSource_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/wjs5943283/QiNiuBucketClient");

        }

        /// <summary>
        /// 重命名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiReName_Click(object sender, RoutedEventArgs e)
        {
            ReName();

        }

        private void ReName()
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<QiNiuFileInfo> list = new List<QiNiuFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                QiNiuFileInfo info = (QiNiuFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {

                RenameWindow rw = new RenameWindow()
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    FileName = list[0].FileName,
                    BucketManager = bucketManager,
                    Bucket = bucket
                };

                rw.ShowDialog();
                Search();
            }
        }


    }
}

