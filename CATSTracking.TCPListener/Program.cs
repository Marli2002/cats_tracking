using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

const int Port = 10155;

await ExecuteAsync(new CancellationToken());

async Task ExecuteAsync(CancellationToken stoppingToken)
{
    Console.WriteLine("TCP Listener Starting");
    TcpListener listener = new TcpListener(IPAddress.Any, Port);
    listener.Start();
    Console.WriteLine("TCP Listener Started");

    while (!stoppingToken.IsCancellationRequested)
    {
        Console.WriteLine("TCP Listener Idle");
        var client = await listener.AcceptTcpClientAsync(stoppingToken);
        Console.WriteLine($"New client connected from {client.Client.RemoteEndPoint}");
        _ = Task.Run(() => HandleClient(client));
    }

    listener.Stop();
}

async Task HandleClient(TcpClient client)
{
    try
    {

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);

        while (client.Connected)
        {
            var line = await reader.ReadLineAsync();
            
            
            if (!string.IsNullOrEmpty(line))
            {
                var match = Regex.Match(line, @"\*.*?#");
                if(match.Success){
                    LogToPushover(match.Value);
                }else{
                    Console.WriteLine("Ignoring trash data");
                }

            }
        }

    }
    catch (Exception tcpEx)
    {
        Console.WriteLine($"Error during communication handling: {tcpEx.Message.ToString()}");

    }
    finally
    {
        Console.WriteLine($"Connection closed from {client.Client.RemoteEndPoint}");
        client.Close();
    }

}

async void LogToPushover(string message)
{

    try
    {

        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://devpanda.work/GPS"),
            Headers =
            {
                { "User-Agent", "insomnia/10.3.1" },
            },
            Content = new StringContent("{\n\"timestamp\": \"" + DateTime.UtcNow.ToString() + "\",\n\"rawData\": \"" + message + "\"\n}")
            {
                Headers =
                {
                ContentType = new MediaTypeHeaderValue("application/json")
                }
            }
        };

        Console.WriteLine($"Sending data: {message}");

        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response from server: {body}");
        }

    }
    catch (Exception ex)
    {

        Console.WriteLine($"Error sending data: {message}\nError: {ex.Message}");
    }

}
