using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class AedenthornUtils
{
    public static bool IgnoreKeyPresses(bool extra = false)
    {
        if (!extra)
            return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true;
        return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true || StoreGui.IsVisible() || InventoryGui.IsVisible() || Menu.IsVisible() || TextViewer.instance?.IsVisible() == true;
    }
    public static bool CheckKeyDown(string value)
    {
        try
        {
            return Input.GetKeyDown(value.ToLower());
        }
        catch
        {
            return false;
        }
    }
    public static bool CheckKeyUp(string value)
    {
        try
        {
            return Input.GetKeyUp(value.ToLower());
        }
        catch
        {
            return false;
        }
    }
    public static bool CheckKeyHeld(string value, bool req = true)
    {
        try
        {
            return Input.GetKey(value.ToLower());
        }
        catch
        {
            return !req;
        }
    }

    public static void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n);
            var value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public static string GetAssetPath(object obj, bool create = false)
    {
        return GetAssetPath(obj.GetType().Namespace, create);
    }
    public static string GetAssetPath(string name, bool create = false)
    {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name);
        if (create && !Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
    public static string GetTransformPath(Transform t)
    {
        if (!t.parent)
        {
            return t.name;

        }
        return GetTransformPath(t.parent) + "/" + t.name;
    }


    public static byte[] EncodeToPNG(Texture2D texture)
    {
        RenderTexture tmp = RenderTexture.GetTemporary(
                                            texture.width,
                                            texture.height,
                                            0,
                                            RenderTextureFormat.Default,
                                            RenderTextureReadWrite.Default);

        // Blit the pixels on texture to the RenderTexture
        Graphics.Blit(texture, tmp);

        // Backup the currently set RenderTexture
        RenderTexture previous = RenderTexture.active;

        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;

        // Create a new readable Texture2D to copy the pixels to it
        Texture2D myTexture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, true, false);

        // Copy the pixels from the RenderTexture to the new Texture
        myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        myTexture2D.Apply();

        // Reset the active RenderTexture
        RenderTexture.active = previous;

        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(tmp);

        // "myTexture2D" now has the same pixels from "texture" and it's readable.

        Texture2D newTexture = new Texture2D(texture.width, texture.height);
        newTexture.SetPixels(myTexture2D.GetPixels());
        newTexture.Apply();
        return newTexture.EncodeToPNG();
    }

}
