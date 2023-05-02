using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;

namespace KinectApp
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinectSensor;
        private ColorFrameReader colorFrameReader;
        private BodyFrameReader bodyFrameReader;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensor = KinectSensor.GetDefault();

            if (kinectSensor != null)
            {
                kinectSensor.Open();

                colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();
                colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;

                bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (colorFrameReader != null)
            {
                colorFrameReader.Dispose();
                colorFrameReader = null;
            }

            if (bodyFrameReader != null)
            {
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }

        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    byte[] pixelData = new byte[colorFrame.FrameDescription.LengthInPixels * 4];
                    colorFrame.CopyConvertedFrameDataToArray(pixelData, ColorImageFormat.Bgra);

                    ColorImage.Source = BitmapSource.Create(colorFrame.FrameDescription.Width, colorFrame.FrameDescription.Height, 96, 96, PixelFormats.Bgra32, null, pixelData, colorFrame.FrameDescription.Width * 4);
                }
            }
        }

        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    SkeletonCanvas.Children.Clear();
                    Body[] bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    foreach (Body body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            foreach (Joint joint in body.Joints.Values)
                            {
                                if (joint.TrackingState == TrackingState.Tracked)
                                {
                                    JointType jointType = joint.JointType;

                                    if (jointType == JointType.HandLeft)
                                    {
                                        DrawJoint(joint, Colors.Red);
                                    }
                                    else if (jointType == JointType.HandRight)
                                    {
                                        DrawJoint(joint, Colors.Blue);
                                    }
                                    else
                                    {
                                        DrawJoint(joint, Colors.Green);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawJoint(Joint joint, Color color)
        {
            CoordinateMapper coordinateMapper = kinectSensor.CoordinateMapper;

            DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(joint.Position);

            double x = depthSpacePoint.X + 60; // Add 50 to shift the joint to the right
            double y = depthSpacePoint.Y + 10;

            Ellipse ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(color)
            };

            Canvas.SetLeft(ellipse, x - ellipse.Width / 2);
            Canvas.SetTop(ellipse, y - ellipse.Height / 2);

            SkeletonCanvas.Children.Add(ellipse);
        }

    }
}