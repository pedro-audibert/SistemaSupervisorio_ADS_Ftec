/*
================================================================================================
ARQUIVO: Program.cs
FUNÇÃO:  Configuração principal da aplicação ASP.NET Core 6+, incluindo:
         1. Serviços (DB, Identity, MVC, Razor Pages, SignalR, MQTT, Localização, Sessão)
         2. Pipeline de Middlewares
         3. Rotas MVC/Razor Pages
         4. Hubs SignalR
================================================================================================
*/

#region NAMESPACES
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mmdba.Data;
using mmdba.Hubs;
using mmdba.Models;
using System;
using System.Globalization;
#endregion

var builder = WebApplication.CreateBuilder(args);

// Adiciona a configuração de Data Protection para persistir as chaves em produção
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, ".dp_keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));


#region Configuração de Serviços

// --- 1. LOCALIZAÇÃO ---
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// --- 2. BANCO DE DADOS E IDENTITY ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<IdentityErrorDescriberPtBr>();

// Configura o "Options Pattern" para as configurações do SendGrid
builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration.GetSection("AuthMessageSenderOptions"));


builder.Services.AddTransient<IEmailSender, mmdba.Services.EmailSender>();

// --- Configuração do Telegram (Opções) ---
builder.Services.Configure<mmdba.Models.TelegramSettings>(builder.Configuration.GetSection("TelegramSettings"));

// --- Regista o Serviço Telegram (Singleton) ---
builder.Services.AddSingleton<mmdba.Services.ITelegramService, mmdba.Services.TelegramService>();

// --- Regista o Serviço OEE (Scoped) ---
builder.Services.AddScoped<mmdba.Services.IOeeService, mmdba.Services.OeeService>();

// --- 3. MVC E RAZOR PAGES COM LOCALIZAÇÃO ---
var mvcBuilder = builder.Services.AddControllersWithViews()
    .AddViewLocalization();

var razorBuilder = builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Runtime compilation em desenvolvimento
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
    razorBuilder.AddRazorRuntimeCompilation();
}

// --- 4. OUTROS SERVIÇOS ---
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});

// --- 5. MOSQUITTO (MQTT) ---
//builder.Services.AddHostedService<MqttSubscriberService>();

#endregion


// ===================================================================
// TESTE DE DIAGNÓSTICO PARA VERIFICAR A CHAVE DO SENDGRID
var tempConfig = builder.Configuration;
var sendGridKeyFromConfig = tempConfig["AuthMessageSenderOptions:SendGridKey"];
Console.WriteLine("===================================================================");
Console.WriteLine(">>> CHAVE SENDGRID LIDA DA CONFIGURAÇÃO: " + (string.IsNullOrEmpty(sendGridKeyFromConfig) ? "NULA OU VAZIA!" : "ENCONTRADA!"));
Console.WriteLine("===================================================================");
// FIM DO TESTE DE DIAGNÓSTICO
// ===================================================================

// --- 6. CONFIGURAÇÃO DA ROTATIVA (Relatórios PDF) ---
var wwwRootPath = builder.Environment.WebRootPath;
Rotativa.AspNetCore.RotativaConfiguration.Setup(wwwRootPath, "Rotativa");

var app = builder.Build();

// Aplica as migrations do Entity Framework na inicialização
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

#region Configuração do Pipeline (Middlewares)

// --- LOCALIZAÇÃO ---
var supportedCultures = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// --- TRATAMENTO DE ERROS ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// --- AUTENTICAÇÃO E AUTORIZAÇÃO ---
app.UseAuthentication();
app.UseAuthorization();

#endregion

#region Rotas MVC, Razor Pages e Hubs SignalR

// Razor Pages
app.MapRazorPages();

// Rotas MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Hubs SignalR
app.MapHub<AlarmesHub>("/alarmesHub");
app.MapHub<IOsHub>("/iosHub");
app.MapHub<AvisosHub>("/avisosHub");
app.MapHub<DadosHub>("/dadosHub");
app.MapHub<StatusHub>("/statusHub");
app.MapHub<VelocidadeHub>("/velocidadeHub");
app.MapHub<ContagemHub>("/contagemHub");

#endregion

// Bloco para semear o usuário Admin e a Role "Admin"
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        // Pega o serviço de configuração para ler o secrets.json
        var configuration = services.GetRequiredService<IConfiguration>();

        // Chama a função que definimos abaixo
        await SeedAdminAsync(userManager, roleManager, logger, configuration);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Um erro ocorreu ao semear o usuário admin.");
    }
}

async Task SeedAdminAsync(UserManager<ApplicationUser> userManager,
                        RoleManager<IdentityRole> roleManager,
                        ILogger<Program> logger,
                        IConfiguration configuration) // Recebe a configuração
{
    string adminRoleName = "Admin";

    // 1. LEIA AS CREDENCIAIS DA CONFIGURAÇÃO (User Secrets)
    string adminEmail = configuration["AdminSeed:Email"];
    string adminPassword = configuration["AdminSeed:Password"];

    // 2. Verificação de segurança
    if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
    {
        logger.LogWarning("Email ou Senha do 'AdminSeed' não configurados nos 'User Secrets'.");
        logger.LogWarning("Por favor, clique com o botão direito no projeto > Manage User Secrets e adicione 'AdminSeed:Email' e 'AdminSeed:Password'.");
        logger.LogWarning("Pulando a semeadura do usuário admin.");
        return; // Sai da função se os segredos não estiverem definidos
    }

    // 3. Criar a Role "Admin" se ela não existir
    logger.LogInformation("Verificando se a role 'Admin' existe...");
    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRoleName));
        logger.LogInformation("Role 'Admin' criada com sucesso.");
    }
    else
    {
        logger.LogInformation("Role 'Admin' já existe.");
    }

    // 4. Criar o Usuário Admin se ele não existir
    logger.LogInformation($"Verificando se o usuário '{adminEmail}' existe...");
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        logger.LogInformation("Usuário admin não encontrado. Criando...");
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true // Confirma o email automaticamente
        };

        // Usa a senha lida do "User Secrets"
        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            logger.LogInformation($"Usuário '{adminEmail}' criado com sucesso.");
            // 5. Adicionar o usuário à role "Admin"
            await userManager.AddToRoleAsync(adminUser, adminRoleName);
            logger.LogInformation($"Usuário '{adminEmail}' adicionado à role '{adminRoleName}'.");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                logger.LogError($"Erro ao criar usuário admin: {error.Description}");
            }
        }
    }
    else
    {
        logger.LogInformation($"Usuário '{adminEmail}' já existe.");
    }
}

app.Run();
