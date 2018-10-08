using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qiniu.Storage;

namespace QiNiuClient
{
    class QiniuArea
    {
        public string Name { get; set; }
        public Zone ZoneValue { get; set; }


        public static IEnumerable<QiniuArea>  GetList()
        {
            List<QiniuArea> list = new List<QiniuArea>
            {
                new QiniuArea() {Name = "华东", ZoneValue = Zone.ZONE_CN_East},
                new QiniuArea() {Name = "华北", ZoneValue = Zone.ZONE_CN_North},
                new QiniuArea() {Name = "华南", ZoneValue = Zone.ZONE_CN_South},
                new QiniuArea() {Name = "北美", ZoneValue = Zone.ZONE_US_North}
             // new QiniuArea() {Name = "东南亚", ZoneValue = Zone.ZONE_AS_Singapore}
            };
            return list;
        }
    }
}
