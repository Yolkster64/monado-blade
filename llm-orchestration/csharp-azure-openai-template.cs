// C# Azure OpenAI LLM API Template
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class LLMController : ControllerBase
{
    [HttpPost("generate")]
    public IActionResult Generate([FromBody] QueryModel query)
    {
        // Replace with your Azure OpenAI call
        var client = new OpenAIClient("<your-endpoint>", new AzureKeyCredential("<your-key>"));
        var response = client.GetCompletions("<deployment-id>", query.Prompt);
        return Ok(new { result = response.Value.Choices[0].Text });
    }
}

public class QueryModel
{
    public string Prompt { get; set; }
}
