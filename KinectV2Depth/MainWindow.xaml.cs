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
                //depthStride = (int)(depthFrameDescription.Width * depthFrameDescription.BytesPerPixel);
                depthStride = (int)(depthFrameDescription.Width * 4);
                //depthPoint = new Point(depthFrameDescription.Width / 2, depthFrameDescription.Height / 2);

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

                //Bitmap dBmps = new Bitmap(512, 424, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                //Graphics g = Graphics.FromImage(dBmps);
                //g.DrawImage(depthBitmap, 0, 0, dBmps.Width, dBmps.Height);

                depthImage.WritePixels(depthRect, depthRGBA, depthStride, 0);
                //g.Dispose();
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
            // 0 - 255
            for (i = 0; i < 256; i++)
            {
                if (i == 0)
                {
                    depthR[i] = depthG[i] = depthB[i] = 255;
                }
                else
                {
                    depthR[i] = (byte)(255 - i);
                    depthG[i] = (byte)i;
                    depthB[i] = 0;
                }
            }
            // 256 - 511
            for (i = 0; i < 256; i++)
            {
                depthR[i + 256] = 0;
                depthG[i + 256] = (byte)(255 - i);
                depthB[i + 256] = (byte)i;
            }
            // 512 - 767
            for (i = 0; i < 256; i++)
            {
                depthR[i + 512] = (byte)i;
                depthG[i + 512] = 0;
                depthB[i + 512] = (byte)(255 - i);
            }

            // 768 - 1023
            for (i = 0; i < 256; i++)
            {
                depthR[i + 768] = (byte)(255 - i);
                depthG[i + 768] = (byte)i;
                depthB[i + 768] = 0;
            }
            // 1024 - 1279
            for (i = 0; i < 256; i++)
            {
                depthR[i + 1024] = 0;
                depthG[i + 1024] = (byte)(255 - i);
                depthB[i + 1024] = (byte)i;
            }
            // 1280 - 1535
            for (i = 0; i < 256; i++)
            {
                depthR[i + 1280] = (byte)i;
                depthG[i + 1280] = 0;
                depthB[i + 1280] = (byte)(255 - i);
            }

            // 1536 - 1791
            for (i = 0; i < 256; i++)
            {
                depthR[i + 1536] = (byte)(255 - i);
                depthG[i + 1536] = (byte)i;
                depthB[i + 1536] = 0;
            }
            // 1792 - 2047
            for (i = 0; i < 256; i++)
            {
                depthR[i + 1792] = 0;
                depthG[i + 1792] = (byte)(255 - i);
                depthB[i + 1792] = (byte)i;
            }
            // 2048 - 2303
            for (i = 0; i < 256; i++)
            {
                depthR[i + 2048] = (byte)i;
                depthG[i + 2048] = 0;
                depthB[i + 2048] = (byte)(255 - i);
            }

            // 2304 - 2559
            for (i = 0; i < 256; i++)
            {
                depthR[i + 2304] = (byte)(255 - i);
                depthG[i + 2304] = (byte)i;
                depthB[i + 2304] = 0;
            }
            // 2560 - 2815
            for (i = 0; i < 256; i++)
            {
                depthR[i + 2560] = 0;
                depthG[i + 2560] = (byte)(255 - i);
                depthB[i + 2560] = (byte)i;
            }
            // 2816 - 3071
            for (i = 0; i < 256; i++)
            {
                depthR[i + 2816] = (byte)i;
                depthG[i + 2816] = 0;
                depthB[i + 2816] = (byte)(255 - i);
            }

            // 3072 - 3327
            for (i = 0; i < 256; i++)
            {
                depthR[i + 3072] = (byte)(255 - i);
                depthG[i + 3072] = (byte)i;
                depthB[i + 3072] = 0;
            }
            // 3328 - 3583
            for (i = 0; i < 256; i++)
            {
                depthR[i + 3328] = 0;
                depthG[i + 3328] = (byte)(255 - i);
                depthB[i + 3328] = (byte)i;
            }
            // 3584 - 3839
            for (i = 0; i < 256; i++)
            {
                depthR[i + 3584] = (byte)i;
                depthG[i + 3584] = 0;
                depthB[i + 3584] = (byte)(255 - i);
            }

            // 3840 - 4095
            for (i = 0; i < 256; i++)
            {
                depthR[i + 3840] = (byte)(255 - i);
                depthG[i + 3840] = (byte)i;
                depthB[i + 3840] = 0;
            }


            

            //System.Windows.Shapes.Rectangle horizontalFillRectangle = new System.Windows.Shapes.Rectangle();
            //horizontalFillRectangle.Width = 200;
            //horizontalFillRectangle.Height = 100;

            // Create a horizontal linear gradient with four stops.   
            /*
            LinearGradientBrush myHorizontalGradient =
                new LinearGradientBrush();
            myHorizontalGradient.StartPoint = new System.Windows.Point(0, 0.5);
            myHorizontalGradient.EndPoint = new System.Windows.Point(1, 0.5);
            myHorizontalGradient.GradientStops.Add(
                new GradientStop(Colors.Yellow, 0.0));
            myHorizontalGradient.GradientStops.Add(
                new GradientStop(Colors.Red, 0.25));
            myHorizontalGradient.GradientStops.Add(
                new GradientStop(Colors.Blue, 0.75));
            myHorizontalGradient.GradientStops.Add(
                new GradientStop(Colors.LimeGreen, 1.0));

            // Use the brush to paint the rectangle.
            bar.Fill = myHorizontalGradient;
            */
            /*
            var bmp = new RenderTargetBitmap((int)bar.Width, (int)bar.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(bar);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (FileStream fs = File.Open("bar.png", FileMode.Create))
            {
                enc.Save(fs);
            }
             * */

            Bitmap myBitmap = new Bitmap("bar.png");
            for (i = 0; i < 8001; i++)
            {
                System.Drawing.Color pixelColor = myBitmap.GetPixel(i, 0);
                depthR[i] = pixelColor.R;
                depthG[i] = pixelColor.G;
                depthB[i] = pixelColor.B;
            }
            myBitmap.Dispose();
            depthR[0] = depthG[0] = depthB[0] = 255;
        }
    }
}
