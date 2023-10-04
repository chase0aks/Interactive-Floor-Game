using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace KinectApp
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinectSensor;
        private ColorFrameReader colorFrameReader;
        private BodyFrameReader bodyFrameReader;
        private double screenWidth;
        private double screenHeight;

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

                screenWidth = SystemParameters.PrimaryScreenWidth;
                screenHeight = SystemParameters.PrimaryScreenHeight;

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

        private Point mousePosition;
        private Size boxSize;

        private void Box_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = e.GetPosition(null);
            boxSize = new Size(Box.ActualWidth, Box.ActualHeight);
            Box.CaptureMouse();
        }

        private void Box_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (Box.IsMouseCaptured)
            {
                Point currentPosition = e.GetPosition(null);
                double deltaX = currentPosition.X - mousePosition.X;
                double deltaY = currentPosition.Y - mousePosition.Y;
                double newWidth = Math.Max(boxSize.Width + deltaX, 0);
                double newHeight = Math.Max(boxSize.Height + deltaY, 0);
                Box.Width = newWidth;
                Box.Height = newHeight;
            }
        }

        private void Box_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Box.ReleaseMouseCapture();
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

                    bool isBodyTracked = false;
                    foreach (Body body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            isBodyTracked = true;
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

                    if (isBodyTracked)
                    {
                        Box.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Box.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void DrawJoint(Joint joint, Color color)
        {
            CoordinateMapper coordinateMapper = kinectSensor.CoordinateMapper;

            ColorSpacePoint colorSpacePoint = coordinateMapper.MapCameraPointToColorSpace(joint.Position);

            double x = colorSpacePoint.X;
            double y = colorSpacePoint.Y - 30;

            if (joint.JointType == JointType.HandRight)
            {
                // Set the mouse position
                double mouseY = screenHeight * y / ColorImage.ActualHeight;
                double mouseX = screenWidth * x / ColorImage.ActualWidth;
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)mouseX, (int)mouseY);
            }
            else
            {
                // Draw the joint as before
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
}
