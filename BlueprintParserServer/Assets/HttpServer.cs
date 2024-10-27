using System;
using System.Net;
using System.Threading;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class HttpServer : MonoBehaviour
{
    private HttpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;
    private string dataPath;
    private Queue<string> fileQueue = new Queue<string>();
    private object queueLock = new object();
    private BlueprintParser blueprintParser = new BlueprintParser();
    [SerializeField] Color averageColor;

    public void QueueFile(string fileId)
    {
        lock (queueLock)
        {
            fileQueue.Enqueue(fileId);
        }
    }

    private void Update()
    {
        //Work through the queue
        lock (queueLock)
        {
            while (fileQueue.Count > 0)
            {
                string fileId = fileQueue.Dequeue();
                ProcessFile(fileId);
            }
        }
    }

    private void ProcessFile(string fileId)
    {
        Debug.Log("Processing file with ID: " + fileId);
        Color color = blueprintParser.ParseBlueprintImage(fileId);
        averageColor = color;
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
    /// <summary>
    /// Listens for incoming HTTP connections and processes them asynchronously.
    /// This method runs in a separate thread to avoid blocking the main Unity thread.
    /// It continuously accepts new connections while the server is running and
    /// delegates the request handling to a thread pool for parallel processing.
    /// </summary>
    /// <remarks>
    /// If an exception occurs during the listening process, it will be logged,
    /// but only if the server is still running to avoid unnecessary error messages
    /// during server shutdown.
    /// </remarks>
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
    private void HandleRequest(object state)
    {
        HttpListenerContext context = (HttpListenerContext)state;
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        string responseString;
        // Handle different paths /parseBlueprint, /status
        switch (request.Url.AbsolutePath)
        {
            //Example query: curl -X POST -H "Content-Type: image/jpeg" --data-binary @C:\Users\Allan\Downloads\Blueprint.jpg http://localhost:8080/parseBlueprint  
            case "/parseBlueprint":
                string ID = UploadBlueprintImage(request, response);
                QueueFile(ID);
                responseString = ID;
                break;
            //Example query: curl http://localhost:8080/retrieveColor?fileId=11964a2c-8b05-4b93-becb-fb0551881e34
            case "/retrieveColor":
                string color = RetrieveColor(request, response);
                responseString = color;
                break;
            case "/status":
                responseString = "Server is running";
                break;
            default:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                responseString = "404 - Not Found";
                break;
        }

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }
    private string UploadBlueprintImage(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // Read the request body
            using (var memoryStream = new MemoryStream())
            {
                request.InputStream.CopyTo(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();

                // Save the image to the server
                string uniqueID = Guid.NewGuid().ToString();
                string fileName = $"blueprint_image_{uniqueID}.jpeg";
                string imagePath = Path.Combine(dataPath, fileName);
                File.WriteAllBytes(imagePath, imageBytes);

                //Response to the client
                response.StatusCode = (int)HttpStatusCode.OK;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(uniqueID);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                return uniqueID;
            }
        }
        catch (Exception e)
        {
            //Response to the client
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes($"Error uploading image: {e.Message}");
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            return null;
        }
        finally
        {
            response.Close();
        }
    }
    private string RetrieveColor(HttpListenerRequest request, HttpListenerResponse response){
        try{
            string fileId = request.QueryString["fileId"];
            string colorString = File.ReadAllText(Path.Combine(dataPath, $"blueprint_image_{fileId}.color"));

            response.StatusCode = (int)HttpStatusCode.OK;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(colorString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            return colorString;
        }
        catch (Exception e)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes($"Error retrieving color: {e.Message}");
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            return null;
        }
    }
}
