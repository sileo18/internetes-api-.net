using Microsoft.EntityFrameworkCore;
using Npgsql;
using SendGrid;
using StackExchange.Redis;
using System.Reflection;
using WordsAPI.CacheService;
using WordsAPI.Config;
using WordsAPI.Config.WordsAPI.Config;
using WordsAPI.Domain;
using WordsAPI.Repositories;
using WordsAPI.Services;

var builder = WebApplication.CreateBuilder(args);


var psqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(psqlConnectionString))
{
    // Lança um erro claro se a connection string não for encontrada.
    // Isso é melhor do que deixar o Npgsql falhar com uma mensagem genérica.
    throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada. Verifique se a variável de ambiente 'ConnectionStrings__DefaultConnection' está configurada corretamente.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(psqlConnectionString));

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = redisConnectionString;
    options.InstanceName = "WordsAPI_";
});


builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<ISendGridClient>(sp =>
{
    // Lê a chave da seção 'SendGrid:ApiKey', que mapeia para a variável `SendGrid__ApiKey`
    var apiKey = sp.GetRequiredService<IConfiguration>().GetValue<string>("SendGrid:ApiKey");
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException("A chave da API do SendGrid não foi configurada.");
    }
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
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "WordsAPI", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// --- Configuração de CORS (Política de Compartilhamento de Recursos) ---
var frontEndUrl = builder.Configuration.GetValue<string>("FRONTEND_URL");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (string.IsNullOrEmpty(frontEndUrl))
        {
            // Política mais restrita se a URL não for fornecida (ou para produção)
            // Ou permita qualquer origem para desenvolvimento se preferir
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            Console.WriteLine("WARN: FRONTEND_URL not set. Allowing any origin for CORS.");
        }
        else
        {
            // Política específica para sua aplicação frontend
            policy.WithOrigins(frontEndUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});


var app = builder.Build();

// Endpoint de Health Check que o Railway usa
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

// Configuração do pipeline de requisição HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Habilita o Swagger em qualquer ambiente, mas a UI só na rota /swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WordsAPI V1");
    // Para acessar o Swagger na raiz da URL (ex: https://sua-api.up.railway.app/)
    c.RoutePrefix = string.Empty; 
});


// app.UseHttpsRedirection(); // Comentado - O proxy reverso do Railway já lida com HTTPS. Habilitar isso pode causar loops de redirect.
app.UseRouting();

// Aplica a política de CORS definida acima
app.UseCors();

app.MapControllers();

app.Run();