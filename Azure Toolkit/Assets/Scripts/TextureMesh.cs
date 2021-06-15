using LightBuzz.Kinect4Azure.Video;
using Microsoft.Azure.Kinect.Sensor;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace LightBuzz.Kinect4Azure
{

    public class TextureMesh : MonoBehaviour
    {
        [SerializeField] private Configuration _configuration;

        [SerializeField] private UnityEngine.UI.Image startStopBtnImg;
        [SerializeField] private Sprite[] startStopSprites;
        private bool isRecording = false;

        [SerializeField] private MediaBarPlayer mediaBarPlayer;
        [SerializeField] private GameObject recordingPanel;
        [SerializeField] private GameObject backBtnGO;

        private bool stopPlayback = false;
        private bool showSavingPanel = false;
        [SerializeField] private GameObject savingPanelGO;

        private KinectSensor _sensor;
        private VideoRecorder _recorder;

        [Tooltip("The rotation and zoom speed when using the left/right/top/down arrow keys or the mouse wheel.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float _speed = 0.5f;

        public bool replaceColor = false;
        public Color newColor = Color.green;

        //Width and Height of Depth image.
        int depthWidth;
        int depthHeight;
        //Number of all points of PointCloud 
        int num;
        //Used to draw a set of points
        Mesh mesh;
        //Array of coordinates for each point in PointCloud
        Vector3[] vertices;
        //Array of colors corresponding to each point in PointCloud
        Color32[] colors;
        //List of indexes of points to be rendered
        int[] indeces;
        //Color image to be attatched to mesh
        Texture2D texture;
        bool initMesh = true;

        public float range;
        public float depth;
        int nearClip = 300;

        private void Start()
        {
            _sensor = KinectSensor.Create(_configuration);

            if (_sensor == null)
            {
                Debug.LogError("Sensor not connected!");
                return;
            }

            _sensor.Open();

            _recorder = new VideoRecorder(new VideoConfiguration
            {
                Path = Path.Combine(Application.persistentDataPath, "Video"),
                ColorResolution = _sensor.Configuration.ColorResolution.Size(),
                DepthResolution = _sensor.Configuration.DepthMode.Size(),
                RecordColor = true,
                RecordDepth = true,
                RecordBody = false,
                RecordFloor = false,
                RecordIMU = false
            });

            _recorder.OnRecordingStarted += OnRecordingStarted;
            _recorder.OnRecordingStopped += OnRecordingStopped;
            _recorder.OnRecordingCompleted += OnRecordingCompleted;

            Debug.Log("Video will be saved at " + _recorder.Configuration.Path);

            Camera.main.transform.LookAt(transform.position);
        }


        private void OnRecordingCompleted()
        {
            Debug.Log("Recording completed");

            showSavingPanel = false;
        }

        private void OnRecordingStopped()
        {
            Debug.Log("Recording stopped");

            showSavingPanel = true;
        }

        private void OnRecordingStarted()
        {
            Debug.Log("Recording started");

            stopPlayback = true;
        }

        private void Update()
        {
            if (stopPlayback)
            {
                mediaBarPlayer.Stop();

                stopPlayback = false;
            }

            if (savingPanelGO.activeSelf != showSavingPanel)
            {
                savingPanelGO.SetActive(showSavingPanel);

                // Playback
                if (!showSavingPanel)
                {
                    mediaBarPlayer.LoadVideo(_recorder.Configuration.Path);
                    mediaBarPlayer.Play();
                    backBtnGO.SetActive(true);
                    recordingPanel.SetActive(false);
                }
            }

            UpdateFrame();
        }

        private void OnDestroy()
        {
            _sensor?.Close();

            _recorder.OnRecordingStarted -= OnRecordingStarted;
            _recorder.OnRecordingStopped -= OnRecordingStopped;
            _recorder.OnRecordingCompleted -= OnRecordingCompleted;
            _recorder.Dispose();

            mediaBarPlayer.Dispose();
        }

        private void UpdateFrame()
        {
            Frame frame = null;

            if (mediaBarPlayer.IsPlaying)
            {
                frame = mediaBarPlayer.Update();
            }
            else if (_sensor != null && _sensor.IsOpen)
            {
                frame = _sensor.Update();
            }

            _recorder?.Update(frame);

            if (frame != null)
            {
                if (frame.DepthFrameSource != null)
                {
                    if (initMesh)
                    {
                        initMesh = false;
                        depthWidth = frame.DepthFrameSource.Width;
                        depthHeight = frame.DepthFrameSource.Height;
                        num = depthWidth * depthHeight;

                        //Instantiate mesh
                        mesh = new Mesh();
                        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                        //Allocation of vertex and color storage space for the total number of pixels in the depth image
                        vertices = new Vector3[num];
                        colors = new Color32[num];
                        texture = new Texture2D(depthWidth, depthHeight);
                        Vector2[] uv = new Vector2[num];
                        Vector3[] normals = new Vector3[num];
                        indeces = new int[6 * (depthWidth - 1) * (depthHeight - 1)];

                        //Initialization of uv and normal 
                        int index = 0;
                        for (int y = 0; y < depthHeight; y++)
                        {
                            for (int x = 0; x < depthWidth; x++)
                            {
                                uv[index] = new Vector2(((float)(x + 0.5f) / (float)(depthWidth)), ((float)(y + 0.5f) / ((float)(depthHeight))));
                                normals[index] = new Vector3(0, -1, 0);
                                index++;
                            }
                        }

                        //Allocate a list of point coordinates, colors, and points to be drawn to mesh
                        mesh.vertices = vertices;
                        mesh.uv = uv;
                        mesh.normals = normals;

                        gameObject.GetComponent<MeshRenderer>().materials[0].mainTexture = texture;
                        gameObject.GetComponent<MeshFilter>().mesh = mesh;
                    }
                    if (frame.ColorFrameSource != null)
                    {
                        BGRA[] colorArray = frame.ColorFrameSource?.PointCloud;
                        Short3[] PointCloud = frame.DepthFrameSource?.PointCloud;

                        int triangleIndex = 0;
                        int pointIndex = 0;
                        int topLeft, topRight, bottomLeft, bottomRight;
                        int tl, tr, bl, br;
                        for (int y = 0; y < depthHeight; y++)
                        {
                            for (int x = 0; x < depthWidth; x++)
                            {

                                vertices[pointIndex].x = PointCloud[pointIndex].X * 0.001f;
                                vertices[pointIndex].y = -PointCloud[pointIndex].Y * 0.001f;
                                vertices[pointIndex].z = PointCloud[pointIndex].Z * 0.001f;

                                colors[pointIndex].a = 255;
                                colors[pointIndex].b = colorArray[pointIndex].B;
                                colors[pointIndex].g = colorArray[pointIndex].G;
                                colors[pointIndex].r = colorArray[pointIndex].R;

                                if (x != (depthWidth - 1) && y != (depthHeight - 1))
                                {
                                    topLeft = pointIndex;
                                    topRight = topLeft + 1;
                                    bottomLeft = topLeft + depthWidth;
                                    bottomRight = bottomLeft + 1;
                                    tl = PointCloud[topLeft].Z;
                                    tr = PointCloud[topRight].Z;
                                    bl = PointCloud[bottomLeft].Z;
                                    br = PointCloud[bottomRight].Z;

                                    if (tl > nearClip && tr > nearClip && bl > nearClip)
                                    {
                                        indeces[triangleIndex++] = topLeft;
                                        indeces[triangleIndex++] = topRight;
                                        indeces[triangleIndex++] = bottomLeft;
                                    }
                                    else
                                    {
                                        indeces[triangleIndex++] = 0;
                                        indeces[triangleIndex++] = 0;
                                        indeces[triangleIndex++] = 0;
                                    }

                                    if (bl > nearClip && tr > nearClip && br > nearClip)
                                    {
                                        indeces[triangleIndex++] = bottomLeft;
                                        indeces[triangleIndex++] = topRight;
                                        indeces[triangleIndex++] = bottomRight;
                                    }
                                    else
                                    {
                                        indeces[triangleIndex++] = 0;
                                        indeces[triangleIndex++] = 0;
                                        indeces[triangleIndex++] = 0;
                                    }
                                }
                                pointIndex++;
                            }
                        }

                        texture.SetPixels32(colors);
                        texture.Apply();

                        mesh.vertices = vertices;

                        mesh.triangles = indeces;
                        mesh.RecalculateBounds();
                    }
                }
            }
        }

        public void BackToRecording()
        {
            mediaBarPlayer.Stop();
            recordingPanel.SetActive(true);
            backBtnGO.SetActive(false);
        }

        public void StartStopRecording()
        {
            isRecording = !isRecording;
            startStopBtnImg.sprite = startStopSprites[isRecording ? 1 : 0];

            Debug.Log("Recording: " + isRecording);

            if (isRecording)
                _recorder.Start();
            else
                _recorder.Stop();
        }

        //Camera Stuff
        private void LateUpdate()
        {
            Vector3 cameraPosition = Camera.main.transform.localPosition;
            Vector3 originPosition = transform.position;
            float angle = _speed * 100.0f * Time.deltaTime;

            if (Input.GetKey(KeyCode.RightArrow))
            {
                Camera.main.transform.RotateAround(originPosition, Vector3.up, angle);
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                Camera.main.transform.RotateAround(originPosition, Vector3.down, angle);
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                Camera.main.transform.RotateAround(originPosition, Vector3.right, angle);
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                Camera.main.transform.RotateAround(originPosition, Vector3.left, angle);
            }

            if (Input.mouseScrollDelta != Vector2.zero)
            {
                Camera.main.transform.localPosition = new Vector3(cameraPosition.x, cameraPosition.y, cameraPosition.z + Input.mouseScrollDelta.y * _speed);
            }
        }
    }
}
