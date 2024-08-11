using System.Reflection;
using Web.Data.Attributes;

namespace WebApi8Guide.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddServicesFromAssemblies(this IServiceCollection services, List<Type> serviceTypes)
        {
            foreach (var serviceType in serviceTypes)
            {
                var assembly = Assembly.GetAssembly(serviceType);
                AddServicesFromAssembly(services, serviceType, assembly);
            }
        }
        public static void AddServicesFromAssembly(this IServiceCollection services, Type serviceType, Assembly assembly)
        {
            // 获取对应接口的实现类  
            var serviceClass = assembly.GetTypes()
                                     .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(serviceType));

            foreach (var service in serviceClass)
            {
                // 获取当前服务类实现的所有接口，并遍历它们 
                //foreach (var interfaceType in service.GetInterfaces())
                //{

                // 检查服务类是否有 SingletonServiceAttribute 特性  
                bool isSingleton = service.GetCustomAttributes(typeof(SingletonServiceAttribute), false).Any();

                if (isSingleton)
                {
                    // 注册为单例(Singleton)
                    services.AddSingleton(serviceType, service);
                }
                else
                {
                    bool isTransient = service.GetCustomAttributes(typeof(TransientServiceAttribute), false).Any();
                    if (isTransient)
                    {
                        // 注册为瞬时(Transient)
                        services.AddTransient(serviceType, service);
                    }
                    else
                    {
                        // 注册为作用域(Scoped)  
                        services.AddScoped(serviceType, service);
                    }
                }
                //}
            }
        }
    }
}
