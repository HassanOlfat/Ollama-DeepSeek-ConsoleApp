using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class simpleWithprompt
    {
        private static readonly HttpClient client = new HttpClient();
        public async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            //  client.Timeout = TimeSpan.FromSeconds(30);
            var url = "http://localhost:11434/api/chat";

            while (true)
            {
                Console.Write("> ");
                string userInput = Console.ReadLine() + "بدهی";
                if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("bye");
                    break;
                }

                // بررسی اگر کاربر درخواست بدهی خود را دارد
                if (userInput.Contains("بدهی") || userInput.Contains("چقدر بدهی دارم"))
                {
                    var prompt = "به کاربر توضیح دهید که محاسبه بدهی او معمولاً ۷۲ ساعت طول می‌کشد و او باید منتظر بماند. پاسخ باید دوستانه و حرفه‌ای باشد.";
                    var modelResponse = await GetModelResponse(url, prompt);
                    Console.WriteLine(modelResponse);
                }
                else
                {
                    // پردازش سایر درخواست‌ها
                    var prompt = $"User asked: {userInput}. Provide a helpful response.";
                    var modelResponse = await GetModelResponse(url, prompt);
                    Console.WriteLine(modelResponse);
                }
            }
        }

        public static async Task<string> GetModelResponse(string url, string prompt)
        {
            var payload = new
            {
                model = "deepseek-r1:1.5b",
                messages = new[] {
                new { role = "user", content = prompt }
            }
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var aggregatedContent = new StringBuilder();

            var jsonObjects = responseString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var jsonObject in jsonObjects)
            {
                try
                {
                    var responseObject = JsonConvert.DeserializeObject<ChatResponse>(jsonObject);
                    var messageContent = responseObject?.message?.content;
                    if (!string.IsNullOrEmpty(messageContent))
                    {
                        aggregatedContent.Append(messageContent);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing JSON object: {ex.Message}");
                }
            }

            aggregatedContent.Remove(0, aggregatedContent.ToString().IndexOf("</think>") + 8);
            return aggregatedContent.ToString().Trim();
        }

        public class ChatResponse
        {
            public Message message { get; set; }
        }

        public class Message
        {
            public string content { get; set; }
        }
    }
}
