var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCertificateForwarding(options => { });
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddHttpClient("ServiceB", client =>
{
    client.BaseAddress = new Uri("http://localhost:5088");
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/test", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("ServiceB");
    var response = await client.GetAsync("/callback");
    if (!response.IsSuccessStatusCode)
        return Results.BadRequest("Failed to call Service B");

    var content = await response.Content.ReadAsByteArrayAsync();
    var json = System.Text.Json.JsonSerializer.Deserialize<object>(content);
    return Results.Ok(json);
});

app.MapGet("/ping", (HttpContext httpContext) =>
{
    return Results.Ok(new { message = $"Pong from Service A" });
});

app.Run();
