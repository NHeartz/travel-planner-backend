using System.Text.Json.Serialization;

namespace TravelPlanner.API.Models;

public class TravelRequest
{
    public string Destination { get; set; } = string.Empty;
    public int Days { get; set; }
    public int Budget { get; set; }
    public string Companion { get; set; } = string.Empty;
    public int? CompanionCount { get; set; }
    public string Style { get; set; } = string.Empty;
    public string SpecialPlaces { get; set; } = string.Empty;
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public Candidate[]? Candidates { get; set; }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public Part[]? Parts { get; set; }
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}