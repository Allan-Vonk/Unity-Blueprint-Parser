using System;
using System.Net;
using System.Threading;
using UnityEngine;

public class HttpServer : MonoBehaviour
{
    private HttpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;

    private void Start()
    {
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
            case "/parseBlueprint":
                responseString = "Not implemented yet";
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
}
