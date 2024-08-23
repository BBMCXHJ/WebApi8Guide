## 配置接口支持返回XML
    默认情况下，ASP.NET Core WebAPI Controller 返回的是JSON数据，但也可以返回XML格式数据
    在服务中配置 AddControllers 服务，增加额外的 AddXmlSerializerFormatters 配置即可，这样既可以返回 JSON 格式，也可以返回 XML 格式
    ```C#
    builder.Services.AddControllers(option =>
    {
        option.ReturnHttpNotAcceptable = true;
    }).AddXmlSerializerFormatters();
    ```
    但是这个配置对于控制器中返回值是JsonResult方法是无效的，返回值始终都是JsonResult

