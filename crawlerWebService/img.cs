using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace crawlerWebService {
        public class img {
            public string productId;
            public string dept;
            public string type;
            public string color;
            public string imageName;
            public string nav_from;
            public string url;
            public img(string Url, string Nav_from) {
                url = Url;
                nav_from = Nav_from;
            }
            public img() { }
            public img(string Url, string ProductId, string Dept, string Type, string ImageName, string Nav_From, string Color) {
                url = Url;
                productId = ProductId;
                dept = Dept;
                type = Type;
                imageName = ImageName;
                nav_from = Nav_From;
                color = Color;
            }
        }
}