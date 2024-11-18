using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

// If you need custom settings, message callbacks, etc. use the following code to modify the elmah.io target:
//var elmahIoTarget = (Elmah.Io.NLog.ElmahIoTarget)LogManager.Configuration.FindTargetByName("elmahio");
//elmahIoTarget.OnMessage = (message) =>
//{
//    message.Version = "9.0";
//};

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddRazorPages();

    builder.Host.UseNLog();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapRazorPages()
       .WithStaticAssets();

    app.Run();
}
catch (Exception e)
{
    logger.Error(e, "Stopped program because of exception");
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}