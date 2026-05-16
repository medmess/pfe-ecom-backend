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
    public async Task<ActionResult<ChatResponseDto>> Chat([FromBody] ChatRequestDto request)
    {
      if (request == null || string.IsNullOrWhiteSpace(request.Message))
      {
        return BadRequest(new ChatResponseDto
        {
          Answer = "Please type a message first."
        });
      }

      var apiKey = _configuration["NvidiaAI:ApiKey"];
      var baseUrl = _configuration["NvidiaAI:BaseUrl"];
      var model = _configuration["NvidiaAI:Model"];

      if (string.IsNullOrWhiteSpace(apiKey))
      {
        return BadRequest(new ChatResponseDto
        {
          Answer = "NVIDIA API key is missing on the backend."
        });
      }

      if (string.IsNullOrWhiteSpace(baseUrl))
      {
        return BadRequest(new ChatResponseDto
        {
          Answer = "NVIDIA BaseUrl is missing on the backend."
        });
      }

      if (string.IsNullOrWhiteSpace(model))
      {
        return BadRequest(new ChatResponseDto
        {
          Answer = "NVIDIA model is missing on the backend."
        });
      }

      try
      {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var body = new
        {
          model,
          messages = new object[]
            {
                        new
                        {
                            role = "system",
                            content = "You are the official AI assistant of Wahran PC Store. Help customers choose gaming PCs, computer components, accessories, delivery options, and order support. Answer clearly and briefly."
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

        using var content = new StringContent(
            jsonBody,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(baseUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
          return BadRequest(new ChatResponseDto
          {
            Answer = $"NVIDIA API error: {(int)response.StatusCode} - {responseJson}"
          });
        }

        using var document = JsonDocument.Parse(responseJson);

        var answer = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return Ok(new ChatResponseDto
        {
          Answer = string.IsNullOrWhiteSpace(answer)
                ? "No AI response received."
                : answer
        });
      }
      catch (Exception ex)
      {
        return BadRequest(new ChatResponseDto
        {
          Answer = $"Backend error: {ex.Message}"
        });
      }
    }
  }
}
