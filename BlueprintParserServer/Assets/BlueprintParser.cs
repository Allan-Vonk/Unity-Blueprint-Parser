using UnityEngine;
using System.IO;
using System;

public class BlueprintParser
{
    bool[,] kernel = new bool[,] { {true, true, true},
                                  {true, true, true}, 
                                  {true, true, true} };
    //Documentation for this file
    
    public BlueprintParser()
    {
        Debug.Log("BlueprintParser constructor called");
    }
    public byte[] ParseBlueprintImage(MemoryStream fileStream, float blackWhiteThreshold, int erodeIterations, int dilateIterations)
    {
        // Load the image from the MemoryStream
        byte[] imageData = fileStream.ToArray();
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);

        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;

        Color[,] pixelData = new Color[width, height];
        //Convert 1D array to 2D array  
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixelData[x, y] = pixels[y * width + x];
            }
        }

        Color averageColor = GetAverageColor(pixels);
        sbyte[,] filteredData = FilterToBlackWhite(pixelData, averageColor, blackWhiteThreshold);
        
        sbyte[,] erodedData = filteredData;
        for (int i = 0; i < erodeIterations; i++)
        {
            Debug.Log("Eroding data");
            erodedData = Erode(ref erodedData, ref kernel);
        }
        sbyte[,] dilatedData = erodedData;
        for (int i = 0; i < dilateIterations; i++)
        {
            Debug.Log("Dilating data");
            dilatedData = Dilate(ref dilatedData, ref kernel);
        }
        
        #region Save and Delete Image Files
        //Save the eroded data as a jpeg file
        //SaveMatrixAsJpeg(dilatedData, Path.Combine(Application.persistentDataPath, $"blueprint_image_{fileId}_filtered.jpeg"));
        
        //string imagePath = Path.Combine(Application.persistentDataPath, $"blueprint_image_{fileId}.jpeg");
        //if (File.Exists(imagePath))
        //{
        //    File.Delete(imagePath);
        //    Debug.Log("Deleted image file: " + imagePath);

        //    string colorFilePath = Path.Combine(Application.persistentDataPath, $"blueprint_image_{fileId}.color");
        //    string colorString = $"{averageColor.r},{averageColor.g},{averageColor.b},{averageColor.a}";
        //    File.WriteAllText(colorFilePath, colorString);
        //}
        #endregion

        return EncodeMatrixAsPNG(dilatedData);
    }
    public Color GetAverageColor(Color[] colors)
    {
        // Initialize color components
        float r = 0f, g = 0f, b = 0f, a = 0f;

        // Sum all color components
        foreach (Color color in colors)
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
    public sbyte[,] FilterToBlackWhite(Color[,] pixelData, Color averageColor, float threshold)
    {
        int width = pixelData.GetLength(0);
        int height = pixelData.GetLength(1);
        sbyte[,] filteredData = new sbyte[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color currentColor = pixelData[x, y];
                float brightness = (currentColor.r + currentColor.g + currentColor.b) / 3f;
                float averageBrightness = (averageColor.r + averageColor.g + averageColor.b) / 3f;

                if (brightness < averageBrightness + threshold)
                {
                    //Wall
                    filteredData[x, y] = -1;
                }
                else
                {
                    //Not wall
                    filteredData[x, y] = 0;
                }
            }
        }

        return filteredData;
    }
    private byte[] EncodeMatrixAsPNG(sbyte[,] matrix)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);
        Texture2D texture = new Texture2D(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, (matrix[x, y] == -1) ? Color.black : Color.white);
            }
        }

        texture.Apply();
        byte[] bytes = texture.EncodeToPNG();
        return bytes;
    }
    private sbyte[,] Erode(ref sbyte[,] matrix, ref bool[,] kernel)
    {
        int height = matrix.GetLength(0);
        int width = matrix.GetLength(1);
        int kernelHeight = kernel.GetLength(0);
        int kernelWidth = kernel.GetLength(1);
        int backgroundPixels = 0;

        sbyte[,] erodedMatrix = new sbyte[height, width];

        int offsetX = kernelWidth / 2;
        int offsetY = kernelHeight / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isForeground = true;

                //Superimpose the kernel
                for (int ky = 0; ky < kernelHeight; ky++)
                {
                    for (int kx = 0; kx < kernelWidth; kx++)
                    {
                        int matrixY = y + ky - offsetY;
                        int matrixX = x + kx - offsetX;

                        //Check if the kernel is outside the matrix
                        if (matrixY < 0 || matrixY >= height || matrixX < 0 || matrixX >= width)
                        {
                            continue; //ignore pixels outside the matrix
                        }
                        //is not wall
                        if (kernel[ky, kx] && matrix[matrixY, matrixX] == 0)
                        {
                            isForeground = false;
                            break;
                        }
                    }
                    if (!isForeground) break;
                }
                if (isForeground)
                {
                    //set wall
                    erodedMatrix[y, x] = -1;
                }
                else
                {
                    //set not wall
                    erodedMatrix[y, x] = 0;
                    backgroundPixels++;
                }
            }
        }
        Debug.Log("Background pixels: " + backgroundPixels);
        return erodedMatrix;
    }
    private sbyte[,] Dilate(ref sbyte[,] matrix, ref bool[,] kernel) 
    {
        int height = matrix.GetLength(0);
        int width = matrix.GetLength(1);
        int kernelHeight = kernel.GetLength(0);
        int kernelWidth = kernel.GetLength(1);
        int foregroundPixels = 0;

        sbyte[,] dilatedMatrix = new sbyte[height, width];

        int offsetX = kernelWidth / 2;
        int offsetY = kernelHeight / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBackground = true; // Start with the assumption that the pixel is not foreground

                // Superimpose the kernel
                for (int ky = 0; ky < kernelHeight; ky++)
                {
                    for (int kx = 0; kx < kernelWidth; kx++)
                    {
                        int matrixY = y + ky - offsetY;
                        int matrixX = x + kx - offsetX;

                        // Check if the kernel is outside the matrix
                        if (matrixY < 0 || matrixY >= height || matrixX < 0 || matrixX >= width)
                        {
                            continue; // Ignore pixels outside the matrix
                        }
                        // If the kernel is foreground and the corresponding matrix pixel is foreground
                        if (kernel[ky, kx] == true && matrix[matrixY, matrixX] == -1)
                        {
                            isBackground = false; // Set to foreground if any part of the kernel overlaps with a foreground pixel
                            break; // No need to check further
                        }
                    }
                    if (!isBackground) break; // Exit the outer loop if we found a foreground pixel
                }

                if (!isBackground)
                {
                    dilatedMatrix[y, x] = -1;
                    foregroundPixels++;
                }
                else
                {
                    dilatedMatrix[y, x] = 0;
                }
            }
        }
        Debug.Log("Foreground pixels: " + foregroundPixels);
        return dilatedMatrix;
    }
}
