using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncZipDownload.Function
{
    public class AsyncZipDownload
    {
        private readonly IConfiguration configuration;

        public AsyncZipDownload(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName("asyncZipDownload")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log
        )
        {   
            // Get the required variables for zipping data from the query parameters in the url 
            string blobContainerName = req.Query["blobContainerName"];
            string blobPath = req.Query["blobPath"];
            string zipFilename = req.Query["zipFilename"];

            // Get the ConnectionString for AzureContainer from local.settings.json
            string _storageConnectionString = configuration.GetValue<string>("AzureContainerConnectionString");

            ICSharpCode.SharpZipLib.Zip.ZipConstants.DefaultCodePage = System.Text.Encoding.UTF8.CodePage;
            int num = -1;

            var allowSynchronousIoOption = req.HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (allowSynchronousIoOption != null)
            {
                allowSynchronousIoOption.AllowSynchronousIO = true;
            }

            // Create the response variable for streaming zip data
            HttpResponse response = req.HttpContext.Response;
            response.StatusCode = 200;
            response.ContentType = "application/octet-stream";
            response.Headers.Add("content-disposition", "attachment; filename= " + zipFilename);

            // Create Zip output stream
            ZipOutputStream stream = new ZipOutputStream(response.Body);
            stream.SetLevel(0);
            stream.IsStreamOwner = false;

            CancellationToken token = new CancellationToken();

            // Create enumerator which will enumerate over all blobs which come under blobPath
            IEnumerator<BlobItem> enumerator = new BlobServiceClient(_storageConnectionString)
                .GetBlobContainerClient(blobContainerName)
                .GetBlobs(BlobTraits.None, BlobStates.None, blobPath, token)
                .GetEnumerator();

            try
            {   
                // Loop over all blobs and add the data to the zip stream while sending output simultanously
                while (enumerator.MoveNext())
                {
                    BlobItem current = enumerator.Current;
                    System.Console.WriteLine($"Zipping Blob - {current.Name}");
                    ZipEntry entry = new ZipEntry(current.Name);
                    stream.PutNextEntry(entry);
                    new BlockBlobClient(_storageConnectionString, blobContainerName, current.Name).DownloadTo((Stream)stream);
                }
            }
            finally
            {
                if ((num < 0) && (enumerator != null))
                {
                    enumerator.Dispose();
                }
            }

            // Close the stream and flush response body
            stream.Finish();
            stream.Close();
            response.Body.Flush();

            return new EmptyResult();
        }
    }
}
