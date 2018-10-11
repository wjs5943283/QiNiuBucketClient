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

        public static bool CdnRefresh(Mac mac,string[] urls)
        {
            CdnManager cdnMgr = new CdnManager(mac);
            var result = cdnMgr.RefreshUrls(urls);
            return result.Code == 200;

        }

    }
}
