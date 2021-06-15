using UnityEngine;
using UnityEngine.UI;

namespace LightBuzz.Kinect4Azure
{
    public class AzurePlaykit: MonoBehaviour
    {
        [SerializeField] private Configuration _configuration;
        [SerializeField] private UniformImage _image;
        [SerializeField] private StickmanManager _stickmanManager;
        [SerializeField] private HandCursor _cursor;
        [SerializeField] private HandButton[] _buttons;
        [SerializeField] private PointCloud _pointCloud;

        [Range(0f, 1f)] public float depth = 0;
        [Range(0f, 1f)] public float range = 0;

        private KinectSensor _sensor;

        private void Start()
        {
            _sensor = KinectSensor.Create(_configuration);

            if (_sensor == null)
            {
                Debug.LogError("Sensor not connected!");
                return;
            }

            _sensor.Open();
        }

        private void Update()
        {
            if (_sensor == null || !_sensor.IsOpen) return;

            Frame frame = _sensor.Update();

            if (frame != null)
            {
                if (frame.ColorFrameSource != null)
                {
                    var pointCloudDepth = frame.DepthFrameSource?.PointCloud;
                    var pointCloudColor = frame.ColorFrameSource?.PointCloud;
                    var bodies = frame.BodyFrameSource?.Bodies;

                    _pointCloud.Load(pointCloudColor, pointCloudDepth);
                }

                if (frame.BodyFrameSource != null)
                {
                    _stickmanManager.Load(frame.BodyFrameSource.Bodies);

                    Body body = frame.BodyFrameSource.Bodies?.Closest();

                    //_cursor.Load(body, _image);

                    foreach (HandButton button in _buttons)
                    {
                        //button.Load(_cursor);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            _sensor?.Close();
        }

        public void Button1_Click()
        {
            Debug.Log("Clicked Button 1");
        }

        public void Button2_Click()
        {
            Debug.Log("Clicked Button 2");
        }
    }
}
