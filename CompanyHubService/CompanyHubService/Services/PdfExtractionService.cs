using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Configuration;
using Azure.AI.OpenAI;

namespace CompanyHubService.Services
{
    public interface IPdfExtractionService
    {
        Task<string> ExtractCompanyDataFromPdf(Stream pdfStream);
    }

    public class PdfExtractionService : IPdfExtractionService
    {
        private readonly IConfiguration _configuration;

        public PdfExtractionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> ExtractCompanyDataFromPdf(Stream pdfStream)
        {
            // Extract text from PDF
            string pdfText = ExtractTextFromPdf(pdfStream);
            string apiKey = _configuration["OpenAI:ApiKey"];
            // Use OpenAI to extract structured data
            return await ExtractStructuredDataWithOpenAI(pdfText, apiKey);
        }

        private string ExtractTextFromPdf(Stream pdfStream)
        {
            StringBuilder text = new StringBuilder();
            using (PdfReader reader = new PdfReader(pdfStream))
            using (PdfDocument document = new PdfDocument(reader))
            {
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    var strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(i), strategy);
                    text.Append(pageText);
                }
            }
            return text.ToString();
        }

        private async Task<string> ExtractStructuredDataWithOpenAI(string pdfText, string apiKey)
        {
            try
            {
                // Create the client with direct API key
                var client = new OpenAIClient(apiKey);

                var chatMessages = new ChatCompletionsOptions();

                // Updated system message with exact JSON structure that matches our expected output
                chatMessages.Messages.Add(new ChatMessage(ChatRole.System, @"
You are an AI assistant that extracts structured company information from text documents. 
Extract the following information in a JSON format that strictly adheres to this structure:

{
  ""companyName"": string,
  ""description"": string,
  ""foundedYear"": number,
  ""address"": string,
  ""website"": string,
  ""companySize"": string,
  ""phone"": string,
  ""email"": string,
  ""partnerships"": [
    string (partner company names)
  ],
  ""portfolio"": [
    {
      ""projectName"": string,
      ""description"": string,
      ""technologiesUsed"": [
        string (technology names)
      ],
      ""clientType"": string,
      ""startDate"": string (ISO date format),
      ""completionDate"": string (ISO date format),
      ""clientCompanyName"": string,
      ""projectUrl"": string,
    }
  ]
}

Important:
- If you can't find specific information, use empty strings, empty arrays, or null values as appropriate.
- Format dates in ISO format (YYYY-MM-DDTHH:mm:ss.sssZ).
- If you can only extract a year for dates, use the format YYYY-01-01T00:00:00.000Z.
- Do not add any fields that are not in this structure.
- Do not omit any fields from this structure.
- Respond ONLY with the JSON object and nothing else.
- The clientType field must be one of these values: [""Startup"", ""Small Business"", ""Medium-Sized Enterprise (SME)"", ""Large Corporation"", ""Non-Governmental Organization (NGO)"", ""Government Agency"", ""Educational Institution"", ""Research Institute"", ""Other""]
- The companySize field must be one of these values: [""1-10 employees"", ""11-50 employees"", ""51-200 employees"", ""201-500 employees"", ""501-1000 employees"", ""1001-5000 employees"", ""5001-10000 employees"", ""10000+ employees""]
"));

                // Add user message with the PDF text
                chatMessages.Messages.Add(new ChatMessage(ChatRole.User, pdfText));

                // Set options
                chatMessages.Temperature = 0.1f;
                chatMessages.MaxTokens = 2000; // Increased token limit for larger responses

                // Get completions - use GPT-4 model for better extraction
                var response = await client.GetChatCompletionsAsync("gpt-4", chatMessages);

                // Return the content
                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling OpenAI API: {ex.Message}");
                return $"{{ \"error\": \"{ex.Message.Replace("\"", "\\\"")}\" }}";
            }
        }
    }
}