using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectOperaFaceDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        bool isWindowsClosing = false; //窗口是否正在关闭中
        const int MaxSkeletonTrackingCount = 6; //最多同时可以跟踪的用户数
        Skeleton[] allSkeletons = new Skeleton[MaxSkeletonTrackingCount];
        int operaFaceIndex = 0; //川剧脸谱的编号

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            hideOperaFace(); //隐藏道具
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
        }


        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor oldKinect = (KinectSensor)e.OldValue;

            stopKinect(oldKinect);

            KinectSensor kinect = (KinectSensor)e.NewValue;

            if (kinect == null)
            {
                return;
            }

            kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            var tsp = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };
            kinect.SkeletonStream.Enable(tsp);
            //kinect.SkeletonStream.Enable();

            kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinect_AllFramesReady);


            try
            {
                //显示彩色图像摄像头
                kinectColorViewer1.Kinect = kinect;
                
                //启动
                kinect.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }

        void hideOperaFace()
        {
            leftEllipse.Visibility = Visibility.Hidden;
            rightEllipse.Visibility = Visibility.Hidden;
            headImage.Visibility = Visibility.Hidden;
        }

        void showOperaFace()
        {
            leftEllipse.Visibility = Visibility.Visible;
            rightEllipse.Visibility = Visibility.Visible;
            headImage.Visibility = Visibility.Visible; 
        }


        void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (isWindowsClosing)
            {
                return;
            }

            //仅获得第一个被骨骼跟踪的用户
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                hideOperaFace();
                return;
            }
            
            //成功骨骼跟踪后才显示道具
            if (first.TrackingState != SkeletonTrackingState.Tracked)
            {
                hideOperaFace(); 
                return;
            }
            else
            {
                showOperaFace();
            }
                        
            mappingSkeleton2CameraCoordinate(first, e); //坐标映射
            operaFaceMagic(first); //表演变脸
        }

        /// <summary>
        /// 判断两个SkeletonPoint点是否在同一个区域
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        bool isTwoSkeletonPointOverlapping(SkeletonPoint p1, SkeletonPoint p2)
        {
            bool isOverlapping = 
                (Math.Abs(p1.X - p2.X) <= 0.05) && (Math.Abs(p1.Y - p2.Y) <= 0.05);
            return isOverlapping;
        }

        /// <summary>
        /// 表演变脸: 循环显示三张不同川剧脸谱，中间抹去脸谱
        /// </summary>
        /// <param name="first"></param>
        void operaFaceMagic(Skeleton first)
        {
            if (isTwoSkeletonPointOverlapping(first.Joints[JointType.Head].Position, first.Joints[JointType.HandRight].Position) ||
                isTwoSkeletonPointOverlapping(first.Joints[JointType.Head].Position, first.Joints[JointType.HandLeft].Position))
            {

                operaFaceIndex++;
                operaFaceIndex %= 4;



                if (operaFaceIndex == 0)
                {
                    // 抹去川剧脸谱
                    headImage.Source = null;
                }
                else
                {
                    
                    string operaFace = "pack://application:,,,/images/face00" + operaFaceIndex + ".png";
                    headImage.Source = new BitmapImage(new Uri(operaFace));
                    
                }
            }
        }

        void mappingSkeleton2CameraCoordinate(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }
                
                //将骨骼坐标点直接映射到彩色图像坐标点

                ColorImagePoint headColorPoint =
                    kinectSensorChooser1.Kinect.MapSkeletonPointToColor(first.Joints[JointType.Head].Position, ColorImageFormat.RgbResolution640x480Fps30);
                
                ColorImagePoint leftColorPoint =
                    kinectSensorChooser1.Kinect.MapSkeletonPointToColor(first.Joints[JointType.HandLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);
                ColorImagePoint rightColorPoint =
                    kinectSensorChooser1.Kinect.MapSkeletonPointToColor(first.Joints[JointType.HandRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

                //ColorImagePoint shoulderPoint =
                //    kinectSensorChooser1.Kinect.MapSkeletonPointToColor(first.Joints[JointType.ShoulderCenter].Position, ColorImageFormat.RgbResolution640x480Fps30);
                //ColorImagePoint spinPoint =
                //    kinectSensorChooser1.Kinect.MapSkeletonPointToColor(first.Joints[JointType.Spine].Position, ColorImageFormat.RgbResolution640x480Fps30);


                //SkeletonPoint sholderPoint = first.Joints[JointType.ShoulderCenter].Position;
                //SkeletonPoint spinePoint = first.Joints[JointType.Spine].Position;
                //double angle = Math.Atan2((sholderPoint.Y - spinePoint.Y), (sholderPoint.Y - spinePoint.Y)) * 180 / Math.PI;
                //TransformGroup tg = new TransformGroup();
                //RotateTransform rt = new RotateTransform(angle);
                //tg.Children.Add(rt);

                //headImage.RenderTransformOrigin = new Point(0.5, 0.5);
                //headImage.RenderTransform = tg;

                ////将骨骼坐标点映射到深度图像坐标点
                //DepthImagePoint headDepthPoint =
                //    depth.MapFromSkeletonPoint(first.Joints[JointType.Head].Position);
                //DepthImagePoint leftDepthPoint =
                //    depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);
                //DepthImagePoint rightDepthPoint =
                //    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);
                ////深度图像坐标点映射到彩色图像坐标点
                //ColorImagePoint headColorPoint =
                //    depth.MapToColorImagePoint(headDepthPoint.X, headDepthPoint.Y,
                //    ColorImageFormat.RgbResolution640x480Fps30);
                //ColorImagePoint leftColorPoint =
                //    depth.MapToColorImagePoint(leftDepthPoint.X, leftDepthPoint.Y,
                //    ColorImageFormat.RgbResolution640x480Fps30);
                //ColorImagePoint rightColorPoint =
                //    depth.MapToColorImagePoint(rightDepthPoint.X, rightDepthPoint.Y,
                //    ColorImageFormat.RgbResolution640x480Fps30);

                
                //修正为质心位置
                //adjustCameraPosition(headImage, headColorPoint);
                //adjustCameraPosition(leftEllipse, leftColorPoint);
                //adjustCameraPosition(rightEllipse, rightColorPoint);

                adjustCameraPosition(headImage, scalePointCoordinate(headColorPoint));
                adjustCameraPosition(leftEllipse, scalePointCoordinate(leftColorPoint));
                adjustCameraPosition(rightEllipse, scalePointCoordinate(rightColorPoint));
            }
        }

        /// <summary>
        /// 根据显示分辨率与彩色图像帧的分辨率的比例，来调整显示坐标
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private ColorImagePoint scalePointCoordinate(ColorImagePoint p)
        {
            double xScaleRate = kinectColorViewer1.Width / 640;
            double yScaleRate = kinectColorViewer1.Height / 480;

            double x = (double)p.X;
            x *= xScaleRate;
            double y = (double)p.Y; 
            y *= yScaleRate;

            p.X = (int)x;
            p.Y = (int)y;

            return p;
        }

        private void adjustCameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //从对象的（left,top）修正为该对象的质心位置
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);
        }

        private void stopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //关闭音频流，如果当前已打开的话
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }
                }
            }
        }

        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }


                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //Linq语法，查找第一个被跟踪的骨骼
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;
            }
        }

        Skeleton GetClosetSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }
                
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //Linq语法，查找离Kinect最近的、被跟踪的骨骼
                Skeleton closestSkeleton = (from s in allSkeletons
                                            where s.TrackingState == SkeletonTrackingState.Tracked &&
                                                  s.Joints[JointType.Head].TrackingState == JointTrackingState.Tracked
                                            select s).OrderBy(s => s.Joints[JointType.Head].Position.Z)
                                    .FirstOrDefault();

                return closestSkeleton;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isWindowsClosing = true;
            stopKinect(kinectSensorChooser1.Kinect); 
        }
    }
}
