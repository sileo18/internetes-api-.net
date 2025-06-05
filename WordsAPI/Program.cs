// Program.cs

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SendGrid;
using StackExchange.Redis; // Para IConnectionMultiplexer
using WordsAPI.CacheService; // Seu serviço de cache
using WordsAPI.Config;
using WordsAPI.Config.WordsAPI.Config; // Suas configurações de email
using WordsAPI.Domain;      // Suas entidades de domínio
using WordsAPI.Repositories; // Seus repositórios
using WordsAPI.Services;     // Seus serviços

var builder = WebApplication.CreateBuilder(args);

// --- Configuração do Redis (IConnectionMultiplexer e CacheService) ---
// Tenta ler do appsettings.json ou da variável de ambiente REDIS_URL
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
if (string.IsNullOrEmpty(redisConnection))
{
    // Se não encontrar em ConnectionStrings, tenta ler de uma variável de ambiente REDIS_URL
    // Este é o padrão do Fly.io para o Redis
    redisConnection = builder.Configuration.GetValue<string>("REDIS_URL");
}

if (string.IsNullOrEmpty(redisConnection))
{
    // Logar um aviso se o Redis não for configurado (se a aplicação puder rodar sem cache)
    // ou lançar uma exceção se o Redis for um requisito rígido.
    Console.WriteLine("Aviso: String de conexão Redis não encontrada. O cache Redis não será ativado.");
}
else
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var configuration = ConfigurationOptions.Parse(redisConnection);
        // Opcional: configurar logging para StackExchange.Redis
        // configuration.LoggerFactory = sp.GetRequiredService<ILoggerFactory>();
        return ConnectionMultiplexer.Connect(configuration);
    });

    builder.Services.AddScoped<ICacheService, CacheService>(); // Seu serviço de cache
}


// --- Configuração do Banco de Dados PostgreSQL ---
var psqlConnectionString = builder.Configuration.GetConnectionString("PsqlConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(psqlConnectionString));
           


// --- Registro de Serviços e Repositórios ---
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IWordService, WordService>();

var sendGridApiKey = builder.Configuration.GetValue<string>("EmailSettings:SendGridApiKey");
if (string.IsNullOrEmpty(sendGridApiKey))
{
    Console.WriteLine("Aviso: SendGrid API Key não encontrada nas configurações. O serviço de email pode não funcionar.");
    // Em produção, você pode querer que isso lance uma exceção e impeça a inicialização.
    // throw new InvalidOperationException("SendGrid API Key é obrigatória para o serviço de email.");
}
else
{
    // Registra o cliente SendGrid para injeção de dependência
    builder.Services.AddSingleton<ISendGridClient>(new SendGridClient(sendGridApiKey));
}

// --- Configuração do Serviço de Email ---
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>(); // Removida duplicação


// --- Configuração do Swagger/OpenAPI ---
builder.Services.AddEndpointsApiExplorer(); // Já estava lá, mas vamos manter no lugar correto
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WordsAPI", Version = "v1", Description = "Uma API para gerenciar palavras e suas definições." });
    
    // Configurar o Swagger para suportar JWT (se você for usar autenticação)
    // c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    // {
    //     Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
    //     Name = "Authorization",
    //     In = ParameterLocation.Header,
    //     Type = SecuritySchemeType.ApiKey,
    //     Scheme = "Bearer"
    // });
    // c.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     {
    //         new OpenApiSecurityScheme
    //         {
    //             Reference = new OpenApiReference
    //             {
    //                 Type = ReferenceType.SecurityScheme,
    //                 Id = "Bearer"
    //             }
    //         },
    //         new string[] {}
    //     }
    // });

    // Incluir comentários XML para documentação de API (se você gerá-los)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) // Verifique se o arquivo existe
    {
        c.IncludeXmlComments(xmlPath);
    }
});


// --- Configuração dos Controllers e Serialização JSON ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignora propriedades nulas na serialização JSON
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        // Opcional: Configurar para lidar com ciclos de objeto se você retornar entidades diretamente
        // options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });


// --- Configuração do Pipeline de Requisições HTTP (Middlewares) ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Middleware de página de exceção detalhada em dev
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WordsAPI V1"); // Nome aqui para Swagger UI
        c.RoutePrefix = string.Empty; // Para acessar Swagger na raiz (ex: http://localhost:5000/)
    });
}
else
{
    app.UseExceptionHandler("/error"); // Middleware de tratamento de exceções global para produção
    app.UseHsts(); // Adiciona cabeçalho HTTP Strict Transport Security
}

app.UseHttpsRedirection(); // Redireciona requisições HTTP para HTTPS

app.UseRouting(); // Middleware de roteamento (essencial para MapControllers)

// --- Autenticação e Autorização ---
// Se você está implementando autenticação/autorização, eles vêm aqui
// app.UseAuthentication();
// app.UseAuthorization();


app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); // Configuração de CORS (cuidado com AllowAny* em produção)

app.MapControllers(); // Mapeia os endpoints dos controladores

app.Run();