using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Pfe.Ecom.Api.Contracts;

namespace Pfe.Ecom.Api.Controllers
{
  [ApiController]
  [Route("api/chat")]
  public class ChatController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ChatController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
      _configuration = configuration;
      _httpClient = httpClientFactory.CreateClient();
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> Chat(
        [FromBody] ChatRequestDto request)
    {
      try
      {
        var apiKey = _configuration["NvidiaAI:ApiKey"];
        var baseUrl = _configuration["NvidiaAI:BaseUrl"];
        var model = _configuration["NvidiaAI:Model"];

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
          model = model,
          messages = new object[]
            {
                        new
                        {
                            role = "system",
                            content = "You are the official AI assistant of Wahran PC Store. Help users choose gaming PCs, components, orders, and accessories."
                        },
                        new
                        {
                            role = "user",
                            content = request.Message
                        }
            },
          temperature = 0.5,
          max_tokens = 500
        };

        var jsonBody = JsonSerializer.Serialize(body);

        var content = new StringContent(
            jsonBody,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(baseUrl, content);

        if (!response.IsSuccessStatusCode)
        {
          return BadRequest(new ChatResponseDto
          {
            Answer = "AI service unavailable."
          });
        }

        var responseJson = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(responseJson);

        var answer =
            document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

        return Ok(new ChatResponseDto
        {
          Answer = answer ?? "No response."
        });
      }
      catch
      {
        return BadRequest(new ChatResponseDto
        {
          Answer = "Error while contacting AI service."
        });
      }
    }
  }
}
