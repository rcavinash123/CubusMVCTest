using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CubusMVCTest.Models;
using System.Net.Http;
using ZooKeeperNet;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Web;
using Microsoft.AspNetCore.Hosting;

namespace CubusMVCTest.Controllers
{

    public class LoginController : Controller
    {
        public LoginController(IWebHostEnvironment env)
        {
            _env = env;
        }
        private readonly IWebHostEnvironment _env;
        class Watcher : IWatcher
        {
            ManualResetEventSlim connected = new ManualResetEventSlim(false);

            public void waitforconnection()
            {
                connected.Wait(new TimeSpan(0, 1, 0));
            }
            public void Process(WatchedEvent e)
            {
                if (e.Type == EventType.NodeDataChanged)
                {
                    Console.WriteLine("Path:{0},State:{0},Type:{0},Wrapper:{0}", e.Path, e.State, e.Type, e.Wrapper);
                    //Console.WriteLine(e.Path);
                }
                else if (e.State == KeeperState.SyncConnected)
                {
                    connected.Set();
                }
            }
        }
        

        public IActionResult Index(LoginModel model)
        {
            ModelState.Remove("Username");
            ModelState.Remove("Password");
            string URL = _env.WebRootPath;
            var fileContents = System.IO.File.ReadAllText(URL + "/version.txt");                        
            model.Version=fileContents;
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Submit(LoginModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string username = model.Username;
                    string password = model.Password;
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        try
                        {
                            Watcher watcher = new Watcher();
                            ZooKeeper zk = new ZooKeeper("192.168.200.181:2181,192.168.200.165:2182,192.168.200.165:2183", new TimeSpan(0, 0, 1, 50000), watcher);
                            watcher.waitforconnection();
                            if (zk.State == ZooKeeper.States.CONNECTED)
                            {
                                var stat = zk.Exists("/microservices/authservice", true);
                                if (stat != null)
                                {
                                    byte[] data = zk.GetData("/microservices/authservice", true, null);
                                    string strdata = System.Text.Encoding.UTF8.GetString(data);
                                    var jsondoc = JsonDocument.Parse(strdata);
                                    var root = jsondoc.RootElement;
                                    JsonElement endpoints = root.GetProperty("authservice");
                                    string url = endpoints.GetProperty("url").GetString();
                                    url = url + "?username=" + username + "&password=" + password;
                                    using (var client = new HttpClient())
                                    {
                                        client.BaseAddress = new Uri(url);
                                        //HTTP GET
                                        var responseTask = client.GetAsync(url);
                                        responseTask.Wait();
                                        var result = responseTask.Result;
                                        if (result.IsSuccessStatusCode)
                                        {
                                            var readTask = result.Content.ReadAsStringAsync();
                                            readTask.Wait();
                                            var resData = readTask.Result;
                                            var resdoc = JsonDocument.Parse(resData);
                                            var resroot = resdoc.RootElement;
                                            string id = resroot.GetProperty("id").GetString();
                                            string status = resroot.GetProperty("status").GetString();
                                            string code = resroot.GetProperty("code").GetString();
                                            string reason = resroot.GetProperty("reason").GetString();
                                            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(code) && code == "200")
                                            {
                                                DashboardModel dmodel = new DashboardModel { id = id };
                                                zk.Dispose();
                                                return RedirectToAction("Index", "Dashboard", dmodel);
                                            }
                                            else if (!string.IsNullOrEmpty(reason))
                                            {
                                                model.Error = reason;
                                            }
                                            else
                                            {
                                                model.Error = "Unable to validate username/password";
                                            }
                                        }
                                        else
                                        {
                                            model.Error = "Unable to validate username/password";
                                        }
                                    }
                                }
                                else
                                {
                                    model.Error = "Unable to validate username/password";
                                }
                            }
                            else
                            {
                                model.Error = "Unable to validate username/password";
                            }
                            zk.Dispose();
                        }
                        catch (ZooKeeperNet.KeeperException.ConnectionLossException ex)
                        {
                            model.Error = "Unable to validate username/password";
                        }
                        catch (ZooKeeperNet.KeeperException.BadVersionException ex)
                        {
                            model.Error = "Unable to validate username/password";
                        }

                    }
                    else
                    {
                        model.Error = "Invalid Username/Password";
                    }
                }
                else
                {
                   return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                model.Error = "Invalid Username/Password";
            }
            return View("Index", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
