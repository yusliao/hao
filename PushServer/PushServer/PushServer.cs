using PushServer.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushServer
{
    partial class PushServer : ServiceBase
    {
        private static Timer timer = null;
        public PushServer()
        {
            InitializeComponent();
            timer = new Timer(TimerCallBack, null, 0, 3 * 1000 * 60 * 60);
        }
        

        protected override void OnStart(string[] args)
        {
            // TODO: 在此处添加代码以启动服务。
            Util.Logs.Log.GetLog(nameof(PushServer)).Info("windows服务启动中");
        }

        protected override void OnStop()
        {
            // TODO: 在此处添加代码以执行停止服务所需的关闭操作。
            Util.Logs.Log.GetLog(nameof(PushServer)).Info("windows服务关闭中");
        }
        /// <summary>
        /// 静态定时器回调方法，用于重置调度对象
        /// </summary>
        /// <param name="o"></param>
        private static void TimerCallBack(object o)
        {
            int timespan =   DateTime.Now.Hour-14;
            if (Math.Abs(timespan) > 2)
                return;
            else if (Math.Abs(timespan) == 2)
            {
                timer.Change(1 * 1000 * 60 * 60, 2 * 1000 * 60 * 10);
                AppServer.CreateReport(DateTime.Now.AddDays(-1));
            }
            else if (Math.Abs(timespan) == 1)
                timer.Change(1 * 1000 * 60 * 10, 1 * 1000 * 60 * 10);
            else
            {
                Util.Logs.Log.GetLog(nameof(PushServer)).Info("定时报表统计业务正在运行...");
                //TODO:
                //统计服务
               
                AppServer.PushReport();
                Util.Logs.Log.GetLog(nameof(PushServer)).Info("定时报表推送完毕");
                
                timer.Change(4 * 1000 * 60 * 60, 2 * 1000 * 60 * 60);
            }


        }
    }
}
