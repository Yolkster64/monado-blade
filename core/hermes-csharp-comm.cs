// Hermes C# Cross-Language Communication
using System;
using System.Net.Sockets;
using System.Text;
class Program {
    static void Main() {
        var client = new TcpClient("127.0.0.1", 9000);
        var data = Encoding.UTF8.GetBytes("Hello from C# Hermes!");
        var stream = client.GetStream();
        stream.Write(data, 0, data.Length);
        var buffer = new byte[1024];
        int bytes = stream.Read(buffer, 0, buffer.Length);
        Console.WriteLine($"Received: {Encoding.UTF8.GetString(buffer, 0, bytes)}");
        client.Close();
    }
}
