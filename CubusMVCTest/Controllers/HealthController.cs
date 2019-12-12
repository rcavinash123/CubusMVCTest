using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CubusMVCTest.Models;
using System.Net.Http;
using System;
using Microsoft.AspNetCore.Hosting;


namespace CubusMVCTest.Controllers
{

    public class HealthController : Controller
    {
        private readonly IWebHostEnvironment _env;
        public HealthController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public ActionResult<string> Index()
        {
            string URL = _env.WebRootPath;
            var fileContents = System.IO.File.ReadAllText(URL + "/version.txt");
            return fileContents;
            // string Status = "";     
            // using (var client = new HttpClient())
            // {
            //     string url = "http://192.168.200.182:5004/PostDeployment?componentName=MVCWeb";                               
            //     client.BaseAddress = new Uri(url);
            //     //HTTP GET
            //     var responseTask = client.GetAsync(url);
            //     responseTask.Wait();
            //     var result = responseTask.Result;
            //     Status = result.StatusCode.ToString();
            // }
            // return "Status : " + Status;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
