using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;


using System.Linq;

public class FaceRequests
{

    const string subscriptionKey = "deleted";//Deleted for the public
    string personGroupId = "deleted";

    const string uriBase = "https://utvecklingsprojekt.cognitiveservices.azure.com/face/v1.0/detect";

    public FaceRequests()
    {

    }

    // Gets the analysis of the specified image by using the Face REST API.
    public async Task<string> MakeAnalysisRequest(string imageFilePath)
    {
        HttpClient client = new HttpClient();



        if (!File.Exists(imageFilePath))
        {
            Debug.Log("Can't find picture");
            return "Can't find picture";
        }

        // Request headers.
        client.DefaultRequestHeaders.Add(
            "Ocp-Apim-Subscription-Key", subscriptionKey);

        // Request parameters. A third optional parameter is "details".
        string requestParameters = "returnFaceAttributes=age,gender,emotion";

        // Assemble the URI for the REST API Call.
        string uri = uriBase + "?" + requestParameters;

        HttpResponseMessage response;

        // Request body. Posts a locally stored JPEG image.
        byte[] byteData = GetImageAsByteArray(imageFilePath);

        using (ByteArrayContent content = new ByteArrayContent(byteData))
        {

            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            response = await client.PostAsync(uri, content);
            string contentString;
            int timeout = 500;
            var task = client.PostAsync(uri, content);
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                var task1 = response.Content.ReadAsStringAsync();
                if (await Task.WhenAny(task1, Task.Delay(100)) == task1)
                {
                    contentString = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    contentString = "error";
                }
            }
            else
            {
                contentString = "error";
            }

            // Display the JSON response.
            return contentString;
        }
    }


    static byte[] GetImageAsByteArray(string imageFilePath)
    {
        using (FileStream fileStream =
            new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
        {
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }


    public async Task<string> recognizeImage(Guid input)
    {
        HttpClient client = new HttpClient();


        client.DefaultRequestHeaders.Add(
            "Ocp-Apim-Subscription-Key", subscriptionKey);


        string uri = "https://utvecklingsprojekt.cognitiveservices.azure.com/face/v1.0/identify";

        HttpResponseMessage response;
        string data = "{" +
    '"' + "PersonGroupId" + '"' + ":" + '"' + personGroupId + '"' + "," +
    '"' + "faceIds" + '"' + ": [" +
        '"' + input.ToString() + '"' + "," +
    "]," +
    '"' + "maxNumOfCandidatesReturned" + '"' + ": 1," +
    '"' + "confidenceThreshold" + '"' + ": 0.5" +
"}";

        using (StringContent content = new StringContent(data))
        {

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            response = await client.PostAsync(uri, content);
            string contentString;
            int timeout = 500;
            var task = client.PostAsync(uri, content);
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                var task1 = response.Content.ReadAsStringAsync();
                if (await Task.WhenAny(task1, Task.Delay(100)) == task1)
                {
                    contentString = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    contentString = "error";
                }
            }
            else
            {
                contentString = "error";
            }


            return contentString;
        }

    }

    public async Task<string> getName(string personId)
    {
        HttpClient client = new HttpClient();

        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        var uri = "https://northeurope.api.cognitive.microsoft.com/face/v1.0/persongroups/" + personGroupId + "/persons/" + personId;
        var response = await client.GetAsync(uri);
        string contentString = await response.Content.ReadAsStringAsync();
        return contentString;
    }



    /*
    public async Task<string> recognizeImage(Guid input)
    {
        string personGroupId = "deleted";
        IList<Guid> sourceFaceId = new List<Guid>();
        sourceFaceId.Add(input);
        // Identify the faces in a person group. 
        var identifyResults = await client.Face.IdentifyAsync(sourceFaceId, personGroupId);
        string output = "asd";
        foreach (var identifyResult in identifyResults)
        {
            Person person = await client.PersonGroupPerson.GetAsync(personGroupId, identifyResult.Candidates[0].PersonId);
            
            output = person.Name;
        }
        return output;
    }
    */
}