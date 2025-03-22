using RabbitMQ.Client;
using System.Text;

namespace rabbit_lab
{
    public class ToClientEmit(ConnectionFactory factory)
    {
        private readonly ConnectionFactory _factory = factory;

        public async void send(string message) {
            //var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: "receptor", type: ExchangeType.Fanout);

            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "receptor", routingKey: string.Empty, body: body);
            Console.WriteLine($" [x] Sent {message}");

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}