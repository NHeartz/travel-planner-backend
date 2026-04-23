using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TravelPlanner.API.Models;
using System.Collections.Concurrent;

namespace TravelPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TravelController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    
    // ฐานข้อมูลจำลองสำหรับเก็บแผนการเดินทางที่เจนเสร็จแล้ว (แบบชั่วคราว)
    private static readonly ConcurrentDictionary<string, string> _savedPlans = new();

    public TravelController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
    }

[HttpPost("generate")]
    public async Task<IActionResult> GenerateItinerary([FromBody] TravelRequest request)
    {
        // ดึง API Key จาก appsettings.json
        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return StatusCode(500, "Gemini API Key is missing in configuration.");
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var companionDetail = request.CompanionCount.HasValue ? $"{request.Companion} {request.CompanionCount} คน" : request.Companion;
        var specialPlacesDetail = !string.IsNullOrWhiteSpace(request.SpecialPlaces) ? $"และต้องรวมสถานที่เหล่านี้ลงในแผนด้วย: {request.SpecialPlaces}" : "";

        var prompt = $@"
            สร้างแผนการเดินทางไป {request.Destination} จำนวน {request.Days} วัน งบประมาณ {request.Budget} บาท ไปกับ {companionDetail} สไตล์การเที่ยวแบบ {request.Style} {specialPlacesDetail}
            ให้ตอบกลับมาเป็น JSON Array เท่านั้น ตามโครงสร้างนี้:
            [
              {{ 
                ""day"": 1, 
                ""title"": ""ชื่อธีมของวัน"", 
                ""activities"": [
                  {{ ""description"": ""ชื่อและรายละเอียดกิจกรรม"", ""category"": ""หมวดหมู่ (เลือกจาก: food, shopping, sightseeing, travel, hotel, other)"" }}
                ] 
              }}
            ]";

        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { response_mime_type = "application/json" }
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            
            // ดักจับ Error 503 (Service Unavailable) เพื่อส่งข้อความภาษาไทยกลับไป
            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                return StatusCode(503, "ระบบ AI กำลังมีผู้ใช้งานจำนวนมาก กรุณาลองใหม่อีกครั้งในอีกสักครู่ครับ");
            }

            return StatusCode((int)response.StatusCode, $"Failed to connect to Gemini API: {error}");
        }

        var responseData = await response.Content.ReadFromJsonAsync<GeminiResponse>();
        var generatedJsonText = responseData?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        
        // สร้าง ID สุ่ม และเซฟแผนเก็บไว้ใน Memory
        var planId = Guid.NewGuid().ToString("N");
        _savedPlans[planId] = generatedJsonText ?? "[]";
        
        // คืนค่าทั้ง ID และแผนกลับไปให้หน้าบ้าน
        var jsonResponse = $"{{\"planId\":\"{planId}\", \"itinerary\":{generatedJsonText ?? "[]"}}}";
        return Content(jsonResponse, "application/json");
    }

    [HttpGet("{id}")]
    public IActionResult GetPlan(string id)
    {
        if (_savedPlans.TryGetValue(id, out var planJson))
        {
            return Content(planJson, "application/json");
        }
        return NotFound("ไม่พบแผนการเดินทางนี้");
    }
}