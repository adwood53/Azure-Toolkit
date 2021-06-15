using Microsoft.Azure.Kinect.Sensor;
using LightBuzz.Kinect4Azure.Video;
using System.IO;
using UnityEngine;
using UnityEngine.UI;


namespace LightBuzz.Kinect4Azure
{
    public class BackgroundRemoval : MonoBehaviour
    {
        [SerializeField] private Configuration _configuration;
        [SerializeField] private UniformImage _image;
        [SerializeField] private StickmanManager _stickmanManager;

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
        private Color32[] _colors;

        public Slider redSlider;
        public Slider blueSlider;
        public Slider greenSlider;
        public Slider alphaSlider;

        public Color customColor;

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
                RecordBody = true,
                RecordFloor = false,
                RecordIMU = false
            });

            _recorder.OnRecordingStarted += OnRecordingStarted;
            _recorder.OnRecordingStopped += OnRecordingStopped;
            _recorder.OnRecordingCompleted += OnRecordingCompleted;

            Debug.Log("Video will be saved at " + _recorder.Configuration.Path);
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
                if (frame.ColorFrameSource != null && frame.UserFrameSource != null &&
                    frame.ColorFrameSource.PointCloud != null && frame.UserFrameSource.Data != null)
                {
                    BGRA[] colorData = frame.ColorFrameSource.PointCloud;
                    byte[] userData = frame.UserFrameSource.Data;

                    int width = frame.UserFrameSource.Width;
                    int height = frame.UserFrameSource.Height;
                    int size = width * height;

                    if (_colors == null) _colors = new Color32[size];

                    if (colorData != null && userData != null)
                    {
                        for (int i = 0; i < size; i++)
                        {
                            if (userData[i] != UserFrameSource.Background)
                            {
                                _colors[i].b = colorData[i].B;
                                _colors[i].g = colorData[i].G;
                                _colors[i].r = colorData[i].R;
                                _colors[i].a = colorData[i].A;
                            }
                            else
                            {
                                _colors[i] = new Color(redSlider.value,greenSlider.value,blueSlider.value,alphaSlider.value);
                            }
                        }

                        _image.Load(_colors, width, height);
                    }
                }

                if (frame.BodyFrameSource != null)
                {
                    _stickmanManager.Load(frame.BodyFrameSource.Bodies);
                }
            }
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

        public void BackToRecording()
        {
            mediaBarPlayer.Stop();
            recordingPanel.SetActive(true);
            backBtnGO.SetActive(false);
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
    }
}
