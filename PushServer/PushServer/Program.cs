using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using PushServer.Service;
using System.Globalization;

namespace PushServer
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            

           
            if (Environment.UserInteractive)
            {
               
                string exeArg = string.Empty;

                while (true)
                {
                    ShowOption();
                    exeArg = Console.ReadLine();
                    Console.WriteLine();

                    Run(exeArg, null);
                }
            }
            else
            {

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new PushServer()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }

        private static void ShowOption()
        {
            var commcolor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("欢迎进入推送服务，本服务的目的是将商铺的订单信息推送到OMS中!");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Please press a key to continue...");
            Console.WriteLine("-[start]: 开启服务;");
            Console.WriteLine("-[stop]: 停止服务;");
            Console.WriteLine("-[i]: 安装服务;");
            Console.WriteLine("-[u]: 卸载服务;");
            Console.WriteLine("-[p]: 推送报表;");
            Console.WriteLine("-[p1]: 推送盘点报表;后跟具体的月份值  exp: p1 6");
            Console.WriteLine("-[c]: 生成报表; 默认生成昨天的报表。 -a 整月.exp: c -a");
            Console.WriteLine("-[c2]: 生成回传订单; exp: c2 ");
            Console.WriteLine("-[c1]: 生成盘点报表; 后跟具体的月份值 exp: c1 6");
            Console.WriteLine("-[in]: 导入订单");
            //Console.WriteLine("-[out]: 导出订单;");
            Console.ForegroundColor = commcolor;
        }

        private static bool Run(string exeArg, string[] startArgs)
        {
            string[] str = exeArg.Trim().Replace('-',' ').Split(' ');
            switch (str[0].ToLower())
            {
                case ("i"):
                    SelfInstaller.InstallMe();
                    return true;

                case ("u"):
                    SelfInstaller.UninstallMe();
                    return true;

                case ("start"):
                    var result_start = SelfInstaller.StartService("PushService");
                    if (result_start)
                        Console.WriteLine("开启服务成功！");
                    else
                    {
                        Console.WriteLine("开启服务失败");
                    }
                    return true;

                case ("stop"):
                    var result_stop = SelfInstaller.StopService("PushService");
                    if (result_stop)
                        Console.WriteLine("停止服务成功！");
                    else
                    {
                        Console.WriteLine("停止服务失败");
                    }
                    return true;
                case ("p"):
                    var pushReportResult = PushReportHelper.PushReport();
                    if (pushReportResult)
                        Console.WriteLine("报表推送成功！");
                    else
                    {
                        Console.WriteLine("报表推送失败");
                    }
                    return true;
                case ("p1"):
                    int month = 6;
                    if (int.TryParse(str[1], out month))
                    {
                        
                        var pushpdReportResult = PushReportHelper.PushPandianReport(month);
                        if (pushpdReportResult)
                            Console.WriteLine("报表推送成功！");
                        else
                        {
                            Console.WriteLine("报表推送失败");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"输入的命令有误，请重新输入");
                    }
                    return true;
                case ("c"):
                    bool isAll = false;
                    for (int i = 1; i < str.Length; i++)
                    {
                        if (string.IsNullOrEmpty(str[i]))
                            continue;
                        else if (str[i]== "a")
                            isAll = true;

                    }
                    var createReportResult = PushReportHelper.CreateReport(isAll);
                    if (createReportResult)
                        Console.WriteLine("报表生成成功！");
                    else
                    {
                        Console.WriteLine("报表生成失败");
                    }
                    return true;
                case ("c1"):
                    int pdmonth = 6;
                    if (int.TryParse(str[1], out pdmonth))
                    {
                        var createPandianReportResult = PushReportHelper.CreatePandianReport(pdmonth);
                        if (createPandianReportResult)
                            Console.WriteLine("报表生成成功！");
                        else
                        {
                            Console.WriteLine("报表生成失败");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"输入的命令有误，请重新输入");
                    }
                   
                    return true;
                case ("c2"):
                    
                    var exportExcelResult = ExcelHelper.ExportExcel();
                    if (exportExcelResult)
                        Console.WriteLine("生成回传订单成功！");
                    else
                    {
                        Console.WriteLine("生成回传订单失败");
                    }
                    return true;
                case ("in"):
                    var importExcelResult = ExcelHelper.Dowork();
                    if (importExcelResult)
                        Console.WriteLine("导入订单成功！");
                    else
                    {
                        Console.WriteLine("导入订单失败");
                    }
                    return true;
                //case ("out"):
                //    var exportExcelResult = ExcelHelper.Dowork(false);
                //    if (exportExcelResult)
                //        Console.WriteLine("导出报表成功！");
                //    else
                //    {
                //        Console.WriteLine("导入报表失败");
                //    }
                //    return true;
                default:
                    Console.WriteLine("Invalid argument!");
                    return false;
            }
        }
    }
}
