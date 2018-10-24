using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;

public static class ImageExtensions
{
    /// <summary>
    /// 通过NET获取网络图片
    /// </summary>
    /// <param name="url">要访问的图片所在网址</param>
    /// <param name="requestAction">对于WebRequest需要进行的一些处理，比如代理、密码之类</param>
    /// <param name="responseFunc">如何从WebResponse中获取到图片</param>
    /// <returns></returns>
    public static Image GetImageFromNet(this string url, Action<WebRequest> requestAction = null, Func<WebResponse, Image> responseFunc = null)
    {
        return new Uri(url).GetImageFromNet(requestAction, responseFunc);
    }
    /// <summary>
    /// 通过NET获取网络图片
    /// </summary>
    /// <param name="url">要访问的图片所在网址</param>
    /// <param name="requestAction">对于WebRequest需要进行的一些处理，比如代理、密码之类</param>
    /// <param name="responseFunc">如何从WebResponse中获取到图片</param>
    /// <returns></returns>
    public static Image GetImageFromNet(this Uri url, Action<WebRequest> requestAction = null, Func<WebResponse, Image> responseFunc = null)
    {
        Image img;
        try
        {
            WebRequest request = WebRequest.Create(url);
            if (requestAction != null)
            {
                requestAction(request);
            }
            using (WebResponse response = request.GetResponse())
            {
                if (responseFunc != null)
                {
                    img = responseFunc(response);
                }
                else
                {
                    img = Image.FromStream(response.GetResponseStream());
                }
            }
        }
        catch
        {
            img = null;
        }
        return img;
    }
}

