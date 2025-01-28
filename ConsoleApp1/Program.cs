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

        while (true)
        {
            Console.Write("> ");
            string userInput = Console.ReadLine();
            if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("bye");
                break;
            }

            // شناسایی کاربر (مثلاً از طریق userInput)
            string userId = ExtractUserId(userInput); // این تابع باید شناسه کاربر را از ورودی استخراج کند

            // دریافت داده‌های پرداختی کاربر
            var payments = await GetUserPayments(userId);

            // محاسبه بدهی کاربر
            var totalDebt = payments.Where(p => p.IsPaid).Sum(p => p.PaidAmount);

            // آماده‌سازی Prompt برای مدل زبانی
            var prompt = $"مجموع بدهی کاربر {totalDebt} است. یک پاسخ دوستانه به کاربر ارائه دهید.";
            var modelResponse = await GetModelResponse(url, prompt);

            // نمایش پاسخ به کاربر
            Console.WriteLine(modelResponse);
        }
    }

    // تابع برای استخراج شناسه کاربر از ورودی
    public static string ExtractUserId(string nationalId)
    {
        // اینجا می‌توانید از منطق خاصی برای استخراج شناسه کاربر استفاده کنید
        // مثلاً با استفاده از عبارات منظم (Regex) یا الگوهای خاص
        return nationalId; // به عنوان مثال، شناسه کاربر ۱ را برمی‌گردانیم
    }

    // تابع برای دریافت داده‌های پرداختی کاربر از پایگاه داده
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

    // تابع برای ارسال درخواست به مدل زبانی
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

        return aggregatedContent.ToString().Trim();
    }

    // کلاس‌های مدل برای دسریالایز کردن داده‌ها
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