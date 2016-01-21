//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Media.Media3D;

    public partial class MainWindow : Window
    {
        private KinectSensor sensor;       
        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;
        public static Vector3D O, X, Y, Z;
        private SolidColorBrush[] ColorBrush;
        private WriteableBitmap colorBitmapC;
        private byte[] colorPixelsC;
        private DepthImagePixel[] depthPixels;
        private WriteableBitmap colorBitmapD;
        private byte[] colorPixelsD;
        private DepthImagePixel[] dep4background;
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        double[] YUV;
        private SkeletonPoint[] ColorInSkel;
        private int MouseY;
        private int MouseX;
        private bool NT=false;
        double yy = 0, uu = 0, vv = 0, curColor = 100,range=20;
        double uur = 110, vvr = 200;
        double uub = 170, vvb = 90;
        double uuy = 40, vvy = 150;
        double uug = 135, vvg = 100;
        Vector3D totalr, totaly, totalg, totalb,cenr,ceny,cenb,ceng;
        double Countr = 0, Countb = 0, County = 0, Countg = 0;
        Boolean flagy = false, flagr = false, flagb = false, flagg = false;
        System.Windows.Point thisredpoint, thisbluepoint, thisyellowpoint, thisgreenpoint,
                     centerr = new System.Windows.Point(320, 240),
                     centerb = new System.Windows.Point(320, 240),
                     centery = new System.Windows.Point(320, 240),
                     centerg = new System.Windows.Point(320, 240);

        static class NewTransfer
        {
            public static Vector3D origin = new Vector3D();
            public static Vector3D xais = new Vector3D();
            public static Vector3D yais = new Vector3D();
            public static Vector3D zais = new Vector3D();

            public static Matrix3D Pmatrix;
            public static Matrix3D Fmatrix;
            public static Matrix3D Wmatrix;
            // |x A B D| -1      |30*x.x 30*y.x 30*z.x o.x+x.x+y.x+z.x|
            // |A y C E|         |30*x.y 30*y.y 30*z.y o.y+x.y+y.y+z.y|
            // |B C z F|    *    |30*x.z 30*y.z 30*z.z o.z+x.z+y.z+z.z|
            // |D E F 4|         |30     30     30     4              |
            public static void CalcP()
            {
                double x, A, B, C, y, D, E, F, z;
                x = xais.X * xais.X + yais.X * yais.X + zais.X * zais.X + origin.X * origin.X;

                y = xais.Y * xais.Y + yais.Y * yais.Y + zais.Y * zais.Y + origin.Y * origin.Y;

                z = xais.Z * xais.Z + yais.Z * yais.Z + zais.Z * zais.Z + origin.Z * origin.Z;

                A = origin.X * origin.Y + xais.X * xais.Y + yais.X * yais.Y + zais.X * zais.Y;

                B = origin.X * origin.Z + xais.X * xais.Z + yais.X * yais.Z + zais.X * zais.Z;

                C = origin.Y * origin.Z + xais.Y * xais.Z + yais.Y * yais.Z + zais.Y * zais.Z;

                D = origin.X + xais.X + yais.X + zais.X;
                E = yais.Y + xais.Y + zais.Y + origin.Y;
                F = yais.Z + xais.Z + zais.Z + origin.Z;

                NewTransfer.Pmatrix = new Matrix3D(x, A, B, D,
                                                   A, y, C, E,
                                                   B, C, z, F,
                                                   D, E, F, 4);
                NewTransfer.Pmatrix.Invert();

            }
            public static void CalcF()
            {
                double A, B, C;
                A = origin.X + xais.X + yais.X + zais.X;
                B = yais.Y + xais.Y + zais.Y + origin.Y;
                C = yais.Z + xais.Z + zais.Z + origin.Z;
                NewTransfer.Fmatrix = new Matrix3D(30 * xais.X, 30 * yais.X, 30 * zais.X, A,
                                                   30 * xais.Y, 30 * yais.Y, 30 * zais.Y, B,
                                                   30 * xais.Z, 30 * yais.Z, 30 * zais.Z, C,
                                                   30, 30, 30, 4);
            }
            public static void CalcW()
            {
                NewTransfer.Wmatrix = Matrix3D.Multiply(Pmatrix, Fmatrix);
            }
            public static Vector3D CalcNp(Vector3D point)
            {
                Vector3D np = new Vector3D();
                np.X = NewTransfer.Wmatrix.M11 * point.X + NewTransfer.Wmatrix.M21 * point.Z + NewTransfer.Wmatrix.M31 * point.Y + NewTransfer.Wmatrix.OffsetX * 1;
                np.Y = NewTransfer.Wmatrix.M12 * point.X + NewTransfer.Wmatrix.M22 * point.Z + NewTransfer.Wmatrix.M32 * point.Y + NewTransfer.Wmatrix.OffsetY * 1;
                np.Z = NewTransfer.Wmatrix.M13 * point.X + NewTransfer.Wmatrix.M23 * point.Z + NewTransfer.Wmatrix.M33 * point.Y + NewTransfer.Wmatrix.OffsetZ * 1;
                return np;
            }            
        }
        private static void Set()
        {
            NewTransfer.origin = O;
            NewTransfer.xais = X;
            NewTransfer.yais = Y;
            NewTransfer.zais = Z;
            NewTransfer.CalcP();
            NewTransfer.CalcF();
            NewTransfer.CalcW();

        }
        public MainWindow()
        {
            InitializeComponent();
        }
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                totalb = new Vector3D();
                totalg = new Vector3D();
                totalr = new Vector3D();
                totaly = new Vector3D();

                cenr = new Vector3D();
                cenb = new Vector3D();
                ceng = new Vector3D();
                ceny = new Vector3D();
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.Image.Source = this.colorBitmap;
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;
                this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

      
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

      
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyPixelDataTo(this.colorPixels);
                    ColorInSkel = ColorToSkel(640, 480);
                }
            }
        }

        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MouseX = (int)e.GetPosition(Image).X;
            MouseY = (int)e.GetPosition(Image).Y;
            if (NT == true)
            {
                Vector3D pp = new Vector3D(ColorInSkel[MouseX + MouseY * 640].X, ColorInSkel[MouseX + MouseY * 640].Y, ColorInSkel[MouseX + MouseY * 640].Z);
                pp = NewTransfer.CalcNp(pp);
                pp = round(pp);

                textBox.Text += "X= "+pp.X + ",Y= " + pp.Y + ", Z= " + pp.Z + "\n";
            }
            else
            {
                textBox.Text += Math.Round(ColorInSkel[MouseX + MouseY * 640].X , 3) + ",";
                textBox.Text += Math.Round(ColorInSkel[MouseX + MouseY * 640].Y , 3) + ",";
                textBox.Text += Math.Round(ColorInSkel[MouseX + MouseY * 640].Z , 3) + "\n";
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Z.X = round(ColorInSkel[MouseX + MouseY * 640].X);
            Z.Y = round(ColorInSkel[MouseX + MouseY * 640].Z);
            Z.Z = round(ColorInSkel[MouseX + MouseY * 640].Y);
            textBox.Text += "Z-axis OK!\n";
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            O.X = round(ColorInSkel[MouseX + MouseY * 640].X);
            O.Y = round(ColorInSkel[MouseX + MouseY * 640].Z);
            O.Z = round(ColorInSkel[MouseX + MouseY * 640].Y);
            textBox.Text += "Origin OK!\n";
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            X.X = round(ColorInSkel[MouseX + MouseY * 640].X);
            X.Y = round(ColorInSkel[MouseX + MouseY * 640].Z);
            X.Z = round(ColorInSkel[MouseX + MouseY * 640].Y);
            textBox.Text += "X-axis OK!\n";
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            Y.X = round(ColorInSkel[MouseX + MouseY * 640].X);
            Y.Y = round(ColorInSkel[MouseX + MouseY * 640].Z);
            Y.Z = round(ColorInSkel[MouseX + MouseY * 640].Y);
            textBox.Text += "Y-axis OK!\n";
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            Set();
            NewTransfer.Wmatrix.M11 = round(NewTransfer.Wmatrix.M11);
            NewTransfer.Wmatrix.M12 = round(NewTransfer.Wmatrix.M12);
            NewTransfer.Wmatrix.M13 = round(NewTransfer.Wmatrix.M13);
            NewTransfer.Wmatrix.M14 = round(NewTransfer.Wmatrix.M14);

            NewTransfer.Wmatrix.M21 = round(NewTransfer.Wmatrix.M21);
            NewTransfer.Wmatrix.M22 = round(NewTransfer.Wmatrix.M22);
            NewTransfer.Wmatrix.M23 = round(NewTransfer.Wmatrix.M23);
            NewTransfer.Wmatrix.M24 = round(NewTransfer.Wmatrix.M24);

            NewTransfer.Wmatrix.M31 = round(NewTransfer.Wmatrix.M31);
            NewTransfer.Wmatrix.M32 = round(NewTransfer.Wmatrix.M32);
            NewTransfer.Wmatrix.M33 = round(NewTransfer.Wmatrix.M33);
            NewTransfer.Wmatrix.M34 = round(NewTransfer.Wmatrix.M34);


            NewTransfer.Wmatrix.OffsetX = round(NewTransfer.Wmatrix.OffsetX);
            NewTransfer.Wmatrix.OffsetY = round(NewTransfer.Wmatrix.OffsetY);
            NewTransfer.Wmatrix.OffsetZ = round(NewTransfer.Wmatrix.OffsetZ);
            NewTransfer.Wmatrix.M44 = round(NewTransfer.Wmatrix.M44);

            textBox.Text += "Pmatrix-inverse \n";
            textBox.Text += "|" + Math.Round(NewTransfer.Pmatrix.M11,3) + "\t" + Math.Round(NewTransfer.Pmatrix.M12, 3) + "\t" + Math.Round(NewTransfer.Pmatrix.M13 ,3)+"\t" + Math.Round(NewTransfer.Pmatrix.M14 ,3)+ "|\n";
            textBox.Text += "|" + Math.Round(NewTransfer.Pmatrix.M21, 3) + "\t" + Math.Round(NewTransfer.Pmatrix.M22, 3) + "\t" + Math.Round(NewTransfer.Pmatrix.M23 ,3)+"\t" + Math.Round(NewTransfer.Pmatrix.M24, 3) + "|\n";
            textBox.Text += "|" + Math.Round(NewTransfer.Pmatrix.M31, 3) + "\t" + Math.Round(NewTransfer.Pmatrix.M32, 3) + "\t" + Math.Round(NewTransfer.Pmatrix.M33 ,3)+"\t" + Math.Round(NewTransfer.Pmatrix.M34, 3) + "|\n";
            textBox.Text += "|" + Math.Round(NewTransfer.Pmatrix.OffsetX, 3) + "\t" + Math.Round(NewTransfer.Pmatrix.OffsetY, 3) + "\t" + Math.Round(NewTransfer.Pmatrix.OffsetZ ,3)+"\t" + Math.Round(NewTransfer.Wmatrix.M44, 3) + "|\n";


            textBox.Text += "Wmatrix\n";
            textBox.Text += "|" + NewTransfer.Wmatrix.M11 + "\t" + NewTransfer.Wmatrix.M12 + "\t" + NewTransfer.Wmatrix.M13 + "\t" + NewTransfer.Wmatrix.M14 + "|\n";
            textBox.Text += "|" + NewTransfer.Wmatrix.M21 + "\t" + NewTransfer.Wmatrix.M22 + "\t" + NewTransfer.Wmatrix.M23 + "\t" + NewTransfer.Wmatrix.M24 + "|\n";
            textBox.Text += "|" + NewTransfer.Wmatrix.M31 + "\t" + NewTransfer.Wmatrix.M32 + "\t" + NewTransfer.Wmatrix.M33 + "\t" + NewTransfer.Wmatrix.M34 + "|\n";
            textBox.Text += "|" + NewTransfer.Wmatrix.OffsetX + "\t" + NewTransfer.Wmatrix.OffsetY + "\t" + NewTransfer.Wmatrix.OffsetZ + "\t" + NewTransfer.Wmatrix.M44 + "|\n";
            NT = true;
        }
        public double round(double a)
        {
           return  Math.Round(a, 3);
        }
        public Vector3D round(Vector3D a)
        {
            Vector3D ans = new Vector3D();
            ans.X = Math.Round(a.X, 3);
            ans.Y = Math.Round(a.Y, 3);
            ans.Z = Math.Round(a.Z, 3);
            return ans;
        }
        public Vector3D round(SkeletonPoint a)
        {
            Vector3D ans = new Vector3D();
            ans.X = Math.Round(a.X, 3);
            ans.Y = Math.Round(a.Y, 3);
            ans.Z = Math.Round(a.Z, 3);
            return ans;
        }
        private void Image_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MouseX = (int)e.GetPosition(Image).X;
            MouseY = (int)e.GetPosition(Image).Y;
            int i = (MouseX + MouseY * 640) * 4;
            textBox.Text += Math.Round(ColorInSkel[MouseX + MouseY * 640].X, 3) + ",";
            textBox.Text += Math.Round(ColorInSkel[MouseX + MouseY * 640].Y, 3) + ",";
            textBox.Text += Math.Round(ColorInSkel[MouseX + MouseY * 640].Z, 3) + "\n";
            yy = 0.299 * colorPixels[i + 2] + 0.587 * colorPixels[i + 1] + 0.114 * colorPixels[i];
            uu = -0.169 * colorPixels[i + 2] - 0.331 * colorPixels[i + 1] + 0.5 * colorPixels[i] + 128;
            vv = 0.5 * colorPixels[i + 2] - 0.419 * colorPixels[i + 1] - 0.081 * colorPixels[i] + 128;
            textBox.Text += "(" + yy + "," + uu + "," + vv + ")\n";
            for (int k = MouseX-10 ; k < MouseX + 10; k ++)
            {
                for (int j = MouseY-10;j<MouseY+10 ; j ++)
                {
                    #region red ball
                    yy = 0.299 * colorPixels[(k + j * 640) * 4 + 2] + 0.587 * colorPixels[(k + j * 640) * 4 + 1] + 0.114 * colorPixels[(k + j * 640) * 4];
                    uu = -0.169 * colorPixels[(k + j * 640) * 4 + 2] - 0.331 * colorPixels[(k + j * 640) * 4 + 1] + 0.5 * colorPixels[(k + j * 640) * 4] + 128;
                    vv = 0.5 * colorPixels[(k + j * 640) * 4 + 2] - 0.419 * colorPixels[(k + j * 640) * 4 + 1] - 0.081 * colorPixels[(k + j * 640) * 4] + 128;
                    if (uu > uur - range && uu < uur + range && vv > vvr - range && vv < vvr + range)
                    {
                        thisredpoint.X = k;
                        thisredpoint.Y = j;
                        //  if (Math.Sqrt((thisredpoint.X - centerr.X) * (thisredpoint.X - centerr.X) + (thisredpoint.Y - centerr.Y) * (thisredpoint.Y - centerr.Y)) < 30)  //(x-a)^2 + (y-b)^2 > k
                        //  {
                        //  colorPixels[i] = 0;
                        //  colorPixels[i + 1] = 0;
                        //  colorPixels[i + 2] = 255;    //640*480=307200*4=1228800  

                        totalr.X += ColorInSkel[k + j * 640].X;
                        totalr.Y += ColorInSkel[k + j * 640].Z;
                        totalr.Z += ColorInSkel[k + j * 640].Y;
                        Countr += 1;
                        // }
                    }
                    #endregion
                    #region blue all
                    if (uu > uub - range && uu < uub + range && vv > vvb - range && vv < vvb + range)
                    {
                        thisredpoint.X = k;
                        thisredpoint.Y = j;
                        //  if (Math.Sqrt((thisredpoint.X - centerb.X) * (thisredpoint.X - centerb.X) + (thisredpoint.Y - centerb.Y) * (thisredpoint.Y - centerb.Y)) < 30)  //(x-a)^2 + (y-b)^2 > k
                        // {
                        //     colorPixels[i] = 0;
                        //    colorPixels[i + 1] = 0;
                        //   colorPixels[i + 2] = 255;    //640*480=307200*4=1228800  

                        totalb.X += ColorInSkel[k + j * 640].X;
                        totalb.Y += ColorInSkel[k + j * 640].Z;
                        totalb.Z += ColorInSkel[k + j * 640].Y;
                        Countb += 1;
                        // }
                    }
                    #endregion
                    #region green all
                    if (uu > uug - range && uu < uug + range && vv > vvg - range && vv < vvg + range)
                    {
                        thisredpoint.X = k;
                        thisredpoint.Y = j;
                        //  if (Math.Sqrt((thisredpoint.X - centerg.X) * (thisredpoint.X - centerg.X) + (thisredpoint.Y - centerg.Y) * (thisredpoint.Y - centerg.Y)) < 30)  //(x-a)^2 + (y-b)^2 > k
                        // {
                        //     colorPixels[i] = 0;
                        //    colorPixels[i + 1] = 0;
                        //   colorPixels[i + 2] = 255;    //640*480=307200*4=1228800  

                        totalg.X += ColorInSkel[k + j * 640].X;
                        totalg.Y += ColorInSkel[k + j * 640].Z;
                        totalg.Z += ColorInSkel[k + j * 640].Y;
                        Countg += 1;
                        // }
                    }
                    #endregion
                    #region yellow all
                    if (uu > uuy - range && uu < uuy + range && vv > vvy - range && vv < vvy + range)
                    {
                        thisredpoint.X = k;
                        thisredpoint.Y = j;
                        //  if (Math.Sqrt((thisredpoint.X - centery.X) * (thisredpoint.X - centery.X) + (thisredpoint.Y - centery.Y) * (thisredpoint.Y - centery.Y)) < 30)  //(x-a)^2 + (y-b)^2 > k
                        // {
                        //     colorPixels[i] = 0;
                        //    colorPixels[i + 1] = 0;
                        //   colorPixels[i + 2] = 255;    //640*480=307200*4=1228800  

                        totaly.X += ColorInSkel[k + j * 640].X;
                        totaly.Y += ColorInSkel[k + j * 640].Z;
                        totaly.Z += ColorInSkel[k + j * 640].Y;
                        County += 1;
                        // }
                    }
                    #endregion
                }
            }
            if(County>20&&flagy==false)
            {
                ceny = totaly / County;
                ceny = round(ceny);
                flagy = true;
                O = ceny;
                textBox.Text += "total pixels: " + County;
                textBox.Text+="Origin:( "+ceny.X+"," + ceny.Y + "," + ceny.Z+")\n";
            }
            if (Countr > 20 && flagr == false)
            {
                cenr = totalr / Countr;
                cenr = round(cenr);
                flagr = true;
                textBox.Text += "total pixels: " + Countr;
                textBox.Text += "Zaxis:( " + cenr.X + "," + cenr.Y + "," + cenr.Z + ")\n";
                Z = cenr;
            }
            if (Countb > 20 && flagb == false)
            {
                cenb = totalb / Countb;
                cenb = round(cenb);
                flagb = true;
                textBox.Text += "total pixels: " + Countb;
                textBox.Text += "Yaxis:( " + cenb.X + "," + cenb.Y + "," + cenb.Z + ")\n";
                Y = cenb;
            }
            if (Countg > 20 && flagg == false)
            {
                ceng = totalg / Countg;
                ceng = round(ceng);
                flagg = true;
                textBox.Text += "total pixels: " + Countg;
                textBox.Text += "Xaxis:( " + ceng.X + "," + ceng.Y + "," + ceng.Z + ")\n";
                X = ceng;
            }
        }
            

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            NT = false;
            flagg = false;
            flagb = false;
            flagr = false;
            flagy = false;
            Countb = 0;
            Countr = 0;
            Countg = 0;
            County = 0;
            totalb = new Vector3D();
            totalg = new Vector3D();
            totalr = new Vector3D();
            totaly = new Vector3D();
        }

        private void textBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //textBox.SelectionStart = textBox.Text.Length;
            textBox.ScrollToEnd();
        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
        
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
        }
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    ///////////這裡是中心深度相減追蹤法
                    //int CenterDepthIndex = MouseX + 640 * MouseY;
                    //int range = 50;
                    //short TheMinDepth = 0;
                    //int MinDepthX = 0, MinDepthY = 0;
                    //for (int xaxis = MouseX - range; xaxis < MouseX + range; xaxis++)
                    //{
                    //    for (int yaxix = MouseY - range; yaxix < MouseY + range; yaxix++)
                    //    {
                    //        int ThisDepthIndex = xaxis + 640 * yaxix;
                    //        if (ThisDepthIndex > 0 && ThisDepthIndex < 307200)
                    //            if (Math.Abs(depthPixels[CenterDepthIndex].Depth - depthPixels[ThisDepthIndex].Depth) < 10
                    //             && Math.Abs(depthPixels[ThisDepthIndex].Depth - dep4background[ThisDepthIndex].Depth) > 100
                    //             && depthPixels[ThisDepthIndex].IsKnownDepth)
                    //            {
                    //                //colorPixelsC[4 * ThisDepthIndex] = 255;
                    //                //colorPixelsC[4 * ThisDepthIndex + 1] = 255;
                    //                //colorPixelsC[4 * ThisDepthIndex + 2] = 255;

                    //                thisdepthx.Add(xaxis);
                    //                thisdepthy.Add(yaxix);
                    //            }
                    //    }
                    //}

                    //if (thisdepthx.Count != 0 && thisdepthy.Count != 0)
                    //{

                    //    MouseX = (int)thisdepthx.Average();
                    //    MouseY = (int)thisdepthy.Average();

                    //    //MouseX = MinDepthX;
                    //    //MouseY = MinDepthY;

                    //    thisdepthx.Clear();
                    //    thisdepthy.Clear();
                    //    //這裡是洪水演算法
                    //    //this.boolarray = new bool[this.sensor.DepthStream.FramePixelDataLength];
                    //    // int i = MouseX + 640 * MouseY;
                    //    //flood(MouseX, MouseY ,depthPixels[i].Depth);
                    //}

                   // this.sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depthPixels, ColorImageFormat.RgbResolution640x480Fps30, colorBitmap);
                    this.colorBitmap.WritePixels(
                           new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                           this.colorPixels,
                           this.colorBitmap.PixelWidth * sizeof(int),
                           0);
                }
            }
        }
        public SkeletonPoint[] ColorToSkel(int W, int H)
        {
            SkeletonPoint[] SKK = new SkeletonPoint[W * H];
            try {
                this.sensor.CoordinateMapper.MapColorFrameToSkeletonFrame(ColorImageFormat.RgbResolution640x480Fps30, DepthImageFormat.Resolution640x480Fps30, this.depthPixels, SKK);
                
            }
            catch 
            { }
                return SKK;
           
        }
        
    }
}