var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();

app.MapGet("/", () => new { Title = "Student Tracker" });
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
