sing System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SendGrid;
using Npgsql; // Adicione esta importação para usar NpgsqlConnectionStringBuilder
using WordsAPI.CacheService;
using WordsAPI.Config;
using WordsAPI.Config.WordsAPI.Config; // Certifique-se que este namespace é válido
using WordsAPI.Domain;
using WordsAPI.Repositories;
using WordsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configuração da Conexão com o Banco de Dados PostgreSQL ---
var psqlConnectionString = builder.Configuration.GetConnectionString("PsqlConnection");

// Verifica se a DATABASE_URL existe e é uma URL no formato "postgres://"
if (!string.IsNullOrEmpty(psqlConnectionString) && psqlConnectionString.StartsWith("postgres://"))
{
    // Converte a URL do PostgreSQL para uma ConnectionString do Npgsql no formato chave-valor
    var uri = new Uri(psqlConnectionString);
    var userInfo = uri.UserInfo.Split(':');

    var connStringBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432, // Usa a porta da URI ou a padrão 5432
        Database = uri.Segments.Last().Trim('/'), // Pega o nome do banco de dados da URI
        Username = userInfo[0],
        Password = userInfo.Length > 1 ? userInfo[1] : null,
        // Adicione outras opções de SSL se necessário, como SslMode e TrustServerCertificate
        // SslMode.Prefer é um bom ponto de partida para a maioria dos deploys na Fly.io
        SslMode = SslMode.Prefer, 
        TrustServerCertificate = true 
    };
    // Sobrescreve a ConnectionString na configuração para que o DbContext use o formato correto
    psqlConnectionString = connStringBuilder.ToString();
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(psqlConnectionString));

// --- Registro de Outros Serviços e Repositórios ---

var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection"); 
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = redisConnectionString;
    options.InstanceName = "WordsAPI_"; 
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddSingleton<ISendGridClient>(sp => 
{
    var apiKey = sp.GetRequiredService<IConfiguration>()
        .GetValue<string>("SendGrid:ApiKey");
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

// Endpoint de Health Check
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
