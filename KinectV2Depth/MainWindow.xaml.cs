using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace KinectV2Depth
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor kinect;

        private DepthFrameReader depthFrameReader;
        private ushort[] depthBuffer;

        WriteableBitmap depthImage;
        Int32Rect depthRect;
        int depthStride;

        private byte[] depthR = new byte[8001];
        private byte[] depthG = new byte[8001];
        private byte[] depthB = new byte[8001];
        private BitmapData depthBitmapData;
        private Bitmap depthBitmap = new Bitmap(512, 424, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        System.Drawing.Rectangle dRect = new System.Drawing.Rectangle(0, 0, 512, 424);
        private byte[] depthRGBA = new byte[512 * 424 * 4];



        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                kinect = KinectSensor.GetDefault();
                if (kinect == null)
                {
                    throw new Exception("Cannot open Kinect v2 sensor.");
                }

                var depthFrameDescription = kinect.DepthFrameSource.FrameDescription;
                depthImage = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96, 96, PixelFormats.Bgr32, null);
                depthBuffer = new ushort[depthFrameDescription.LengthInPixels];
                depthRect = new Int32Rect(0, 0, depthFrameDescription.Width, depthFrameDescription.Height);
                depthStride = (int)(depthFrameDescription.Width * 4);

                ImageDepth.Source = depthImage;

                // Open depth reader.
                depthFrameReader = kinect.DepthFrameSource.OpenReader();
                depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;

                kinect.Open();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }

            MakeDepthColorPallette();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(depthFrameReader != null)
            {
                depthFrameReader.Dispose();
                depthFrameReader = null;
            }

            if(kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }

        void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (var depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                // Acquire depth frame data.
                depthFrame.CopyFrameDataToArray(depthBuffer);

                
                // Convert depth data into Image.
                ConvertDepthColor();

                depthBitmapData = depthBitmap.LockBits(dRect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                Marshal.Copy(depthRGBA, 0, depthBitmapData.Scan0, depthRGBA.Length);
                depthBitmap.UnlockBits(depthBitmapData);

                depthImage.WritePixels(depthRect, depthRGBA, depthStride, 0);
            }
        }

        void ConvertDepthColor()
        {
            int i, j = 0;
            int k;
            for (i = 0; i < depthBuffer.Length; i++)
            {
                k = depthBuffer[i];
                //k = k - 500;
                if ((k < 0) | (k > 8000)){ k = 0; }
                depthRGBA[j] = depthB[k];
                depthRGBA[j + 1] = depthG[k];
                depthRGBA[j + 2] = depthR[k];
                depthRGBA[j + 3] = 0;
                j += 4;
            }
        }

        void MakeDepthColorPallette()
        {
            int i;

            Bitmap colorBarBitmap = new Bitmap("..\\..\\bar.png");
            for (i = 0; i < 8001; i++)
            {
                System.Drawing.Color pixelColor = colorBarBitmap.GetPixel(i, 0);
                depthR[i] = pixelColor.R;
                depthG[i] = pixelColor.G;
                depthB[i] = pixelColor.B;
            }
            depthR[0] = depthG[0] = depthB[0] = 255;
        }
    }
}
