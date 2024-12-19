#region Using directives
using System;
using System.IO;
using System.Net;
using System.Text;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using UAManagedCore;
#endregion

public class StartWebServer : BaseNetLogic
{
    private HttpListener httpListener;
    private string mapFilePath;
    private string baseDirectory;

    /// <summary>
    /// Initializes and starts the HTTP listener.
    /// </summary>
    public override void Start()
    {
        InitWebServer();
    }

    /// <summary>
    /// Stops all running tasks and the HTTP listeners.
    /// </summary>
    public override void Stop()
    {
        httpListener?.Stop();
    }

    /// <summary>
    /// Starts the HTTP listener and serves the OpenStreetMap viewer.
    /// </summary>
    private void InitWebServer()
    {
        // Get the base folder 
        try
        {
            mapFilePath = new ResourceUri(LogicObject.GetVariable("MapFile").Value).Uri;
            baseDirectory = Path.GetDirectoryName(mapFilePath);
            Log.Info("WebServer.Start", "OpenStreetMap file: " + mapFilePath);
            Log.Info("WebServer.Start", "Base directory: " + baseDirectory);
        }
        catch (Exception ex)
        {
            Log.Error("WebServer.Start", "Failed to get the map file path. Exception: " + ex.Message);
            return;
        }
        // Check if the directory exists
        if (!File.Exists(mapFilePath) || !Directory.Exists(baseDirectory))
        {
            Log.Error("WebServer.Start", "OpenStreetMap base directory cannot be accessed: " + mapFilePath);
            return;
        }

        // Read the server address from the configuration
        string serverAddress = "127.0.0.1";

        // Read the server port from the configuration
        var serverPort = LogicObject.GetVariable("ListenerPort").Value;
        // Validate the server port
        if (string.IsNullOrEmpty(serverPort) || !int.TryParse(serverPort, out int port))
        {
            Log.Error("WebServer.Start", "Invalid server port: " + serverPort);
            return;
        }

        try
        {
            // Initialize and start the HTTP listener
            httpListener = new HttpListener();
            string fullAddress = $"http://{serverAddress}:{port}/";
            Log.Info("WebServer.Start", "Starting HTTP listener at " + fullAddress);
            httpListener.Prefixes.Add(fullAddress); // Change this to the desired IP and port
            httpListener.Start();
            _ = httpListener.BeginGetContext(OnRequest, null);
            Log.Info("WebServer.Start", $"HTTP listener started at {fullAddress}");
        }
        catch (Exception ex)
        {
            Log.Error("WebServer.Start", "Failed to start the HTTP listener. Exception: " + ex.Message);
        }
    }

    /// <summary>
    /// Handles incoming HTTP requests.
    /// </summary>
    /// <param name="result">The result of the asynchronous operation.</param>
    private void OnRequest(IAsyncResult result)
    {
        try
        {
            // Check if the listener is still active
            if (!httpListener.IsListening)
                return;

            // Get the context of the incoming request
            var context = httpListener.EndGetContext(result);
            _ = httpListener.BeginGetContext(OnRequest, null);

            var request = context.Request;
            var response = context.Response;

            // Handle the root URL by serving a specific HTML file
            if (request.Url.AbsolutePath == "/")
            {
                if (File.Exists(mapFilePath))
                {
                    response.ContentType = "text/html";
                    byte[] buffer = File.ReadAllBytes(mapFilePath);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    response.StatusCode = (int) HttpStatusCode.NotFound;
                }
            }
            else if (request.Url.AbsolutePath == "/map.html" ||
                    request.Url.AbsolutePath == "/js/GenerateMap.js" ||
                    request.Url.AbsolutePath == "/img/marker.svg")
            {
                ServeFile(ref request, ref response);
            }
            else if (request.Url.AbsolutePath == "/js/MarkersList.js")
            {
                response.ContentType = "application/javascript";
                byte[] buffer = Encoding.UTF8.GetBytes(GetMarkersList());
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                // Handle unknown URLs
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }

            // Close the response stream
            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            Log.Error("WebServer.OnRequest", "Error handling the request: " + ex.Message);
        }
    }

    private static string GetMarkersList()
    {
        var store = Project.Current.Get<Store>("DataStores/EmbeddedDatabase");

        if (store == null)
        {
            Log.Error("GetMarkersList", "Database not found");
            return "var markersList = []";
        }

        store.Query("SELECT * FROM Markers", out string[] header, out object[,] resultSet);

        if (resultSet == null || resultSet.GetLength(0) == 0)
        {
            Log.Warning("GetMarkersList", "No markers found");
            return "var markersList = []";
        }

        try
        {
            StringBuilder stringBuilder = new StringBuilder();
            _ = stringBuilder.Append("var markersList = [\n");
            for (int i = 0; i < resultSet.GetLength(0); i++)
            {
                //Log.Verbose1("GetMarkersList", $"Marker {i}: {resultSet[i, 0]}, {resultSet[i, 1]}, {resultSet[i, 2]}");
                _ = stringBuilder.Append("\t{");
                for (int j = 0; j < resultSet.GetLength(1); j++)
                {
                    if (header[j].EndsWith("tude"))
                    {
                        _ = stringBuilder.Append($"\"{header[j]}\": {Convert.ToDouble(resultSet[i, j]).ToString().Replace(',', '.')},");
                    }
                    else
                    {
                        _ = stringBuilder.Append($"\"{header[j]}\": \"{resultSet[i, j]}\",");
                    }
                }
                _ = stringBuilder.Remove(stringBuilder.Length - 1, 1).Append("},\n");
            }
            _ = stringBuilder.Remove(stringBuilder.Length - 2, 2).Append("\n];");
            return stringBuilder.ToString();
        }
        catch (Exception ex)
        {
            Log.Error("GetMarkersList", "Error creating the markers list: " + ex.Message);
            return "var markersList = []";
        }
    }

    /// <summary>
    /// Serves the requested file to the HTTP response.
    /// </summary>
    /// <param name="request">The HTTP request containing the file path.</param>
    /// <param name="response">The HTTP response to write the file content to.</param>
    private void ServeFile(ref HttpListenerRequest request, ref HttpListenerResponse response)
    {
        string requestedFile = request.Url.AbsolutePath;
        string filePath = Path.Combine(baseDirectory, requestedFile.TrimStart('/'));
        if (File.Exists(filePath))
        {
            response.ContentType = GetContentType(filePath);
            byte[] buffer = File.ReadAllBytes(filePath);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        else
        {
            response.StatusCode = (int) HttpStatusCode.NotFound;
        }
    }

    /// <summary>
    /// Gets the MIME type based on the file extension.
    /// </summary>
    /// <param name="filePath">The path of the file to get the MIME type for.</param>
    /// <returns>The MIME type as a string.</returns>
    private static string GetContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream",
        };
    }
}
