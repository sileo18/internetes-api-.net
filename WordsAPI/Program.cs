// Program.cs
using Microsoft.EntityFrameworkCore;
using WordsAPI.Config;
using WordsAPI.Domain;
using WordsAPI.Repositories;
using WordsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// Adicionar servi�os ao cont�iner.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)); // Opcional: para mapear PascalCase para snake_case

// Registrar Reposit�rios e Servi�os para Inje��o de Depend�ncia
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IWordService, WordService>();
// Adicione outros aqui (IUserRepository, UserRepository, IUserService, UserService, etc.)

builder.Services.AddControllers()
    .AddJsonOptions(options => // Opcional: para configurar serializa��o JSON
    {
        // options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles; // Para evitar ciclos se n�o usar DTOs
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull; // N�o serializar nulos
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => // Configura��es do Swagger
{
    // options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Internetes API", Version = "v1" });
});


var app = builder.Build();

// Configurar o pipeline de requisi��es HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // c.SwaggerEndpoint("/swagger/v1/swagger.json", "Internetes API V1");
        // c.RoutePrefix = string.Empty; // Para acessar Swagger na raiz (/) em dev
    });
    // app.UseDeveloperExceptionPage(); // J� habilitado por padr�o em dev
}
else
{
    // Adicionar um middleware de tratamento de exce��es global para produ��o
    app.UseExceptionHandler("/error"); // Voc� precisaria criar um endpoint /error
    app.UseHsts();
}

app.UseHttpsRedirection();

// app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); // Configure CORS apropriadamente

app.UseAuthorization();

app.MapControllers();

app.Run();