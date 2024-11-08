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
public struct RouteRequest {
    public Vector2Int startNode;
    public Vector2Int endNode;
    public MemoryStream blueprintMatrix;
    public Vector2IntArrayWrapper path;
}
[System.Serializable]
public class Vector2IntArrayWrapper 
{
    public Vector2Int[] path;
}

public class HttpServer : MonoBehaviour
{
    private HttpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;
    private ConcurrentQueue<Task<BlueprintParseRequest>> parseRequestQueue = new ConcurrentQueue<Task<BlueprintParseRequest>>();
    private ConcurrentQueue<Task<RouteRequest>> routeRequestQueue = new ConcurrentQueue<Task<RouteRequest>>();
    private BlueprintParser blueprintParser = new BlueprintParser();

    [SerializeField] Texture2D image;

    private void Update()
    {
        //Work through the queue
        while (parseRequestQueue.Count > 0)
        {
            if (parseRequestQueue.TryDequeue(out Task<BlueprintParseRequest> request)){
                request.RunSynchronously();
            }
        }
        while (routeRequestQueue.Count > 0)
        {
            if (routeRequestQueue.TryDequeue(out Task<RouteRequest> request)){
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

    private void ProcessBlueprint(ref BlueprintParseRequest request)
    {
        byte[] matrix = blueprintParser.ParseBlueprintImage(request.fileStream, request.blackWhiteThreshold, request.erodeIterations, request.dilateIterations);
        request.matrix = matrix;
    }
    private int Get1DIndex(Vector2Int pos, int width){
        return pos.x + pos.y * width;
    }
    private void ProcessRoute(ref RouteRequest request)
    {
        //Implement route finding here
        //preferably in another file with a* and hlsl
        request.path = new Vector2IntArrayWrapper{path = new Vector2Int[]{request.startNode, request.endNode}};
        byte[] imageData = request.blueprintMatrix.ToArray();
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);

        Color[] pixels = texture.GetPixels();
        Color[,] pixelData = new Color[texture.width, texture.height];
        //Convert 1D array to 2D array  
        // TODO: maybe rework so it just acceses the 1d array with a function that converts 2d coordinates to 1d then we can rework the image.setpixels
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                pixelData[x, y] = pixels[y * texture.width + x];
            }
        }
        Color averageColor = blueprintParser.GetAverageColor(pixels);

        sbyte[,] matrix = blueprintParser.FilterToBlackWhite(pixelData, averageColor, 0f);
        uint[,] distanceMatrix = new uint[matrix.GetLength(0), matrix.GetLength(1)];

        try{    
            distanceMatrix = FloodFill.FloodFillMatrix(ref matrix, request.startNode);
        }
        catch (Exception e){
            Debug.LogError("Error in FloodFill: " + e);
        }
        //convert distance matrix to a texture 2d, the max value is 1200, so we need to map them to correctly fit the color range
        image = new Texture2D(distanceMatrix.GetLength(0), distanceMatrix.GetLength(1));
        for (int y = 0; y < distanceMatrix.GetLength(1); y++){
            for (int x = 0; x < distanceMatrix.GetLength(0); x++){
                image.SetPixel(x, y, new Color(distanceMatrix[x, y] / 1200f, 0, 0));
            }
        }
        image.Apply();
        request.path = new Vector2IntArrayWrapper{path = new Vector2Int[]{request.startNode, request.endNode}};
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
            //example query with parameters: curl -X POST -H "Content-Type: image/jpeg" --data-binary @C:\Users\Allan\Downloads\Blueprint.jpg "http://localhost:8080/parseBlueprint?blackWhiteThreshold=-0.1&erodeIterations=2&dilateIterations=2" -o C:\Users\Allan\Downloads\Output.jpg
            case "/parseBlueprint":
                //Get the parameters from the query string
                float blackWhiteThreshold = request.QueryString["blackWhiteThreshold"] != null ? float.Parse(request.QueryString["blackWhiteThreshold"]) : -0.1f;
                int erodeIterations = request.QueryString["erodeIterations"] != null ? int.Parse(request.QueryString["erodeIterations"]) : 2;
                int dilateIterations = request.QueryString["dilateIterations"] != null ? int.Parse(request.QueryString["dilateIterations"]) : 2;


                //Create a new task to process the blueprint image
                Task<BlueprintParseRequest> parseRequestTask = new Task<BlueprintParseRequest>(() => {
                    MemoryStream imageStream = GetImageStream(request);

                    BlueprintParseRequest parseRequest = new BlueprintParseRequest{fileStream = imageStream, blackWhiteThreshold = blackWhiteThreshold, erodeIterations = erodeIterations, dilateIterations = dilateIterations};
                    ProcessBlueprint(ref parseRequest);

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
            //Example query: curl -X POST -H "Content-Type: image/jpeg" --data-binary @C:\Users\Allan\Downloads\Blueprint.jpg http://localhost:8080/getRoute?startNode=0,0&endNode=10,10    
            case "/getRoute":
                Task<RouteRequest> routeRequestTask = new Task<RouteRequest>(() => {
                    MemoryStream imageStream = GetImageStream(request);
                    RouteRequest routeRequest = new RouteRequest{startNode = ParseVector2Int(request.QueryString["startNode"]), endNode = ParseVector2Int(request.QueryString["endNode"]), blueprintMatrix = imageStream};
                    ProcessRoute(ref routeRequest);
                    return routeRequest;
                });
                routeRequestQueue.Enqueue(routeRequestTask);
                try{
                    RouteRequest routeRequestResult = await routeRequestTask;
                    string jsonPath = JsonUtility.ToJson(routeRequestResult.path);
                    byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonPath);

                    response.ContentType = "application/json";
                    response.ContentLength64 = jsonBytes.Length;
                    response.OutputStream.Write(jsonBytes, 0, jsonBytes.Length);
                    response.Close();

                }
                catch (Exception e){
                    Debug.LogError("Error returning route: " + e);
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
    private Vector2Int ParseVector2Int(string value)
    {
        string[] parts = value.Split(',');
        Vector2Int pos = new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
        return pos;
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
