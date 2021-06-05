using System;
using Microsoft.Extensions.Caching.Memory;
using Autofac;

namespace Covid19TW
{
    public class IoC
    {
        private static IContainer container;
        public static void Register()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions() { }));
            builder.RegisterInstance(new SystemConfig { ItemsQueryLimit = 30 });                
            builder.RegisterType<DataManager>().As<IDataManager>().SingleInstance();
            container = builder.Build();
        }

        public static T Get<T>() where T : class
        {
            if (typeof(T).IsInterface == false)
            {
                throw new Exception("T must be interface");
            }
            return container.Resolve<T>();
        }

        public static T Get<T>(string name) where T : class
        {
            // if (typeof(T).IsInterface == false)
            // {
            //     throw new Exception("T must be interface");
            // }
            return container.ResolveNamed<T>(name);
        }

        public static SystemConfig GetConfig()
        {
            return container.Resolve<SystemConfig>();
        }
        public static MemoryCache GetCache()
        {
            return container.Resolve<MemoryCache>();
        }
    }
}