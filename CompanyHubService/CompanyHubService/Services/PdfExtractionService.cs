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

            // Use OpenAI to extract structured data with hardcoded API key
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
                
                // Add system message
                chatMessages.Messages.Add(new ChatMessage(ChatRole.System, 
                    "You are an AI assistant that extracts structured company information from text. Extract the following as JSON: companyName, location, foundingYear, employeeSize, websiteUrl, description, and an array of projects with details including name, description, type, and completionDate."));
                
                // Add user message with the PDF text
                chatMessages.Messages.Add(new ChatMessage(ChatRole.User, pdfText));
                
                // Set options
                chatMessages.Temperature = 0.1f;
                chatMessages.MaxTokens = 1000;
                
                // Get completions
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