using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;

class Program
{
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        client.Timeout = TimeSpan.FromSeconds(30);
        var url = "http://localhost:11434/api/chat";
        string prompt;
        string modelResponse;
        while (true)
        {
            Console.Write("> ");
            string userInput = Console.ReadLine();
            if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("bye");
                break;
            }
            if (userInput.Contains("زمان") || userInput.Contains("کی"))
            {
                 prompt = "به کاربر توضیح دهید که محاسبه بدهی او معمولاً ۷۲ ساعت طول می‌کشد و او باید منتظر بماند. پاسخ باید دوستانه و حرفه‌ای باشد.";
                 modelResponse = await GetModelResponse(url, prompt);
                Console.WriteLine(modelResponse);
            }
            else if (userInput.Contains("چقدر"))
            {
                string userId = ExtractUserId(userInput);

                var payments = await GetUserPayments(userId);

                var totalDebt = payments.Where(p => p.IsPaid).Sum(p => p.PaidAmount);

                prompt = $"مجموع بدهی کاربر {totalDebt} است. یک پاسخ دوستانه به کاربر ارائه دهید.";
                modelResponse = await GetModelResponse(url, prompt);

                Console.WriteLine(modelResponse);
            }
          
        }
    }

    public static string ExtractUserId(string nationalId)
    {
        return nationalId; 
    }

    public static async Task<Payment[]> GetUserPayments(string nationalId)
    {
        string connectionString = "Server=.;Database=Spotbar_v2;Trusted_Connection=True;TrustServerCertificate=True;";
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = @"SELECT       TOP (100) dbo.Drivers.NationalId, dbo.Drivers.Name, dbo.Drivers.FamilyName, dbo.Paids.PaidAmount, dbo.Paids.IsPaid
                        FROM            dbo.Drivers INNER JOIN
                         dbo.Driver_Vehicles ON dbo.Drivers.Id = dbo.Driver_Vehicles.DriverId INNER JOIN
                         dbo.ArrivalsDepartures ON dbo.Driver_Vehicles.Id = dbo.ArrivalsDepartures.Driver_VehicleId INNER JOIN
                         dbo.Transits ON dbo.ArrivalsDepartures.Id = dbo.Transits.ArrivalsDepartureId INNER JOIN
                         dbo.Paids ON dbo.Transits.Id = dbo.Paids.TransitId
where nationalId=@nationalId";
            var payments = await connection.QueryAsync<Payment>(query, new { nationalId = nationalId });
            return payments.ToArray();
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

    public class Payment
    {
       public string NationalId { get; set; }
        public string Name { get; set; }

        public string FamilyName { get; set; }
     //   public DateTime PaidDateTime { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsPaid { get; set; }
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