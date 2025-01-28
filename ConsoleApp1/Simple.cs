using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public  class Simple
    {
        public  async Task Main()
        {
            // Set console encoding to UTF-8
            Console.OutputEncoding = Encoding.UTF8;

            var client = new HttpClient();
            var url = "http://localhost:11434/api/chat"; // Replace with the correct endpoint
            client.DefaultRequestHeaders.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
            while (true)
            {
                Console.Write("> ");
                string userInput = Console.ReadLine();
                if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("bye");
                    break;
                }

                // Prepare the payload
                var payload = new
                {
                    model = "deepseek-r1:1.5b",
                    messages = new[] {
                    new { role = "user", content = userInput }
                }
                };

                // Serialize payload to JSON
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    // Send POST request
                    var response = await client.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();

                    // Read response as string
                    var responseString = await response.Content.ReadAsStringAsync();

                    // Aggregate content
                    var aggregatedContent = new StringBuilder();

                    // Process the newline-separated JSON response
                    var jsonObjects = responseString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var jsonObject in jsonObjects)
                    {
                        try
                        {
                            // Deserialize each JSON object
                            var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonObject);
                            var messageContent = responseObject?.message?.content?.ToString();
                            if (!string.IsNullOrEmpty(messageContent))
                            {
                                aggregatedContent.Append(messageContent); // Append the content
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing JSON object: {ex.Message}");
                        }
                    }
                    //s  string shapedText = Bidi.BidiFormatter.Format(aggregatedContent.ToString().Trim());

                    // Print the aggregated content as a single line
                    Console.WriteLine(aggregatedContent.ToString().Trim());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while sending the request: {ex.Message}");
                }
            }
        }

    }
}
