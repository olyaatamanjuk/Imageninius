namespace Imaginator;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

public class Gemini
{
    private static readonly string apiKey = "AIzaSyCKwAqSfGYWOSboIiQ4uTsZELXQUkLzr58";
    
     public static async Task<Dictionary<string, object>> GetImageInfo(FullFileInfo fullFileInfo, string geminiPromt)
    {
        var Result = new Dictionary<string, object>();

        if (string.IsNullOrWhiteSpace(geminiPromt)) geminiPromt = "Опиши, що зображено на фото.";

        using var httpClient = new HttpClient();
        var mimeType =
            "image/jpeg"; // Актуально для JPEG. За потреби: image/png                                                                
        var fileName = fullFileInfo.File.Name;
        byte[] fileBytes = Convert.FromBase64String(fullFileInfo.Base64);

        // Крок 1: Завантаження зображення                                                                                                       
        var startUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";
        var metadata = $"{{\"file\": {{\"display_name\": \"{fileName}\"}}}}";

        var startRequest = new HttpRequestMessage(HttpMethod.Post, startUrl);
        startRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
        startRequest.Headers.Add("X-Goog-Upload-Command", "start");
        startRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);
        startRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", fileBytes.Length.ToString());
        startRequest.Content = new StringContent(metadata, Encoding.UTF8, "application/json");

        var startResponse = await httpClient.SendAsync(startRequest);
        if (!startResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Failed to start upload: {startResponse.StatusCode}");
            return Result;
        }

        var uploadUrl = startResponse.Headers.GetValues("X-Goog-Upload-URL").FirstOrDefault();
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            Console.WriteLine("❌ Upload URL not found.");
            return Result;
        }

        var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
        uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");
        uploadRequest.Content = new ByteArrayContent(fileBytes);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var uploadResponse = await httpClient.SendAsync(uploadRequest);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Upload failed: {uploadResponse.StatusCode}");
            return Result;
        }

        var uploadResultJson = await uploadResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(uploadResultJson);
        var fileUri = doc.RootElement.GetProperty("file").GetProperty("uri").GetString();

        // Крок 2: Виклик Gemini з file_uri (через v1beta)                                                                                       
        var geminiUrl =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { file_data = new { mime_type = mimeType, file_uri = fileUri } },
                        new { text = geminiPromt }
                    }
                }
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var geminiResponse = await httpClient.PostAsync(geminiUrl, jsonContent);
        var geminiResult = await geminiResponse.Content.ReadAsStringAsync();

        Console.WriteLine("📷 Відповідь Gemini:");
        Console.WriteLine(geminiResult);

        //Result = geminiResult;                                                                                                                 

        var resultDict = ExtractJsonFromGemini(geminiResult);

        //return Result;                                                                                                                     
        return resultDict;
    }
     
    public static Dictionary<string, object> ExtractJsonFromGemini(string geminiResult)
    {
        using var doc = JsonDocument.Parse(geminiResult);
        var text = doc
            .RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        // Витягуємо JSON з markdown-блоку ```json ... ```
        var match = Regex.Match(text, @"```json\s*(\{.*?\})\s*```", RegexOptions.Singleline);
        if (!match.Success)
            return null;

        var innerJson = match.Groups[1].Value;

        // Парсимо як словник
        var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(innerJson);
        return jsonData;
    }
}