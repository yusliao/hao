using FluentScheduler;
using OMS.Models;
using PushServer.JobServer;
using PushServer.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Diagnostics;
using System.IO;
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
       
      
        public PushServer()
        {
            InitializeComponent();
            JobManager.Initialize(new PushJob());
            JobManager.JobException += info =>
              {
                  Util.Logs.Log.GetLog(nameof(PushServer)).Error("An error just happened with a scheduled job: " + info.Exception);
                  WxPushNews.SendErrorText($"错误类型：{Util.Helpers.Enum.GetDescription<ExceptionType>(ExceptionType.PushException)},信息：{info.Exception}");
              };
            JobManager.JobEnd += PushJob.OnJobEnd;
            
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
            JobManager.Stop();
        }
     
    }
}
