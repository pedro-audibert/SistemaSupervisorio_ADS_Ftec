using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options; // Importante: Adicionar este using
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

namespace mmdba.Services
{
    public class EmailSender : IEmailSender
    {
        // Iremos receber as opções de configuração já "empacotadas" aqui.
        private readonly AuthMessageSenderOptions _options;
        private readonly IConfiguration _configuration;

        // O construtor agora recebe IOptions<AuthMessageSenderOptions> injetado.
        public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor, IConfiguration configuration)
        {
            _options = optionsAccessor.Value;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // 1. Busca a chave da API a partir das opções injetadas. É mais seguro e fiável.
                var apiKey = _options.SendGridKey;

                if (string.IsNullOrEmpty(apiKey))
                {
                    Debug.WriteLine("ERRO: A chave 'SendGridKey' dentro de 'AuthMessageSenderOptions' não foi encontrada. Verifique os seus ficheiros appsettings.");
                    // Lança uma exceção para que o erro seja visível nos logs de produção.
                    throw new Exception("A chave 'SendGridKey' não está configurada.");
                }

                // 2. Busca o remetente (continua a funcionar da mesma forma).
                var fromEmail = _configuration["SendGrid:FromEmail"];
                var fromName = _configuration["SendGrid:FromName"];

                // 3. Cria o cliente e a mensagem (sem alterações).
                var client = new SendGridClient(apiKey);
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(fromEmail, fromName),
                    Subject = subject,
                    HtmlContent = htmlMessage,
                    PlainTextContent = "Please enable HTML viewing to see this email."
                };
                msg.AddTo(new EmailAddress(email));

                // 4. Envia o e-mail e aguarda a resposta (sem alterações).
                var response = await client.SendEmailAsync(msg);

                // 5. Verifica se o SendGrid aceitou o e-mail (sem alterações).
                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Email para {email} foi aceite pelo SendGrid e está na fila de envio.");
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    Debug.WriteLine($"============= FALHA AO ENVIAR EMAIL =============");
                    Debug.WriteLine($"Status Code: {response.StatusCode}");
                    Debug.WriteLine($"Erro retornado pelo SendGrid: {errorBody}");
                    Debug.WriteLine($"===================================================");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ocorreu uma exceção ao tentar enviar o e-mail: {ex.Message}");
            }
        }
    }
}