// Program.cs
using Microsoft.EntityFrameworkCore; // Certifique-se de que esta importação está presente
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using WordsAPI.CacheService;
using WordsAPI.Config;
using WordsAPI.Domain;
using WordsAPI.Repositories;
using WordsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuração de Email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("RedisConnection"));

    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddScoped<ICacheService, CacheService>();

// Configuração do DbContext com QuerySplittingBehavior.SplitQuery
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
           

// Registro de Repositórios e Serviços
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IWordService, WordService>();

// Configuração do Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer(); // Necessário para Minimal APIs no Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo() { Title = "Nome da sua API", Version = "v1" });
    // Se você tiver comentários XML da documentação, adicione aqui:
    // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // c.IncludeXmlComments(xmlPath);
});

// Configuração de Controllers e Serialização JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Adicionado novamente, mas AddEndpointsApiExplorer já está acima. Pode ser removido se duplicado.
// builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configuração do Pipeline de Requisições HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Configurações para a UI do Swagger (opcional, mas comum)
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nome da sua API V1");
        c.RoutePrefix = string.Empty; // Para que a UI esteja na raiz (ex: http://localhost:5000/)
    });
}
else
{
    // Configurações para produção
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); // Configuração de CORS

app.UseAuthorization();

app.MapControllers(); // Mapeia os endpoints dos controladores

app.Run();