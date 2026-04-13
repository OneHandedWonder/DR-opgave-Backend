using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();      // enable controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    options.AddPolicy("RestrictedPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
              .WithMethods("POST", "PUT", "DELETE")
              .AllowAnyHeader();
    });
});
/*
builder.Services.AddDbContext<>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DR-DB")));
builder.Services.AddScoped<>();
builder.Services.AddSingleton<>();*/

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors();

app.MapControllers();                   // map controller routes

app.Run();