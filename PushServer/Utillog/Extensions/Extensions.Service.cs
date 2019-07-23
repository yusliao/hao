using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Util.Logs.Abstractions;
using Util.Logs.Core;
using Util.Logs.Formats;
using Autofac;
using Util.Dependency;

namespace Util.Logs.Extensions {
    /// <summary>
    /// 日志扩展
    /// </summary>
    public static partial class Extensions {
        /// <summary>
        /// 注册NLog日志操作
        /// </summary>
        /// <param name="services">服务集合</param>
        public static void AddNLog( this IServiceCollection services ) {
            services.TryAddScoped<ILogProviderFactory, Util.Logs.NLog.LogProviderFactory>();
            services.TryAddSingleton<ILogFormat, ContentFormat>();
            services.TryAddScoped<ILogContext, LogContext>();
            services.TryAddScoped<ILog, Log>();
        }
        /// <summary>
        /// 注册NLog日志操作
        /// </summary>
        /// <param name="services">服务集合</param>
        public static void AddNLog(this Autofac.ContainerBuilder  services)
        {
            services.AddScoped<ILogProviderFactory, Util.Logs.NLog.LogProviderFactory>();
            services.AddSingleton<ILogFormat, ContentFormat>();
            services.AddScoped<ILogContext, LogContext>();
            services.AddScoped<ILog, Log>();
        }

    }
}
