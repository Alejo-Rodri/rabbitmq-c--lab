using rabbit_lab;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

const string QUEUE_NAME = "rpc_queue";

var factory = new ConnectionFactory
{
    HostName = "192.168.1.14", // Cambia esto por la IP del servidor RabbitMQ
    UserName = "guest",    // Usuario de RabbitMQ
    Password = "guest", // Contraseña de RabbitMQ
    VirtualHost = "/",          // Cambia esto si usas otro Virtual Host
    Port = 5672                 // Puerto por defecto de RabbitMQ
};

//var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: QUEUE_NAME, durable: false, exclusive: false,
    autoDelete: false, arguments: null);

await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (object sender, BasicDeliverEventArgs ea) =>
{
    AsyncEventingBasicConsumer cons = (AsyncEventingBasicConsumer)sender;
    IChannel ch = cons.Channel;
    string response = string.Empty;

    byte[] body = ea.Body.ToArray();
    IReadOnlyBasicProperties props = ea.BasicProperties;
    var replyProps = new BasicProperties
    {
        CorrelationId = props.CorrelationId
    };

    try
    {
        var message = Encoding.UTF8.GetString(body);
        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(message);

        if (jsonObject != null && jsonObject.ContainsKey("type"))
        {
            string type = jsonObject["type"].ToString();

            if (type == "offer")
            {
                var emit = new ToTransmisorEmit(factory);
                emit.Send(message); // Send the entire JSON message
                response = "Offer processed";
            }
            else if (type == "answer")
            {
                var toClient = new ToClientEmit(factory);
                toClient.send(message); // Send the entire JSON message
                response = "Answer processed";
            }
            else
            {
                Console.WriteLine(" [.] Tipo de mensaje desconocido.");
                response = "Error: Tipo de mensaje desconocido.";
            }
        }
        else
        {
            Console.WriteLine(" [.] Formato JSON incorrecto o falta 'type'.");
            response = "Error: Formato JSON incorrecto.";
        }
    }
    catch (JsonException ex)
    {
        Console.WriteLine($" [.] Error al deserializar JSON: {ex.Message}");
        response = "Error: JSON invalido";
    }
    catch (Exception ex)
    {
        Console.WriteLine($" [.] Error: {ex.Message}");
        response = "Error: procesando la solicitud";
    }
    finally
    {
        var responseBytes = Encoding.UTF8.GetBytes(response);
        await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
            mandatory: true, basicProperties: replyProps, body: responseBytes);
        await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
    }
};

await channel.BasicConsumeAsync(QUEUE_NAME, false, consumer);
Console.WriteLine(" [x] Awaiting RPC requests");
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();