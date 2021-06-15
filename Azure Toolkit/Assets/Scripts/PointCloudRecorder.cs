using LightBuzz.Kinect4Azure.Video;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace LightBuzz.Kinect4Azure
{
    public class PointCloudRecorder : MonoBehaviour
    {
        public bool StartStop = false;

        [SerializeField] private Configuration _configuration;
        [SerializeField] private PointCloud _pointCloud;

        [Tooltip("The rotation and zoom speed when using the left/right/top/down arrow keys or the mouse wheel.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float _speed = 0.5f;

        [SerializeField] private Image startStopBtnImg;
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

            Camera.main.transform.LookAt(_pointCloud.transform.position);
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

        private void OnDestroy()
        {
            _sensor?.Close();

            _recorder.OnRecordingStarted -= OnRecordingStarted;
            _recorder.OnRecordingStopped -= OnRecordingStopped;
            _recorder.OnRecordingCompleted -= OnRecordingCompleted;
            _recorder.Dispose();

            mediaBarPlayer.Dispose();
        }

        private void Update()
        {
            if (StartStop)
            {
                StartStopRecording();
                StartStop = false;
            }

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
                var pointCloudDepth = frame.DepthFrameSource?.PointCloud;
                var pointCloudColor = frame.ColorFrameSource?.PointCloud;

                _pointCloud.Load(pointCloudColor, pointCloudDepth);
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

        //Camera Stuff
        private void LateUpdate()
        {
            Vector3 cameraPosition = Camera.main.transform.localPosition;
            Vector3 originPosition = _pointCloud.transform.position;
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