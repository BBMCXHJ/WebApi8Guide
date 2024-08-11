using System.Reflection;
using Web.Data.Attributes;

namespace WebApi8Guide.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddServicesFromAssembly<T>(this IServiceCollection services, Assembly assembly)
        {
            // 获取所有实现了T接口的类  
            var serviceTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(T)));

            foreach (var serviceType in serviceTypes)
            {
                // 遍历服务类型的所有接口  
                foreach (var interfaceType in serviceType.GetInterfaces())
                {
                    // 检查服务类是否有 SingletonServiceAttribute 特性  
                    bool isSingleton = serviceType.GetCustomAttributes(typeof(SingletonServiceAttribute), false).Any();

                    if (isSingleton)
                    {
                        // 注册为单例(Singleton)
                        services.AddSingleton(interfaceType, serviceType);
                    }
                    else
                    {
                        bool isTransient = serviceType.GetCustomAttributes(typeof(TransientServiceAttribute), false).Any();
                        if (isTransient)
                        {
                            // 注册为瞬时(Transient)
                            services.AddTransient(interfaceType, serviceType);
                        }
                        else 
                        {
                            // 注册为作用域(Scoped)  
                            services.AddScoped(interfaceType, serviceType);
                        }                    
                    }
                }
            }
        }
    }
}
