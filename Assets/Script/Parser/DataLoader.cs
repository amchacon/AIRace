using UnityEngine;

public class DataLoader : MonoBehaviour
{
    /// <summary>
    /// Parse data from Json/file
    /// </summary>
    public Data LoadPlayerData()
    {
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Data.txt");
        string result = System.IO.File.ReadAllText(filePath);
        Data data = JsonUtility.FromJson<Data>(result);
        SetupPlayerData(data);
        return data;
    }

    /// <summary>
    /// Converts player color and gets player icon from web
    /// </summary>
    private void SetupPlayerData(Data data)
    {
        ImageLoader imageLoader = new ImageLoader();
        foreach (var player in data.Players)
        {
            player.trueColor = HexToColor(player.Color);
            StartCoroutine(imageLoader.LoadImage(player, ConvertPlayerIcon));
        }
    }

    /// <summary>
    /// Converts a Texture to a Sprite (Callback ImageLoader Coroutine)
    /// </summary>
    private void ConvertPlayerIcon(Players player, Texture2D texture)
    {
        Rect rect = new Rect(0f, 0f, texture.width, texture.height);
        player.iconSprite = Sprite.Create(texture, rect, Vector2.zero);
    }

    /// <summary>
    /// Converts a Hexa color to RGBA
    /// </summary>
    private Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        byte a = 255;
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, a);
    }
}
