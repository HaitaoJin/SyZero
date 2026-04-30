using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using SyZero.Client;
using SyZero.Runtime.Security;
using SyZero.Serialization;
using SyZero.Web.Common;
using SyZero.Web.Common.Jwt;
using SyZero.Web.Common.Util;

namespace SyZero
{
    public static class SyZeroCommonExtension
    {

        /// <summary>
        /// 注册CommonModule
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddSyZeroCommon(this IServiceCollection services)
        {
            services.AddSingleton<IJsonSerialize, JsonSerialize>();
            services.AddSingleton<IXmlSerialize, XmlSerialize>();
            services.AddSingleton<ISyEncode, SyEncode>();
            services.AddSingleton<IToken, JwtToken>();
            services.AddSingleton<IPrizeUtil, PrizeUtil>();
            services.AddScoped<IAliasMethod, AliasMethod>();
            services.AddScoped<IClient, HttpRestClient>();

            services.AddSingleton<RestClient>(p =>
            {
                return new RestClient(new HttpClient());
            });

            return services;
        }
    }
}
