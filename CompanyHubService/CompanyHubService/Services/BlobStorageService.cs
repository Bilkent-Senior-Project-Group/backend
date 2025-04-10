using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration)
    {
        _blobServiceClient = new BlobServiceClient(configuration["AzureBlobStorage:ConnectionString"]);
        _containerName = configuration["AzureBlobStorage:ContainerName"];
    }

    public async Task<string> UploadLogoAsync(Stream fileStream, string fileName)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = blobContainer.GetBlobClient(fileName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = "image/png" });

        return blobClient.Uri.ToString(); // Return Azure Blob URL
    }

    public async Task<bool> DeleteLogoAsync(string fileName)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = blobContainer.GetBlobClient(fileName);

        return await blobClient.DeleteIfExistsAsync();
    }
}
