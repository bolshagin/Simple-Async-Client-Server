using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Client
    {
        private const int Port = 8888;
        private const string Address = "127.0.0.1";

        private const string StopWord = "/stop";

        private static readonly string[] Separators = {",", ".", "!", "?", ";", ":", " "};

        private static readonly List<string> Words = new List<string>();

        private static void GetWordsFromAllFiles(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.txt", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        var lines = File.ReadLines(file, Encoding.UTF8);
                        foreach (var line in lines)
                        {
                            var words = line.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var word in words) Words.Add(word);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\nОтсутствуют *.txt файлы в данном каталоге");
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void Answer(string data)
        {
            var answer = Convert.ToBoolean(data);
            Console.WriteLine(answer ? "Ответ: палиндром" : "Ответ: не палиндром");
        }

        private static void SendData(string word, NetworkStream stream)
        {
            var data = Encoding.UTF8.GetBytes(word + Environment.NewLine);
            stream.Write(data, 0, data.Length);
        }

        private static string ReadResponseData(NetworkStream stream)
        {
            var data = new byte[5];
            var bytes = stream.Read(data, 0, data.Length);
            var responseData = Encoding.UTF8.GetString(data, 0, bytes);
            return responseData;
        }

        private static bool CheckArguments(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Неверное число аргументов");
                Console.WriteLine(
                    "Клиент принимает только один параметр - путь до каталога. \nНапример: Client.exe C:\\path\\to\\txt\\files");
                return true;
            }

            return false;
        }

        private static void Main(string[] args)
        {
            Console.Title = "Клиент";

            if (CheckArguments(args)) return;

            GetWordsFromAllFiles(args[0]);

            try
            {
                var client = new TcpClient(Address, Port);
                using (var stream = client.GetStream())
                {
                    foreach (var word in Words)
                        try
                        {
                            Console.WriteLine("\nОтправляю на сервер: {0}", word);
                            SendData(word, stream);

                            var responseData = ReadResponseData(stream);

                            Answer(responseData);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                    Console.WriteLine("\nEnter, чтобы закрыть клиент");
                    Console.ReadKey();

                    SendData(StopWord, stream);

                    stream.Close();
                    client.Close();
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}