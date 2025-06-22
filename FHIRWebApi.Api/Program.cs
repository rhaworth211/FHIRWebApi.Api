using Hl7.Fhir.Model.CdsHooks;
using Hl7.Fhir.Rest;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDevClient", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddSingleton(new FhirClient("https://server.fire.ly", new FhirClientSettings
{
    PreferredFormat = ResourceFormat.Json,
    VerifyFhirVersion = false
}));
//builder.Services.AddSingleton(new FhirClient("https://hapi.fhir.org/baseR4", new FhirClientSettings
//{
//    PreferredFormat = ResourceFormat.Json,          
//    VerifyFhirVersion = true,
//    PreferredReturn = Prefer.ReturnRepresentation,  
//    Timeout = 30_000
//}));
builder.Services.AddControllers().AddNewtonsoftJson(); 
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FHIR Patient API V1");
    });
}

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering(); // allows multiple reads

    var body = string.Empty;
    if (context.Request.ContentLength > 0 &&
        (context.Request.Method == "POST" || context.Request.Method == "PUT"))
    {
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0; // rewind
    }

    Console.WriteLine($"-> {context.Request.Method} {context.Request.Path}");
    if (!string.IsNullOrEmpty(body))
        Console.WriteLine($" Body: {body}");

    await next();
});

app.UseHttpsRedirection();

app.UseCors("AllowAngularDevClient");

app.UseAuthorization();

app.MapControllers();

app.Run();
