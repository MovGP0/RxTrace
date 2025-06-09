using Microsoft.AspNetCore.Builder;

namespace RxTrace;

public static class AppBuilderExtensions
{
    public static IApplicationBuilder UseRxTrace(this IApplicationBuilder app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));
        app.UseMiddleware<RxTraceMiddleware>();
        return app;
    }
}