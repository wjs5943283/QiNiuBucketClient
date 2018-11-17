using System;
using Newtonsoft.Json;

using Qiniu.Http;

using Qiniu.Util;
using System.Collections.Generic;
using System.Text;
using Qiniu.CDN;
using Qiniu.Storage;

namespace QiNiuClient
{
    public static class QiNiuHelper
    {
        public static string GetStorageType(int type)
        {
            switch (type)
            {
                case 0: return "标准存储";
                case 1: return "低频存储";
                default: return "未知类型";
            }
        }

        public static string GetFileSize(long Size)
        {
            string m_strSize = "";
            long FactSize = 0;
            FactSize = Size;
            if (FactSize < 1024.00)
                m_strSize = DoubleToString(FactSize)  + " B";
            else if (FactSize >= 1024.00 && FactSize < 1048576)
                m_strSize = DoubleToString(FactSize / 1024.00) + " KB";
            else if (FactSize >= 1048576 && FactSize < 1073741824)
                m_strSize = DoubleToString(FactSize / 1024.00 / 1024.00) + " MB";
            else if (FactSize >= 1073741824) m_strSize = DoubleToString(FactSize / 1024.00 / 1024.00 / 1024.00) + " GB";
            return m_strSize;

        }

        private static string DoubleToString(double data)
        {
            string s = data.ToString("F2");
            if (s.EndsWith(".00"))
            {
                s = s.Substring(0,s.Length - 3);
            }
            return s;
        }

        public static string GetDataTime(long unixTimeStamp)
        {
            // long unixTimeStamp = 1478162177;
            //unixTimeStamp         1360395673.4587420 
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
            DateTime dt = startTime.AddSeconds((double)unixTimeStamp/10000000);
            // return dt.ToString("yyyy/MM/dd HH:mm:ss:ffff");
            return dt.ToString("yyyy/MM/dd HH:mm:ss");
        }

        public static bool RefreshUrls(Mac mac,string[] urls)
        {
            CdnManager cdnMgr = new CdnManager(mac);
            var result = cdnMgr.RefreshUrls(urls);
            return result.Code == 200;
           
        }

        public static bool RefreshDirs(Mac mac, string[] urls)
        {
            CdnManager cdnMgr = new CdnManager(mac);
            var result = cdnMgr.RefreshDirs(urls);
            return result.Code == 200;
           
        }

        public static bool PrefetchUrls(Mac mac, string[] urls,out string Result)
        {
            StringBuilder sb = new StringBuilder();
            CdnManager cdnMgr = new CdnManager(mac);
            var result = cdnMgr.PrefetchUrls(urls);


            sb.AppendLine($"Code:{result.Result.Code}");
            if (result.Result.InvalidUrls != null)
            {
                sb.AppendLine($"InvalidUrls:");
                foreach (string url in result.Result.InvalidUrls)
                {
                    sb.Append(url + " ");
                }
            }
          
            if (result.Code == 200)
            {

                if (!string.IsNullOrWhiteSpace(result.Result.RequestId))
                {
                    sb.AppendLine($"RequestId:{result.Result.RequestId}");
                }
                sb.AppendLine($"每日的预取 url 限额quotaDay:{result.Result.QuotaDay}");
                sb.AppendLine($"剩余的预取 url 限额surplusDay:{result.Result.SurplusDay}");

                Result = sb.ToString();
                return true;
            }

            if (!string.IsNullOrWhiteSpace(result.Result.Error))
            {
                sb.AppendLine($"Error:{result.Result.Error}");
            }

            Result = sb.ToString();
                return false;
          

           // cdnMgr.RefreshUrlsAndDirs()


           
        }


        public static bool IsImage(string path)
        {
            System.Drawing.Image img = System.Drawing.Image.FromFile(path);
            return img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        /// <summary>
        /// 移动文件，如果bucket和newBucket相同则为重命名文件
        /// </summary>
        /// <param name="bucketManager"></param>
        /// <param name="bucket">原始空间</param>
        /// <param name="Key">原始文件名</param>
        /// <param name="newBucket">目标空间</param>
        /// <param name="newKey">目标文件名</param>
        /// <param name="force">是否设置强制覆盖</param>
        /// <returns></returns>
        public static ActionResult Move(BucketManager bucketManager,string bucket, string Key, string newBucket, string newKey, bool force = false)
        {
            HttpResult moveRet = bucketManager.Move(bucket, Key, newBucket, newKey, force);
            ActionResult ar = new ActionResult();
            if (moveRet.Code == (int) HttpCode.OK)
            {
                ar.IsSuccess = true;
                ar.Msg = moveRet.ToString();
            }
            else
            {
                ar.IsSuccess = false;
                ar.Msg = moveRet.ToString();
            }
            return ar;
        }

        public static string RemoveWindowsFileNameSpicalChar(string str,char repChar='-')
        {

            return str.Replace('\\',repChar).Replace('/', repChar).Replace(':', repChar).Replace('*', repChar).Replace('?', repChar).Replace('"', repChar).Replace('<', repChar).Replace('>', repChar).Replace('|', repChar);
        }



    }

    public class ActionResult
    {
        public bool IsSuccess { get; set; }
        public string Msg { get; set; }
    }
}
