using System.Collections.Generic;
using System.Net;
using RestSharp;

namespace exportDB
{
    public class DoorAPI
    {
        public static bool useCloudServer = false;

        const string baseUrlLocal = "http://localhost:8080/";
        const string baseUrlCloud = "https://mongorevit.herokuapp.com/";

        public static string RestAPIBaseUrl
        {
            get { return useCloudServer ? baseUrlCloud : baseUrlLocal; }
        }

        public static List<Door> Get(string collectionName)
        {
            RestClient client = new RestClient(RestAPIBaseUrl);
            RestRequest request = new RestRequest("/api" + "/" + collectionName, Method.GET);

            IRestResponse<List<Door>> response = client.Execute<List<Door>>(request);

            return response.Data;
        }

        public static HttpStatusCode PostBatch(out string content, out string errorMessage, string collectionName, List<Door> doorData)
        {
            RestClient client = new RestClient(RestAPIBaseUrl);
            RestRequest request = new RestRequest("/api" + "/" + collectionName + "/" + "batch", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(doorData);

            IRestResponse response = client.Execute(request);
            content = response.Content;
            errorMessage = response.ErrorMessage;

            return response.StatusCode;
        }
    }
}
