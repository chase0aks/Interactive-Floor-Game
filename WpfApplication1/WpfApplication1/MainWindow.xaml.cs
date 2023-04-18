using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        BodyFrameReader bodyReader;
        ColorFrameReader colorReader;
        Body body;
        DepthFrameReader depthReader;
        Canvas canvas;
        Image depthImage;
        VisualBrush depthBrush;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;
            this.Closed += OnClosed;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize Kinect sensor
            sensor = KinectSensor.GetDefault();
            sensor.Open();

            // Initialize body reader
            bodyReader = sensor.BodyFrameSource.OpenReader();
            bodyReader.FrameArrived += OnBodyFrameArrived;

            // Initialize depth reader
            depthReader = sensor.DepthFrameSource.OpenReader();

            // Initialize color reader
            colorReader = sensor.ColorFrameSource.OpenReader();
            colorReader.FrameArrived += OnColorFrameArrived;

            // Create canvas to draw skeleton
            canvas = new Canvas();
            canvas.Width = 1920;
            canvas.Height = 1080;

            // Create visual brush to hold color image as background
            depthBrush = new VisualBrush();
            depthImage = new Image();
            depthImage.Width = 1920;
            depthImage.Height = 1080;
            depthBrush.Visual = depthImage;
            canvas.Background = depthBrush;

            Content = canvas;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            // Stop body reader, depth reader, color reader, and close Kinect sensor when application closes
            bodyReader.Dispose();
            depthReader.Dispose();
            colorReader.Dispose();
            sensor.Close();
        }


        void OnBodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    Body[] bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    foreach (Body body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            this.body = body;
                            DrawBodyJoints(body);
                        }
                    }
                }
            }
        }


        void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    // Create color bitmap
                    int width = colorFrame.FrameDescription.Width;
                    int height = colorFrame.FrameDescription.Height;
                    byte[] colorData = new byte[width * height * 4];
                    colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Bgra);

                    // Set color bitmap as the source of the depthImage
                    BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, colorData, width * 4);
                    depthImage.Source = bitmap;

                    // Display bounding box adjuster
                    Rectangle rect = new Rectangle();
                    rect.Stroke = new SolidColorBrush(Colors.Green);
                    rect.StrokeThickness = 5;
                    rect.Width = 200;
                    rect.Height = 200;
                    Canvas.SetLeft(rect, 0);
                    Canvas.SetTop(rect, 0);
                    canvas.Children.Add(rect);

                    // Update bounding box position based on user input
                    canvas.MouseMove += (s, ev) =>
                    {
                        if (ev.LeftButton == MouseButtonState.Pressed && ev.OriginalSource == rect)
                        {
                            double x = ev.GetPosition(canvas).X - rect.Width / 2;
                            double y = ev.GetPosition(canvas).Y - rect.Height / 2;

                            Canvas.SetLeft(rect, x);
                            Canvas.SetTop(rect, y);
                        }
                    };

                    // Check if body is detected
                    if (body != null)
                    {
                        int left = (int)((body.Joints[JointType.SpineBase].Position.X * depthImage.ActualWidth / 2) + depthImage.ActualWidth / 2);
                        int top = (int)(-body.Joints[JointType.SpineBase].Position.Y * depthImage.ActualHeight / 2 + depthImage.ActualHeight / 2);
                        int boundingboxwidth = (int)(Math.Abs(body.Joints[JointType.SpineShoulder].Position.X - body.Joints[JointType.SpineBase].Position.X) * depthImage.ActualWidth / 2);
                        int boundingboxheight = (int)(Math.Abs(body.Joints[JointType.Head].Position.Y - body.Joints[JointType.SpineBase].Position.Y) * depthImage.ActualHeight / 2);

                        // Check if body is inside bounding box
                        if (left > Canvas.GetLeft(rect) && left < Canvas.GetLeft(rect) + rect.Width &&
                            top > Canvas.GetTop(rect) && top < Canvas.GetTop(rect) + rect.Height)
                        {
                            DrawBodyJoints(body);
                        }
                    }
                }
            }
        }



        void DrawBodyJoints(Body body)
        {
            // Draw joints
            foreach (Joint joint in body.Joints.Values)
            {
                Point point = new Point(joint.Position.X * canvas.Width / 2 + canvas.Width / 2,
                                        -joint.Position.Y * canvas.Height / 2 + canvas.Height / 2);
                Ellipse ellipse = new Ellipse();
                ellipse.Fill = new SolidColorBrush(Colors.Red);
                ellipse.Width = 10;
                ellipse.Height = 10;
                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
                canvas.Children.Add(ellipse);
            }

            // Draw bones
            DrawBone(body.Joints[JointType.Head], body.Joints[JointType.Neck]);
            DrawBone(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder]);
            DrawBone(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft]);
            DrawBone(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight]);
            DrawBone(body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid]);
            DrawBone(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft]);
            DrawBone(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight]);
            DrawBone(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]);
            DrawBone(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]);
            DrawBone(body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft]);
            DrawBone(body.Joints[JointType.WristRight], body.Joints[JointType.HandRight]);
            DrawBone(body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight]);
            DrawBone(body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft]);
            DrawBone(body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft]);
            DrawBone(body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight]);
            DrawBone(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase]);
            DrawBone(body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft]);
            DrawBone(body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight]);
            DrawBone(body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
            DrawBone(body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
            DrawBone(body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
            DrawBone(body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
            DrawBone(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft]);
            DrawBone(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight]);
        }
        void DrawBone(Joint joint1, Joint joint2)
        {
            Point point1 = new Point(joint1.Position.X * canvas.Width / 2 + canvas.Width / 2,
                                      -joint1.Position.Y * canvas.Height / 2 + canvas.Height / 2);
            Point point2 = new Point(joint2.Position.X * canvas.Width / 2 + canvas.Width / 2,
                                      -joint2.Position.Y * canvas.Height / 2 + canvas.Height / 2);
            Line line = new Line();
            line.Stroke = new SolidColorBrush(Colors.Green);
            line.StrokeThickness = 6;
            line.X1 = point1.X;
            line.X2 = point2.X;
            line.Y1 = point1.Y;
            line.Y2 = point2.Y;
            canvas.Children.Add(line);
        }
    }
}