using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SaveSpritesFromTexture : EditorWindow
{
    [SerializeField]
    private Texture2D textureToSave; // Texture2D to save

    [MenuItem("Custom/Save Sprites From Texture")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow<SaveSpritesFromTexture>("Save Sprites");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Select Texture2D to Save", EditorStyles.boldLabel);
        textureToSave = EditorGUILayout.ObjectField("Texture", textureToSave, typeof(Texture2D), false) as Texture2D;

        if (GUILayout.Button("Save Sprites") && textureToSave != null)
        {
            SaveSprites();
        }
    }

    private void SaveSprites()
    {
        Texture2D texture = textureToSave;

        // Check if the texture is readable. If not, try to make it readable.
        if (!texture.isReadable)
        {
            Debug.Log("The texture is not readable. Attempting to make it readable...");
            TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.isReadable = true;
                textureImporter.SaveAndReimport();
                Debug.Log("Texture is now readable.");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError("Failed to make the texture readable. Check the import settings of the texture.");
                return;
            }
        }

        // Load all sprite assets from the texture
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(texture));
        Sprite[] sprites = System.Array.FindAll(assets, asset => asset is Sprite).Cast<Sprite>().ToArray();

        Debug.LogFormat("sprites.Length: {0}", sprites.Length);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("No sprites found in the selected Texture2D.");
            return;
        }

        string folderPath = Application.dataPath + "/SavedSprites/";

        // Check if the directory exists, and create it if it doesn't.
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        Dictionary<string, Texture2D> spriteTextures = new Dictionary<string, Texture2D>();

        foreach (Sprite sprite in sprites)
        {
            Texture2D spriteTexture = SpriteToTexture2D(sprite);
            spriteTextures[sprite.name] = spriteTexture;
        }

        // Create a dictionary to store sprite names and their corresponding Rects
        Dictionary<string, Rect> spriteRects = new Dictionary<string, Rect>();

        foreach (Sprite sprite in sprites)
        {
            spriteRects[sprite.name] = sprite.rect;
        }

        // Check for overlapping Rects and merge them
        List<string> mergedSprites = new List<string>();

        foreach (var sprite1 in spriteRects)
        {
            string spriteName1 = sprite1.Key;
            Rect rect1 = sprite1.Value;

            if (!mergedSprites.Contains(spriteName1))
            {
                Texture2D mergedTexture = new Texture2D((int)rect1.width, (int)rect1.height);
                Color[] mergedPixels = new Color[mergedTexture.width * mergedTexture.height];

                foreach (var sprite2 in spriteRects)
                {
                    string spriteName2 = sprite2.Key;
                    Rect rect2 = sprite2.Value;

                    if (spriteName1 != spriteName2 && !mergedSprites.Contains(spriteName2))
                    {
                        if (rect1.Overlaps(rect2))
                        {
                            // Calculate the intersection of the two Rects
                            Rect intersection = Rect.MinMaxRect(
                                Mathf.Max(rect1.x, rect2.x),
                                Mathf.Max(rect1.y, rect2.y),
                                Mathf.Min(rect1.xMax, rect2.xMax),
                                Mathf.Min(rect1.yMax, rect2.yMax)
                            );

                            // Copy pixels from the overlapping area of both sprites
                            for (int y = 0; y < (int)intersection.height; y++)
                            {
                                for (int x = 0; x < (int)intersection.width; x++)
                                {
                                    int destX = (int)(x + (intersection.x - rect1.x));
                                    int destY = (int)(y + (intersection.y - rect1.y));
                                    Color pixel1 = spriteTextures[spriteName1].GetPixel(x, y);
                                    Color pixel2 = spriteTextures[spriteName2].GetPixel((int)(x + (intersection.x - rect2.x)), (int)(y + (intersection.y - rect2.y)));
                                    mergedPixels[destY * mergedTexture.width + destX] = Color.Lerp(pixel1, pixel2, 0.5f); // Merge pixel colors
                                }
                            }

                            mergedSprites.Add(spriteName2);
                        }
                    }
                }

                // Copy pixels from the first sprite
                for (int y = 0; y < (int)rect1.height; y++)
                {
                    for (int x = 0; x < (int)rect1.width; x++)
                    {
                        mergedPixels[y * mergedTexture.width + x] = spriteTextures[spriteName1].GetPixel(x, y);
                    }
                }

                // Apply merged pixels to the new texture
                mergedTexture.SetPixels(mergedPixels);
                mergedTexture.Apply();

                // Save the merged sprite as a PNG
                byte[] mergedBytes = mergedTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes(folderPath + spriteName1 + ".png", mergedBytes);
            }
        }

        Debug.Log("Sprites saved successfully to: " + folderPath);
    }

    private Texture2D SpriteToTexture2D(Sprite sprite)
    {
        Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
        texture.SetPixels(sprite.texture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y,
                            (int)sprite.rect.width, (int)sprite.rect.height));
        texture.Apply();
        return texture;
    }
}
