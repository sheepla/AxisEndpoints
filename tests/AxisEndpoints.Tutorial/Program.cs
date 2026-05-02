using AxisEndpoints.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAxisEndpoints();

var app = builder.Build();

// Maps the OpenAPI endpoint at /openapi.json
app.MapOpenApi();

// Maps all endpoints defined in the application, including HelloEndpoint,
// at their respective routes (e.g., /hello for HelloEndpoint)
app.MapAxisEndpoints();

// Maps the Scalar API reference endpoint at /scalar
app.MapScalarApiReference();

app.Run();
