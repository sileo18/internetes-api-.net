// Program.cs

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SendGrid;
// using StackExchange.Redis; // Este 'using' não é necessário para AddStackExchangeRedisCache
using WordsAPI.CacheService;
using WordsAPI.Config;
using WordsAPI.Config.WordsAPI.Config;
using WordsAPI.Domain;
using WordsAPI.Repositories;
using WordsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Registro de Serviços e Repositórios ---

var psqlConnectionString = builder.Configuration.GetConnectionString("PsqlConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(psqlConnectionString));

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection"); // Use o nome da chave do seu appsettings.json
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = redisConnectionString;
    
    options.InstanceName = "WordsAPI_"; 
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddSingleton<ISendGridClient>(sp => 
{
    var apiKey = sp.GetRequiredService<IConfiguration>()
        .GetValue<string>("EmailSettings:SendGridApiKey");
    return new SendGridClient(apiKey);
});


builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IWordService, WordService>();
builder.Services.AddTransient<IEmailService, EmailService>();





builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WordsAPI", Version = "v1", Description = "Uma API para gerenciar palavras e suas definições." });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WordsAPI V1");
        c.RoutePrefix = string.Empty;
    });
}
else
{
    app.UseExceptionHandler("/error"); 
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); 

app.MapControllers();

app.Run();