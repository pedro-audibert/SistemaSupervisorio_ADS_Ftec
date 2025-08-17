

using Microsoft.AspNetCore.Builder;
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

var builder = WebApplication.CreateBuilder(args);

#region Configuração de Serviços

// --- 1. CONFIGURAÇÃO DE LOCALIZAÇÃO (IDIOMA) ---
// Adiciona o serviço principal de localização.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// --- 2. CONFIGURAÇÃO DO BANCO DE DADOS E IDENTITY ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
//    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Registra o serviço de email personalizado.
builder.Services.AddTransient<IEmailSender, mmdba.Services.EmailSender>();

// --- 3. CONFIGURAÇÃO DE MVC, RAZOR PAGES E VALIDAÇÕES TRADUZIDAS ---
// Adiciona os serviços de Controllers e Views, já habilitando a tradução nas Views.
var mvcBuilder = builder.Services.AddControllersWithViews()
    .AddViewLocalization();

// Adiciona os serviços de Razor Pages, habilitando a tradução nas Views e nas mensagens de validação (Data Annotations).
var razorBuilder = builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Adiciona a compilação em tempo de execução APENAS se estiver em modo de Desenvolvimento.
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
    razorBuilder.AddRazorRuntimeCompilation();
}

// --- 4. OUTROS SERVIÇOS (SIGNALR, SESSÃO) ---
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});

// --- 5. MOSQUITTO (MQTT) ---
builder.Services.AddHostedService<MqttSubscriberService>();

#endregion

var app = builder.Build();

#region Configuração do Pipeline (Middlewares)

// --- CONFIGURAÇÃO DE LOCALIZAÇÃO (MIDDLEWARE) ---
// Define a cultura pt-BR como padrão e suportada pela aplicação.
// IMPORTANTE: Este bloco deve vir antes de outros middlewares que dependem de cultura, como o de Autenticação.
var supportedCultures = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// --- Sequência Crítica de Autenticação e Autorização ---
app.UseAuthentication();
app.UseAuthorization();

#endregion

#region Mapeamento de Rotas

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapeamento dos Hubs SignalR
app.MapHub<AlarmesHub>("/alarmesHub");
app.MapHub<IOsHub>("/iosHub");
app.MapHub<AvisosHub>("/avisosHub");
app.MapHub<DadosHub>("/dadosHub");
app.MapHub<StatusHub>("/statusHub");
app.MapHub<VelocidadeHub>("/velocidadeHub");
app.MapHub<ContagemHub>("/contagemHub");

#endregion

app.Run();