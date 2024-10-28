using System;
using System.Net;
using System.Threading;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
public struct BlueprintParseRequest
{
    public MemoryStream fileStream;
    public float blackWhiteThreshold;
    public int erodeIterations;
    public int dilateIterations;
    public byte[] matrix;
}

public class HttpServer : MonoBehaviour
{
    private HttpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;
    private string dataPath;
    private ConcurrentQueue<Task<BlueprintParseRequest>> parseRequestQueue = new ConcurrentQueue<Task<BlueprintParseRequest>>();
    private BlueprintParser blueprintParser = new BlueprintParser();

    private void Update()
    {
        //Work through the queue
        while (parseRequestQueue.Count > 0)
        {
            if (parseRequestQueue.TryDequeue(out Task<BlueprintParseRequest> request)){
                request.RunSynchronously();
            }
        }
    }
    private void Start()
    {
        Texture2D.allowThreadedTextureCreation = true;
        StartServer();
    }

    private void OnDisable()
    {
        StopServer();
    }

    private void StartServer()
    {
        dataPath = Application.persistentDataPath;
        if (!isRunning)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/");
            listener.Start();

            listenerThread = new Thread(ListenForConnections);
            listenerThread.Start();

            isRunning = true;
            Debug.Log("HttpServer started on http://0.0.0.0:8080/");
        }
    }

    private void ProcessFile(ref BlueprintParseRequest request)
    {
        byte[] matrix = blueprintParser.ParseBlueprintImage(request.fileStream, request.blackWhiteThreshold, request.erodeIterations, request.dilateIterations);
        request.matrix = matrix;
    }

    private void StopServer()
    {
        if (isRunning)
        {
            isRunning = false;
            listener.Stop();
            listenerThread.Join();
            Debug.Log("HttpServer stopped");
        }
    }

    private void ListenForConnections()
    {
        while (isRunning)
        {
            try
            {
                HttpListenerContext context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(HandleRequest, context);
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"Error in ListenForConnections: {e.Message}");
                }
            }
        }
    }
    private async void HandleRequest(object state)
    {
        HttpListenerContext context = (HttpListenerContext)state;
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        string responseString;
        // Handle different paths /parseBlueprint, /status
        switch (request.Url.AbsolutePath)
        {
            //Example query: curl -X POST -H "Content-Type: image/jpeg" --data-binary @C:\Users\Allan\Downloads\Blueprint.jpg http://localhost:8080/parseBlueprint -o C:\Users\Allan\Downloads\Output.jpg
            //example query with parameters: curl -X POST -H "Content-Type: image/jpeg" --data-binary @C:\Users\Allan\Downloads\Blueprint.jpg http://localhost:8080/parseBlueprint?blackWhiteThreshold=-0.1&erodeIterations=2&dilateIterations=2 
            case "/parseBlueprint":
                //Get the parameters from the query string
                float blackWhiteThreshold = request.QueryString["blackWhiteThreshold"] != null ? float.Parse(request.QueryString["blackWhiteThreshold"]) : -0.1f;
                int erodeIterations = request.QueryString["erodeIterations"] != null ? int.Parse(request.QueryString["erodeIterations"]) : 2;
                int dilateIterations = request.QueryString["dilateIterations"] != null ? int.Parse(request.QueryString["dilateIterations"]) : 2;


                //Create a new task to process the blueprint image
                Task<BlueprintParseRequest> parseRequestTask = new Task<BlueprintParseRequest>(() => {
                    MemoryStream imageStream = GetImageStream(request);

                    BlueprintParseRequest parseRequest = new BlueprintParseRequest{fileStream = imageStream, blackWhiteThreshold = blackWhiteThreshold, erodeIterations = erodeIterations, dilateIterations = dilateIterations};
                    ProcessFile(ref parseRequest);

                    return parseRequest;
                });
                //Queue new task to process the blueprint image
                parseRequestQueue.Enqueue(parseRequestTask);
                
                //Return the parserequest matrix as a jpeg
                try{
                    BlueprintParseRequest parseRequestResult = await parseRequestTask;
                    Debug.Log("Returning matrix as jpeg");

                    response.ContentType = "image/jpeg";
                    response.ContentLength64 = parseRequestResult.matrix.Length;
                    response.OutputStream.Write(parseRequestResult.matrix, 0, parseRequestResult.matrix.Length);
                    response.Close();
                }
                catch (Exception e){
                    Debug.LogError("Error returning matrix as jpeg: " + e);
                }
                break;
            case "/status":
                responseString = "Server is running";
                break;
            default:
                //Write error to the response if nothing else was handled   
                response.StatusCode = (int)HttpStatusCode.NotFound;
                responseString = "404 - Not Found";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
                break;
        }
    }
    private MemoryStream GetImageStream(HttpListenerRequest request){
        using (var memoryStream = new MemoryStream())
        {
            request.InputStream.CopyTo(memoryStream);
            return memoryStream;
        }
    }
    #region UploadBlueprintImage
    // private string UploadBlueprintImage(HttpListenerRequest request, HttpListenerResponse response)
    // {
    //     try
    //     {
    //         // Read the request body
    //         using (var memoryStream = new MemoryStream())
    //         {
    //             request.InputStream.CopyTo(memoryStream);
    //             byte[] imageBytes = memoryStream.ToArray();

    //             // Save the image to the server
    //             string uniqueID = Guid.NewGuid().ToString();
    //             string fileName = $"blueprint_image_{uniqueID}.jpeg";
    //             string imagePath = Path.Combine(dataPath, fileName);
    //             File.WriteAllBytes(imagePath, imageBytes);

    //             // // Respond to the client with the unique ID of the uploaded image
    //             // response.StatusCode = (int)HttpStatusCode.OK;
    //             // byte[] buffer = System.Text.Encoding.UTF8.GetBytes(uniqueID);
    //             // response.ContentLength64 = buffer.Length;
    //             // response.OutputStream.Write(buffer, 0, buffer.Length);
    //             return uniqueID;
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         //Response to the client
    //         response.StatusCode = (int)HttpStatusCode.InternalServerError;
    //         byte[] buffer = System.Text.Encoding.UTF8.GetBytes($"Error uploading image: {e.Message}");
    //         response.ContentLength64 = buffer.Length;
    //         response.OutputStream.Write(buffer, 0, buffer.Length);
    //         response.Close();
    //         return null;
    //     }

    // }
    #endregion

}
