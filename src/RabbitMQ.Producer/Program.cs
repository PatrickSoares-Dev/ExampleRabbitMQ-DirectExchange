using RabbitMQ.Client;
using RabbitMQ.Model;

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

int quantidadePedidos;
while (true)
{
    Console.WriteLine("Quantos pedidos você quer enviar? (informe um número inteiro positivo)");
    var input = Console.ReadLine();

    if (int.TryParse(input, out quantidadePedidos) && quantidadePedidos > 0)
    {
        break;
    }

    Console.WriteLine("Entrada inválida. Por favor insira um número inteiro positivo.");
}

Console.WriteLine($"Enviando {quantidadePedidos} pedidos...");

for (int i = 1; i <= quantidadePedidos; i++)
{
    var pedido = CriarPedidoFake(i);
    var mensagemJson = System.Text.Json.JsonSerializer.Serialize(pedido);
    var body = System.Text.Encoding.UTF8.GetBytes(mensagemJson);

    var properties = new BasicProperties
    {
        Persistent = true, //MENSAGEM GRAVADA EM DISCO CASO O RABBIT SEJA REINICIADO A MENSAGEM NÃO SE PERDE.
        ContentType = "application/json", 
        ContentEncoding = "utf-8"
    };

    //PUBLICA A MENSAGEM
    await channel.BasicPublishAsync(
        exchange: exchangeName,
        routingKey: routingKey,
        mandatory: false,
        basicProperties: properties,
        body: body
        );
    Console.WriteLine($"Pedido {pedido.Id} enviado.");

    Console.WriteLine("Aperte ENTER para sair...");
    Console.ReadLine();
}

static Pedido CriarPedidoFake (int index)
{
    return new Pedido
    {
        Id = Guid.NewGuid(),
        ClienteEmail = $"cliente{index}@email.com",
        ValorTotal = Random.Shared.Next(100, 5000),
        DataCriacao = DateTime.UtcNow,
        Itens = new List<Item>
        {
            new Item
            {
                NomeProduto = $"Produto {index}A",
                PrecoUnitario = Random.Shared.Next(10, 500),
                Quantidade = Random.Shared.Next(1, 5)
            },
        },
    };
}