using UnityEngine;
using System.IO;
using System;

public class ImageLoader
{
    public ImageLoader()
    {
        Debug.Log("ImageLoader constructor called");
    }

    public Texture2D LoadImage(string fileId)
    {
        try
        {
           Debug.Log("Loading image from " + fileId);
           
           string imagePath = Path.Combine(Application.persistentDataPath, $"blueprint_image_{fileId}.jpeg");
           if (!File.Exists(imagePath))
           {
               Debug.LogError("Image file does not exist: " + imagePath);
               return null;
           }
           Texture2D texture = new Texture2D(0, 0);
           texture.LoadImage(File.ReadAllBytes(imagePath));
           return texture;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading image: {e.Message}");
            return null;
        }
    }
}