using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http; 
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using System.Threading;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using System.Text;

namespace OCRSolution
{
    public static class OCRFunction
    {
        static string endpoint = "https://ocr-cognitive-service.cognitiveservices.azure.com";
        static string key = "4fa0ca88e5b14b359eb0be57b4915c51";
        static ComputerVisionClient client;

        private static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        public static async Task<List<string>> ReadImage(ComputerVisionClient client, IFormFile file)
        {
            List<string> extractedLines;
            using (var stream = file.OpenReadStream())
            {
                var ocrResult = await client.RecognizePrintedTextInStreamAsync(true, stream);
                extractedLines = ExtractWordsFromOcrResult(ocrResult);
            }
            return extractedLines;
        }

        private static List<string> ExtractWordsFromOcrResult(OcrResult ocrResult)
        {
            
            var result = new List<string>();

            foreach (var line in ocrResult.Regions[0].Lines)
            {
                var lineText = new StringBuilder();
                foreach (var word in line.Words)
                {
                    lineText.Append(word.Text + " ");
                }
                result.Add(lineText.ToString());
            }
            return result;
        }

        [FunctionName("OCRFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            client = Authenticate(endpoint, key);
            try
            {
                var formdata = await req.ReadFormAsync();
                var files = req.Form.Files;
                var resultCollection = new List<List<string>>();
                foreach (var file in files)
                {
                    var fileRsult= await ReadImage(client, file);
                    resultCollection.Add(fileRsult);
                }
                return new OkObjectResult(resultCollection);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }

           

        }

    }
}
