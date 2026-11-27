using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using StudentTracker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<InMemoryTrackerStore>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var app = builder.Build();

var frontendRoot = FindFrontendRoot(app.Environment.ContentRootPath);
if (frontendRoot is not null)
{
    var fileProvider = new PhysicalFileProvider(frontendRoot);

    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/back", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/.", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next();
    });

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider
    });
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapOpenApi("/swagger/{documentName}/swagger.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
    });
}

app.Run();

static string? FindFrontendRoot(string startPath)
{
    var dir = new DirectoryInfo(startPath);
    for (var i = 0; i < 6 && dir is not null; i++)
    {
        if (File.Exists(Path.Combine(dir.FullName, "index.html")))
        {
            return dir.FullName;
        }

        dir = dir.Parent;
    }

    return null;
}
