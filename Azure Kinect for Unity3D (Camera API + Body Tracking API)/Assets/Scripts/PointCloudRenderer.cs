using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor;
using Microsoft.Azure.Kinect.Sensor;

[RequireComponent(typeof(VisualEffect))]
public class PointCloudRenderer : MonoBehaviour
{
    private Texture2D texColor; //A Texture holding the Colour Data of the Points in the PointCloud
    private Texture2D texPosScale;//A Texture holding the Position and Scale data of the Points in the PointCloud
    VisualEffect vfx; //The Visual Effect Graph used to Render the PointCloud.
    uint resolution = 2048;

    public float particleSize = 0.1f;
    bool toUpdate = false;
    bool isLoaded = false;
    uint particleCount = 0;

    private void OnValidate()
    {
        vfx = GetComponent<VisualEffect>();
        vfx.visualEffectAsset = (VisualEffectAsset)AssetDatabase.LoadAssetAtPath("Assets/VFX/PointCloud.vfx", typeof(VisualEffectAsset));
    }

    private void Update()
    {
        if (toUpdate)
        {
            toUpdate = false;

            vfx.Reinit();
            vfx.SetUInt(Shader.PropertyToID("ParticleCount"), particleCount);
            vfx.SetTexture(Shader.PropertyToID("TexColor"), texColor);
            vfx.SetTexture(Shader.PropertyToID("TexPosScale"), texPosScale);
            vfx.SetUInt(Shader.PropertyToID("Resolution"), resolution);
        }
    }

    public void Load(BGRA[] color, Short3[] position)
    {
        if (!isLoaded)
        {
            texColor = new Texture2D(position.Length > (int)resolution ? (int)resolution : position.Length, Mathf.Clamp(position.Length / (int)resolution, 1, (int)resolution), TextureFormat.RGBAFloat, false);
            texPosScale = new Texture2D(position.Length > (int)resolution ? (int)resolution : position.Length, Mathf.Clamp(position.Length / (int)resolution, 1, (int)resolution), TextureFormat.RGBAFloat, false);
            isLoaded = true;
        }
        int texWidth = texColor.width;
        int texHeight = texColor.height;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = x + y * texWidth;
                texColor.SetPixel(x, y, new Color(color[index].R, color[index].G, color[index].B, color[index].A));
                var data = new Color(position[index].X, position[index].Y, position[index].Z, particleSize);
                texPosScale.SetPixel(x, y, data);
            }
        }

        texPosScale.Apply();
        texColor.Apply();

        particleCount = (uint)position.Length;
        toUpdate = true;
    }
}

