using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class Server
    {
        private const int Port = 8888;
        private const string StopWord = "/stop";

        private static readonly List<Task> Tasks = new List<Task>();

        private static bool IsPalindrome(string str)
        {
            var n = str.Length;
            for (var i = 0; i < n / 2; i++)
                if (str[i] != str[n - i - 1])
                    return false;
            return true;
        }

        private static void Main(string[] args)
        {
            Console.Title = "Сервер";

            var tokenSource = new CancellationTokenSource();

            TcpListener server = null;
            try
            {
                server = new TcpListener(IPAddress.Any, Port);
                server.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключения...");

                var task = Listen(server, tokenSource.Token);
                Console.ReadKey();

                try
                {
                    tokenSource.Cancel();
                    server.Stop();
                    task.Wait(tokenSource.Token);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task Listen(TcpListener server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
                try
                {
                    var client = await server.AcceptTcpClientAsync();
                    Console.WriteLine("Подключен клиент.");
                    Tasks.Add(Process(client, token));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            await Task.WhenAll(Tasks);
        }

        private static async Task Process(TcpClient client, CancellationToken token)
        {
            var data = new byte[256];
            var b = new StringBuilder();
            using (var stream = client.GetStream())
            {
                do
                {
                    try
                    {
                        var bytes = await stream.ReadAsync(data, 0, data.Length, token);
                        if (bytes > 0)
                        {
                            b.Append(Encoding.UTF8.GetString(data, 0, bytes));
                            var index = b.IndexOf(Environment.NewLine, 0, true);
                            if (index > 0)
                            {
                                var word = b.ToString(0, index);
                                if (word == StopWord)
                                {
                                    client.Close();
                                    break;
                                }

                                b.Remove(0, index + Environment.NewLine.Length);
                                await Task.Delay(1000);
                                Console.WriteLine("Принято от клиента: {0}", word);
                                data = Encoding.UTF8.GetBytes(IsPalindrome(word.Trim()).ToString());
                                stream.Write(data, 0, data.Length);
                            }
                        }
                        else
                        {
                            await Task.Delay(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                } while (client.Connected && !token.IsCancellationRequested);
            }

            Console.WriteLine("Клиент отключен");
        }
    }
}