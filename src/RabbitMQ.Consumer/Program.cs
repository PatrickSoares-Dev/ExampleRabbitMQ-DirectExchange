using RabbitMQ.Client;
using RabbitMQ.Model;
using System.Text.Json;

const string exchangeName = "pedido.exchange";
const string queueName = "pedido.criados";
const string routingKey = "pedido.criado";

var factory = new RabbitMQ.Client.ConnectionFactory()
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
    VirtualHost = "/",
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(
    exchange: exchangeName, //NOME DA EXCHANGE, NO CASO "pedido.exchange"
    type: RabbitMQ.Client.ExchangeType.Direct, //AQUI USA DIRECT EXCHANGE -- EXISTEM OUTROS TIPOS: FANOUT / TOPIC / HEADERS
    durable: true, //RESISTE A REINICIOS DO RABBITMQ, ESSENCIAL EM PROD
    autoDelete: false //A EXCHANGE NÃO É DELETADA APÓS NÃO TER UTILIZAÇÃO
    );


await channel.QueueDeclareAsync(
    queue: queueName,
    durable: true,
    exclusive: false, //OUTRAS CONEXÕES PODEM UTILIZAR ESSA FILA
    autoDelete: false, //A FILA NÃO SOME QUANDO NÃO ESTIVER SENDO UTILIZADA
    arguments: null //CONFIGURAÇÕES DA MENSAGEM, TAMANHO MÁXIMO E ETC
    );

//AQUI FAZ O LINK DA EXCHANGE COM A QUEUE, SEM ELA A MENSAGEM NUNCA CHEGA
await channel.QueueBindAsync(
    queue: queueName,
    exchange: exchangeName,
    routingKey: routingKey,
    arguments: null
    );


//CONSUMER

// SEM ISSO AQUI, O RABBITMQ PODE ENVIAR VARIAS MENSAGENS AO MESMO TEMPO PARA VARIOS QUEUE, DEIXANDO SOBRECARREGADO
await channel.BasicQosAsync(
    prefetchSize: 0,
    prefetchCount: 1, //PEGA APENAS 1 MENSAGEM POR VEZ, 
    global: false
    );

var consumer = new RabbitMQ.Client.Events.AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (model, ea) =>
{

    try
    {
        var body = ea.Body.ToArray();
        var json = System.Text.Encoding.UTF8.GetString(body);
        var pedido = System.Text.Json.JsonSerializer.Deserialize<Pedido>(json);

        Console.WriteLine($"[x] Pedido recebido: {pedido}");
        //SIMULA PROCESSAMENTO
        await Task.Delay(2000);
        Console.WriteLine($"[x] Pedido processado: {pedido}");

        Console.WriteLine("===================================");
        Console.WriteLine("");
        Console.WriteLine($"[CONSUMER] | Pedido recebido Id: {pedido?.Id}");
        Console.WriteLine($"[CONSUMER] | Cliente: {pedido?.ClienteEmail}");
        Console.WriteLine($"[CONSUMER] | Total: {pedido?.ValorTotal:C}");
        Console.WriteLine($"[CONSUMER] | Criado: {pedido?.DataCriacao:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("===================================");

        await Task.Delay(2000); //SIMULA PROCESSAMENTO DO PEDIDO

        //CONFIRMA QUE A MENSAGEM FOI PROCESSADA
        await channel.BasicAckAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false
            );

    } catch(JsonException ex)
    {
        Console.WriteLine($"[ERRO] Falha ao desserializar o pedido: {ex.Message}");
        await channel.BasicNackAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false,
            requeue: false //DESCARTA A MENSAGEM
            );
        throw;
    } catch (Exception ex)
    {
        Console.WriteLine($"[ERRO] Falha ao processar o pedido: {ex.Message}");
        await channel.BasicNackAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false,
            requeue: true //RECOLOCA A MENSAGEM NA FILA PARA TENTAR NOVAMENTE
            );
        throw;
    }

};

    await channel.BasicConsumeAsync(
        
        queue: queueName,
        autoAck: false, //CONFIRMAÇÃO MANUAL DAS MENSAGENS
        consumer: consumer
    );  

    Console.WriteLine(" [*] Aguardando por pedidos. Para sair pressione CTRL+C");  
    Console.ReadLine();