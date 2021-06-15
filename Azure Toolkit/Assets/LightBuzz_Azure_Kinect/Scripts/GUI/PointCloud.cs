using Microsoft.Azure.Kinect.Sensor;
using UnityEngine;

namespace LightBuzz.Kinect4Azure
{
    public class PointCloud : MonoBehaviour
    {
        private Mesh _mesh;
        private int[] _indices;
        private Vector3[] _vertices;
        private Color32[] _colors;

        public ushort minDepth = 1000;
        public ushort maxDepth = 2000;

        public bool isPlaykit = false;
        public bool isCustomColour = false;
        public Color32 customColor = Color.red;

        bool firstFrame = true;
        BGRA[] pointCloudColorPrev;
        Short3[] pointCloudDepthPrev;



        /// <summary>
        /// Loads the point cloud data.
        /// </summary>
        /// <param name="pointCloudColor">The RGBA point cloud data.</param>
        /// <param name="pointCloudDepth">The Depth point cloud data.</param>
        public void Load(BGRA[] pointCloudColor, Short3[] pointCloudDepth)
        {
            if (pointCloudColor == null || pointCloudColor.Length == 0) return;
            if (pointCloudDepth == null || pointCloudDepth.Length == 0) return;
            int size = pointCloudDepth.Length;

            if (_mesh == null)
            {
                _mesh = new Mesh {indexFormat = UnityEngine.Rendering.IndexFormat.UInt32};

                _vertices = new Vector3[size];
                _colors = new Color32[size];
                _indices = new int[size];

                for (int i = 0; i < size; i++)
                {
                    _indices[i] = i;
                }

                _mesh.vertices = _vertices;
                _mesh.colors32 = _colors;
                _mesh.SetIndices(_indices, MeshTopology.Points, 0);

                gameObject.GetComponent<MeshFilter>().mesh = _mesh;
            }

            if (firstFrame)
            {
                firstFrame = false;
                pointCloudColorPrev = pointCloudColor;
                pointCloudDepthPrev = pointCloudDepth;
                for (int i = 0; i < size; i++)
                {
                    pointCloudColorPrev[i].B = 0;
                    pointCloudColorPrev[i].G = 0;
                    pointCloudColorPrev[i].R = 0;
                    pointCloudColorPrev[i].A = 0;
                }
            }

            for (int i = 0; i < size; i++)
            {

                if (pointCloudDepth[i].Z >= minDepth && pointCloudDepth[i].Z <= maxDepth)
                {
                    if (isPlaykit)
                    {
                        _vertices[i].x = pointCloudDepth[i].X / 1000.0f;
                        _vertices[i].y = -pointCloudDepth[i].Y / 1000.0f;
                        _vertices[i].z = 1f;
                    }
                    else
                    {
                        _vertices[i].x = pointCloudDepth[i].X / 1000.0f;
                        _vertices[i].y = -pointCloudDepth[i].Y / 1000.0f;
                        _vertices[i].z = pointCloudDepth[i].Z / 1000.0f;
                    }

                    if (!isCustomColour)
                    {
                        _colors[i].b = pointCloudColor[i].B;
                        _colors[i].g = pointCloudColor[i].G;
                        _colors[i].r = pointCloudColor[i].R;
                        _colors[i].a = pointCloudColor[i].A;

                        pointCloudColorPrev[i].B = pointCloudColor[i].B;
                        pointCloudColorPrev[i].G = pointCloudColor[i].G;
                        pointCloudColorPrev[i].R = pointCloudColor[i].R;
                        pointCloudColorPrev[i].A = pointCloudColor[i].A;
                    }
                    else
                    {
                        _colors[i] = customColor;
                        pointCloudColorPrev[i].B = customColor.b;
                        pointCloudColorPrev[i].G = customColor.g;
                        pointCloudColorPrev[i].R = customColor.r;
                        pointCloudColorPrev[i].A = customColor.a;
                    }
                }
                else
                {
                    _vertices[i].x = pointCloudDepth[i].X / 1000.0f;
                    _vertices[i].y = -pointCloudDepth[i].Y / 1000.0f;
                    _vertices[i].z = 1f;

                    if (isPlaykit)
                    {
                        _colors[i].b = pointCloudColorPrev[i].B;
                        _colors[i].g = pointCloudColorPrev[i].G;
                        _colors[i].r = pointCloudColorPrev[i].R;
                        _colors[i].a = pointCloudColorPrev[i].A;
                    }
                    else
                    {
                        _colors[i] = Color.clear;
                    }

                }
            }

            _mesh.vertices = _vertices;
            _mesh.colors32 = _colors;
            _mesh.RecalculateBounds();
        }
    }
}