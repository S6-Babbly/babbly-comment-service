using babbly_comment_service.Data;
using babbly_comment_service.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<CassandraContext>();
builder.Services.AddScoped<ICassandraRepository<Comment>, CommentRepository>();
builder.Services.AddScoped<CommentRepository>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS
app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Register shutdown event to dispose of Cassandra context
app.Lifetime.ApplicationStopping.Register(() =>
{
    var scope = app.Services.CreateScope();
    var cassandraContext = scope.ServiceProvider.GetRequiredService<CassandraContext>();
    cassandraContext.Dispose();
});

app.Run();
