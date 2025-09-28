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
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<IdentityErrorDescriberPtBr>();

///TESTEEEEE
// Configura o "Options Pattern" para as configurações do SendGrid
builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration.GetSection("AuthMessageSenderOptions"));


builder.Services.AddTransient<IEmailSender, mmdba.Services.EmailSender>();

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

app.Run();
