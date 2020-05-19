using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;

namespace OMS.PushServer
{
    public static class SelfInstaller
    {
        private static readonly string _exePath = Assembly.GetExecutingAssembly().Location;

        public static bool InstallMe()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new string[] { _exePath });
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool UninstallMe()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/u", _exePath });
            }
            catch
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="srv_name">服务名称</param>
        /// <param name="time_out">启动超时，默认30秒</param>
        /// <returns>成功：true，失败：false</returns>
        public static bool StartService(string srv_name, int time_out = 30000)
        {
            ServiceController service = new ServiceController(srv_name);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(time_out);
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <param name="srv_name">服务名称</param>
        /// <param name="time_out">停止超时，默认30秒</param>
        /// <returns>成功：true，失败：false</returns>
        public static bool StopService(string srv_name, int time_out = 30000)
        {
            ServiceController service = new ServiceController(srv_name);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(time_out);
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
        }

    }
}
