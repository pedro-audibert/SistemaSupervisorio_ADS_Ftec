using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using System.Diagnostics; // Adicionado para depuração
using System; // Adicionado para Exception

namespace mmdba.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Mude a assinatura do método para async Task para podermos usar await e ver a resposta
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // 1. Busca a chave da API
                var apiKey = _configuration["SendGridKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    Debug.WriteLine("ERRO: A chave 'SendGridKey' não foi encontrada. Verifique seus User Secrets.");
                    return;
                }

                // 2. Busca o remetente
                var fromEmail = _configuration["SendGrid:FromEmail"];
                var fromName = _configuration["SendGrid:FromName"];

                // 3. Cria o cliente e a mensagem
                var client = new SendGridClient(apiKey);
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(fromEmail, fromName),
                    Subject = subject,
                    HtmlContent = htmlMessage,
                    PlainTextContent = "Please enable HTML viewing to see this email."
                };
                msg.AddTo(new EmailAddress(email));

                // 4. Envia o email e AGUARDA a resposta do SendGrid
                var response = await client.SendEmailAsync(msg);

                // 5. Verifica se o SendGrid aceitou o email e nos informa o resultado
                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Email para {email} foi aceito pelo SendGrid e está na fila de envio.");
                }
                else
                {
                    // Se o SendGrid retornou um erro, vamos ver qual foi.
                    var errorBody = await response.Body.ReadAsStringAsync();
                    Debug.WriteLine($"============= FALHA AO ENVIAR EMAIL =============");
                    Debug.WriteLine($"Status Code: {response.StatusCode}");
                    Debug.WriteLine($"Erro retornado pelo SendGrid: {errorBody}");
                    Debug.WriteLine($"===================================================");
                }
            }
            catch (Exception ex)
            {
                // Se ocorreu um erro antes mesmo de falar com o SendGrid (ex: rede)
                Debug.WriteLine($"Ocorreu uma exceção ao tentar enviar o email: {ex.Message}");
            }
        }
    }
}