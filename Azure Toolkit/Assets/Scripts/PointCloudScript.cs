using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.Azure.Kinect.Sensor;

namespace LightBuzz.Kinect4Azure
{
    public class PointCloudScript: MonoBehaviour
    {
        //Variable for handling Kinect
        KinectSensor kinect;
        //Number of all points of PointCloud 
        int pointsInCloud;
        //Used to draw a set of points
        Mesh mesh;
        //Array of coordinates for each point in PointCloud
        Vector3[] vertices;
        //Array of colors corresponding to each point in PointCloud
        Color32[] colors;
        Color32 ColorNew;
        //List of indexes of points to be rendered
        int[] indices;
        //Color image 
        Texture2D texture;
        RawImage renderImage;
        //Depth Bounds for Rendering
        float depthMin;
        float depthMax;
        [SerializeField] Color paintColour = Color.black;
        [SerializeField] bool realColour = false;

        [SerializeField] private Configuration _configuration;
        [SerializeField] private UniformImage _image;
        [SerializeField] private StickmanManager _stickmanManager;
        [SerializeField] private HandCursor _cursor;
        [SerializeField] private HandButton[] _buttons;
        [SerializeField] private Text[] _texts;

        public float range;
        public float depth;

        public float skeletonMin = 0;
        public float skeletonMax = 5;

        float camDepth;
        float camRange;

        bool init = false;

        void Start()
        {
            kinect = KinectSensor.Create(_configuration);

            if (kinect == null)
            {
                Debug.LogError("Sensor not connected!");
                return;
            }

            kinect.Open();
            renderImage = GameObject.Find("RenderImage").GetComponent<RawImage>();

            for (int i = 0; i < _buttons.Length; i++)
            {
                Color col1 = _buttons[i].GetComponent<UnityEngine.UI.Image>().color;
                col1.a = 0;

                _buttons[i].pause = true;
                _buttons[i].GetComponent<UnityEngine.UI.Image>().color = col1;

            }
            Color col = _cursor.GetComponent<UnityEngine.UI.Image>().color;
            col.a = 0;
            _cursor.GetComponent<UnityEngine.UI.Image>().color = col;

            for (int i = 0; i < _texts.Length; i++)
            {
                Color col1 = _texts[i].color;
                col1.a = 0;

                _texts[i].color = col1;
            }
        }

        private void Update()
        {
            depthMin = depth;
            depthMax = depth + range;

            if (kinect == null || !kinect.IsOpen) return;
            Frame frame = kinect.Update();

            if (frame != null)
            {
                if (frame.DepthFrameSource != null)
                {
                    int width = frame.DepthFrameSource.Width; 
                    int height = frame.DepthFrameSource.Height;

                    if(!init)
                    {
                        init = true;
                        //Allocation of vertex and color storage space for the total number of pixels in the depth image
                        pointsInCloud = width * height;
                        vertices = new Vector3[pointsInCloud];
                        colors = new Color32[pointsInCloud];
                        texture = new Texture2D(width, height);
                        renderImage.texture = texture;
                    }
                }

                if (frame.ColorFrameSource != null)
                {
                    _image.Load(frame.ColorFrameSource);
                }

                if (frame.BodyFrameSource != null)
                {
                    _stickmanManager.Load(frame.BodyFrameSource.Bodies);

                    Body body = frame.BodyFrameSource.Bodies?.Closest();

                    _cursor.Load(body, _image);

                    foreach (HandButton button in _buttons)
                    {
                        button.Load(_cursor);
                    }
                }

                BGRA[] colorArray = frame.ColorFrameSource?.PointCloud;
                Short3[] xyzArray = frame.DepthFrameSource?.PointCloud;

                int previousDrawn = 0;
                for (int i = 0; i < pointsInCloud; i++)
                {
                    float tempZ = xyzArray[i].Z * 0.001f;
                    if (tempZ <= depthMax && tempZ >= depthMin)
                    {
                        previousDrawn = i;
                        vertices[i].x = xyzArray[i].X * 0.001f;
                        vertices[i].y = xyzArray[i].Y * 0.001f;
                        vertices[i].z = xyzArray[i].Z * 0.001f;

                        Color brushColor;

                        if (realColour)
                        {
                            brushColor.r = colorArray[i].R;
                            brushColor.g = colorArray[i].G;
                            brushColor.b = colorArray[i].B;
                            brushColor.a = paintColour.a;

                            if (colors[i] != Color.clear)
                            {
                                colors[i] = Color32.LerpUnclamped(colors[i], new Color32(colorArray[i].R, colorArray[i].G, colorArray[i].B, 255), paintColour.a);
                            }
                            else
                            {
                                colors[i].b = colorArray[i].B;
                                colors[i].g = colorArray[i].G;
                                colors[i].r = colorArray[i].R;
                                colors[i].a = (byte)(paintColour.a * 255);
                            }
                        }
                        else
                        {
                            if (colors[i] != Color.clear)
                            {
                                colors[i] = Color.LerpUnclamped(colors[i], new Color(paintColour.r, paintColour.g, paintColour.b), paintColour.a);
                            }
                            else
                            {
                                colors[i] = paintColour;
                            }
                        }
                    }
                }
                texture.SetPixels32(colors);
                texture.Apply();
            }
        }

