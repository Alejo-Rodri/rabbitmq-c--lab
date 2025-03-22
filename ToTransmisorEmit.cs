using RabbitMQ.Client;
using System.Text;

namespace rabbit_lab
{
    public class ToTransmisorEmit(ConnectionFactory factory)
    {
        private readonly ConnectionFactory _factory = factory;

        public async void Send(string message) {
            //var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: "transmisor", type: ExchangeType.Fanout);

            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "transmisor", routingKey: string.Empty, body: body);
            Console.WriteLine($" [x] Sent {message}");

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}