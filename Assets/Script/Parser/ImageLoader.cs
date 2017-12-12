using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ImageLoader
{
    /// <summary>
    /// Downloads texture (player icon) from URL
    /// </summary>
    public IEnumerator LoadImage(Players player, Action<Players, Texture2D> OnCompleteCallback)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(player.Icon);
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log("Load Image: " + www.error);
        }
        else
        {
            Texture2D texture = new Texture2D(1, 1);
            texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            if (OnCompleteCallback != null)
                OnCompleteCallback(player, texture);
        }
    }
}
