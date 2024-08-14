using System.Reflection;
using Web.Data.Attributes;

namespace WebApi8Guide.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddServicesFromAssemblies(this IServiceCollection services)
        {
            var assembly = Assembly.Load("Web.Data");
            var serviceInterfaces = assembly.GetTypes().Where(t => t.IsInterface);
            foreach (var serviceType in serviceInterfaces)
            {
                //var assembly = Assembly.GetAssembly(serviceType);
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
                var lifetimeAttribute = service.GetCustomAttribute<ServiceLifetimeAttribute>();
                // 没有用ServiceLifetime自定义属性标记的服务默认注入时是Scoped
                var lifetime = lifetimeAttribute?.Lifetime ?? ServiceLifetime.Scoped;

                switch (lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(serviceType, service);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(serviceType, service);
                        break;
                    default: // ServiceLifetime.Scoped  
                        services.AddScoped(serviceType, service);
                        break;
                }
            }
        }
    }
}
