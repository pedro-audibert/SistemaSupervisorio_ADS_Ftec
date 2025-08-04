// MqttSubscriberService.cs
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class MqttSubscriberService : BackgroundService
{
    private readonly ILogger<MqttSubscriberService> _logger;

    public MqttSubscriberService(ILogger<MqttSubscriberService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateMqttClient();

        // Atribui a função que vai tratar as mensagens recebidas
        mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883) // Conecta ao broker local
            .WithClientId("dotnet-subscriber-" + Guid.NewGuid())
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Conecta apenas se não estiver conectado
                if (!mqttClient.IsConnected)
                {
                    await mqttClient.ConnectAsync(options, stoppingToken);
                    _logger.LogInformation("Cliente MQTT conectado ao broker 'localhost:1883'.");

                    // Se inscreve no tópico "wildcard" para receber tudo de "mqtt/rotuladora/"
                    var topicFilter = new MqttTopicFilterBuilder()
                        .WithTopic("mqtt/rotuladora/+")
                        .Build();

                    await mqttClient.SubscribeAsync(topicFilter, stoppingToken);
                    _logger.LogInformation("Inscrito no tópico wildcard 'mqtt/rotuladora/+'. Aguardando mensagens...");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Não foi possível conectar ou se inscrever no broker MQTT. Nova tentativa em 5 segundos.");
            }

            // Espera 5 segundos antes de verificar a conexão novamente
            await Task.Delay(5000, stoppingToken);
        }
    }

    private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        _logger.LogInformation("Mensagem genérica recebida no tópico [{Topic}] | Payload: {Payload}", topic, payload);

        // Usa um "switch" para direcionar a mensagem para a lógica correta baseada no tópico
        switch (topic)
        {
            case "mqtt/rotuladora/alarmes":
                // TODO: Coloque aqui sua lógica para tratar ALARMES
                ProcessarAlarmes(payload);
                break;

            case "mqtt/rotuladora/Avisos":
                // TODO: Coloque aqui sua lógica para tratar AVISOS
                ProcessarAvisos(payload);
                break;

            case "mqtt/rotuladora/Contagem":
                // TODO: Coloque aqui sua lógica para tratar CONTAGEM
                ProcessarContagem(payload);
                break;

            case "mqtt/rotuladora/Dados":
                // TODO: Coloque aqui sua lógica para tratar DADOS GERAIS
                ProcessarDados(payload);
                break;

            case "mqtt/rotuladora/IOs":
                // TODO: Coloque aqui sua lógica para tratar I/Os (Entradas/Saídas)
                ProcessarIOs(payload);
                break;

            case "mqtt/rotuladora/Status":
                // TODO: Coloque aqui sua lógica para tratar STATUS
                ProcessarStatus(payload);
                break;

            case "mqtt/rotuladora/Velocidade":
                // TODO: Coloque aqui sua lógica para tratar VELOCIDADE
                ProcessarVelocidade(payload);
                break;

            default:
                // Caso receba uma mensagem de um tópico inesperado (mas que corresponde ao wildcard)
                _logger.LogWarning("Recebida mensagem em um tópico não tratado: {Topic}", topic);
                break;
        }

        return Task.CompletedTask;
    }

    // --- MÉTODOS DE EXEMPLO PARA CADA TÓPICO ---
    // Substitua o conteúdo destes métodos pela sua lógica real (salvar no banco, chamar outra API, etc.)

    private void ProcessarAlarmes(string payload) => _logger.LogInformation(">>> LÓGICA DE ALARMES EXECUTADA: {Payload}", payload);
    private void ProcessarAvisos(string payload) => _logger.LogInformation(">>> LÓGICA DE AVISOS EXECUTADA: {Payload}", payload);
    private void ProcessarContagem(string payload) => _logger.LogInformation(">>> LÓGICA DE CONTAGEM EXECUTADA: {Payload}", payload);
    private void ProcessarDados(string payload) => _logger.LogInformation(">>> LÓGICA DE DADOS EXECUTADA: {Payload}", payload);
    private void ProcessarIOs(string payload) => _logger.LogInformation(">>> LÓGICA DE I/Os EXECUTADA: {Payload}", payload);
    private void ProcessarStatus(string payload) => _logger.LogInformation(">>> LÓGICA DE STATUS EXECUTADA: {Payload}", payload);
    private void ProcessarVelocidade(string payload) => _logger.LogInformation(">>> LÓGICA DE VELOCIDADE EXECUTADA: {Payload}", payload);
}