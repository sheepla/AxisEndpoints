using AxisEndpoints.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// AddAuthentication / AddAuthorization are intentionally omitted so the example
// runs without any credentials. Endpoints that would normally be protected are
// marked AllowAnonymous for local testing purposes — see each endpoint's Configure()
// for the authorization settings you would use in a real application.
builder.Services.AddAxisEndpoints();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // OpenAPI document available at /scalar/v1
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapAxisEndpoints();

app.Run();
