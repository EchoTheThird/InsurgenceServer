﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdminSiteNew.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AdminSiteNew.Controllers
{
    [Authorize(Policy = "Moderator")]
    public class GTSController : Controller
    {
        public ActionResult Index()
        {
            var model = new GTSListModel
            {
                GTS = DatabaseSpace.DbGTS.GetOpenGTSTrades(),
                StartIndex = 0
            };
            return View(model);
        }
    }
}