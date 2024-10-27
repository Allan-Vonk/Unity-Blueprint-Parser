using UnityEngine;
using System.IO;
using System;

public class BlueprintParser
{
    public BlueprintParser()
    {
        Debug.Log("BlueprintParser constructor called");
    }
    public Color ParseBlueprintImage(string fileId)
    {
        ImageLoader imageLoader = new ImageLoader();
        Texture2D texture = imageLoader.LoadImage(fileId);

        Color averageColor = GetAverageColor(texture.GetPixels());


        
        string imagePath = Path.Combine(Application.persistentDataPath, $"blueprint_image_{fileId}.jpeg");
        if (File.Exists(imagePath))
        {
            File.Delete(imagePath);
            Debug.Log("Deleted image file: " + imagePath);

            string colorFilePath = Path.Combine(Application.persistentDataPath, $"blueprint_image_{fileId}.color");
            string colorString = $"{averageColor.r},{averageColor.g},{averageColor.b},{averageColor.a}";
            File.WriteAllText(colorFilePath, colorString);
        }

        //Debugging in inspector
        return averageColor;
    }
    private Color GetAverageColor(Color[] colors)
    {
        // Initialize color components
        float r = 0f, g = 0f, b = 0f, a = 0f;

        // Sum all color components
        foreach (var color in colors)
        {
            r += color.r;
            g += color.g;
            b += color.b;
            a += color.a;
        }

        // Calculate average for each component
        int count = colors.Length;
        return new Color(r / count, g / count, b / count, a / count);
    }
}