        public void clearCanvas()
        {
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
        }

        //Set point cloud color
        public void setColor(int i)
        {
            switch (i)
            {
                default:
                    paintColour = Color.white;
                    break;
                case 1:
                    paintColour = Color.yellow;
                    break;
                case 2:
                    paintColour = Color.red;
                    break;
                case 3:
                    paintColour = Color.magenta;
                    break;
                case 4:
                    paintColour = Color.grey;
                    break;
                case 5:
                    paintColour = Color.green;
                    break;
                case 6:
                    paintColour = Color.cyan;
                    break;
                case 7:
                    paintColour = Color.black;
                    break;
                case 8:
                    paintColour = Color.blue;
                    break;
                case 0:
                    paintColour = Color.white;
                    break;
            }
        }
        public void setRealColor()
        {
            realColour = !realColour;
        }

        public void skeletonPosition(Vector3 pos)
        {

            Debug.Log("Pos: " + pos + " Distance:    " + Vector3.Distance(pos, transform.position) + "  This:   " + transform.position);
            if(Vector3.Distance(pos, transform.position) < skeletonMax && Vector3.Distance(pos, transform.position) > skeletonMin)
            {
                for (int i = 0; i < _buttons.Length; i++)
                {
                    Color col1 = _buttons[i].GetComponent<UnityEngine.UI.Image>().color;
                    col1.a = 255;

                    _buttons[i].pause = false;
                    _buttons[i].GetComponent<UnityEngine.UI.Image>().color = col1;

                }
                Color col = _cursor.GetComponent<UnityEngine.UI.Image>().color;
                col.a = 255;
                _cursor.GetComponent<UnityEngine.UI.Image>().color = col;
                for (int i = 0; i < _texts.Length; i++)
                {
                    Color col1 = _texts[i].color;
                    col1.a = 255;

                    _texts[i].color = col1;
                }
            }
            else
            {
                for (int i = 0; i < _buttons.Length; i++)
                {
                    Color col1 = _buttons[i].GetComponent<UnityEngine.UI.Image>().color;
                    col1.a = 0;

                    _buttons[i].pause = true;
                    _buttons[i].GetComponent<UnityEngine.UI.Image>().color = col1;

                }
                Color col = _cursor.GetComponent<UnityEngine.UI.Image>().color;
                col.a = 0;
                _cursor.GetComponent<UnityEngine.UI.Image>().color = col;

                for (int i = 0; i < _texts.Length; i++)
                {
                    Color col1 = _texts[i].color;
                    col1.a = 0;

                    _texts[i].color = col1;
                }
            }
        }

        //Stop Kinect as soon as this object disappear
        private void OnDestroy()
        {
            kinect.Close();
        }
    }
}
