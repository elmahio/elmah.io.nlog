﻿using System.Web;
using System.Web.Mvc;

namespace Elmah.Io.NLog.AspNetMvc
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
