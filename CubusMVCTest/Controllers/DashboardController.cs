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
using StackExchange.Redis;
using Microsoft.AspNetCore.Hosting;

namespace CubusMVCTest.Controllers
{

    public class DashboardController : Controller
    {
        public DashboardController(IWebHostEnvironment env)
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

        
        public IActionResult Index(DashboardModel model)
        {
            string id = model.id;
            string URL = _env.WebRootPath;
            var fileContents = System.IO.File.ReadAllText(URL + "/version.txt");                        
            model.Version=fileContents;
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    Watcher watcher = new Watcher();
                    ZooKeeper zk = new ZooKeeper("192.168.200.181:2181,192.168.200.165:2182,192.168.200.165:2183", new TimeSpan(0, 0, 1, 50000), watcher);
                    watcher.waitforconnection();
                    if (zk.State == ZooKeeper.States.CONNECTED)
                    {
                        var stat = zk.Exists("/microservices/accountservice", true);
                        if (stat != null)
                        {
                            byte[] data = zk.GetData("/microservices/accountservice", true, null);
                            string strdata = System.Text.Encoding.UTF8.GetString(data);
                            var jsondoc = JsonDocument.Parse(strdata);
                            var root = jsondoc.RootElement;
                            JsonElement endpoints = root.GetProperty("accountservice");
                            string url = endpoints.GetProperty("url").GetString();
                            url = url + "?id=" + id;
                            using (var client = new HttpClient())
                            {
                                client.BaseAddress = new Uri(url);
                                var responseTask = client.GetAsync(url);
                                responseTask.Wait();
                                var result = responseTask.Result;
                                if (result.IsSuccessStatusCode)
                                {
                                    var readTask = result.Content.ReadAsStringAsync();
                                    readTask.Wait();
                                    var resData = readTask.Result;
                                    AccountsData AcctData = JsonSerializer.Deserialize<AccountsData>(resData, null);
                                    if (AcctData != null && AcctData.AccountResult != null && AcctData.code != "500")
                                    {
                                        DashboardModel dmodel = new DashboardModel { id = id, AccountsData = AcctData };
                                        dmodel.Version = model.Version;
                                        List<DashboardModel> dbModel = new List<DashboardModel>();
                                        dbModel.Add(dmodel);
                                        zk.Dispose();
                                        return View("Dashboard", dmodel);
                                    }
                                    else
                                    {
                                        return Redirect("~/Login/Index");
                                    }
                                }
                                else
                                {
                                    model.Error = "Unable to get account information";
                                }
                            }
                        }
                        else
                        {
                            model.Error = "Unable to get account information";
                        }
                    }
                    else
                    {
                        model.Error = "Unable to get account infomration";
                    }
                    zk.Dispose();
                }
                catch (ZooKeeperNet.KeeperException.ConnectionLossException ex)
                {
                    model.Error = "Unable to get account infomration";
                }
                catch (ZooKeeperNet.KeeperException.BadVersionException ex)
                {
                    model.Error = "Unable to get account infomration";
                }
            }
            else
            {
                return Redirect("~/Login/Index");
            }
            return View("Dashboard");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult LogOut(DashboardModel model)
        {
            string id = model.id;
            Watcher watcher = new Watcher();
            ZooKeeper zk = new ZooKeeper("192.168.200.181:2181,192.168.200.165:2182,192.168.200.165:2183", new TimeSpan(0, 0, 1, 50000), watcher);
            watcher.waitforconnection();
            try
            {
                if (zk.State == ZooKeeper.States.CONNECTED)
                {
                    var redisstat = zk.Exists("/databases/redisdb", true);
                    if (redisstat != null)
                    {
                        byte[] redisinfo = zk.GetData("/databases/redisdb", true, null);
                        string strredisinfo = System.Text.Encoding.UTF8.GetString(redisinfo);
                        var redisjsondoc = JsonDocument.Parse(strredisinfo);
                        var redisroot = redisjsondoc.RootElement;
                        JsonElement redisendpoints = redisroot.GetProperty("endpoints");
                        string redishosts = redisendpoints.GetProperty("host").GetString();
                        if (!string.IsNullOrEmpty(redishosts))
                        {
                            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redishosts);
                            IDatabase db = redis.GetDatabase();
                            db.KeyDelete(id, CommandFlags.None);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return Redirect("~/Login/Index");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
