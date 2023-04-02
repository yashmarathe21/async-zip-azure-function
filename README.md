# Azure Function to Zip Data and Stream to Response

This is an Azure Function that zips blobs while simultaneously streaming the output to response.
Basically, the function doesn't wait for zipping all the blobs and starts streaming the output parallely.
This ensures that the user doesn't have to wait indefinitely for the zipping process to finish and then start the download.

## Setup

1. Clone repo
2. Install [Dotnet SDK (6.0.406) and Runtime (6.0.14)](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
3. Copy local.settings.sample.json as local.settings.json
3. Add Connection String to Azure Storage in local.settings.json
3. Start the function by using the command - `func host start`

# Test the function locally - 

You can make the following request to zip the data and get the file as response - 
```
curl --location 'http://localhost:7071/api/asyncZipDownload?blobContainerName=testcontainer1&blobPath=test1&zipFileName=test.zip'
```

Note: As of now, the variables are passed as query parameters in the url for convenience of testing, but if you want to send as request body, you can add the following code in the function to retrieve variables from the body
```
// Get the required variables for zipping data from request body
string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
var data = JsonSerializer.Deserialize<BlobData>(requestBody);

string blobContainerName = data.blobContainerName;
string blobPath = data.blobPath;
string zipFilename = data.zipFilename;
```

# Example of how the function actually works

<img src="./public/zipSampelDownload.gif"></img>

You can see here that the download begins almost instantly. The function is zipping all the blobs while simultaneously streaming out to the response.