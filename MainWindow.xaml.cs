//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


namespace Microsoft.Samples.Kinect.InfraredBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Forms;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows.Threading;
    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Maximum value (as a float) that can be returned by the InfraredFrame
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;
        
        /// <summary>
        /// The value by which the infrared source data will be scaled
        /// </summary>
        private const float InfraredSourceScale = 0.75f;

        /// <summary>
        /// Smallest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// Largest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for infrared frames
        /// </summary>
        private InfraredFrameReader infraredFrameReader = null;

        /// <summary>
        /// Description (width, height, etc) of the infrared frame data
        /// </summary>
        private FrameDescription infraredFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap infraredBitmap = null;
        
        
        
        private DepthFrameReader depthFrameReader = null;

        /// <summary> merge sareru?s
        /// Description of the data contained in the depth frame
        /// </summary>
        private FrameDescription depthFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap depthBitmap = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] depthPixels = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        /// 



        /// <summary>
        /// Current status text to display
        /// </summary>
        private int RECORD_SIZE = 128;
        private int counter = 0;
        private int writeDownedCounter = 0;
        private bool cursol_locked = false;
        private Point p = new Point();
        private Point targetPosition = new Point();
        private List<KeyValuePair<string, ushort>> MyTimeValue = new List<KeyValuePair<string, ushort>>();
        private bool TimeStampFrag = false;
        private bool IsTimestampNeeded = true;
        private bool WritingFlag = false;
        private bool ArrayResized = false;
        private bool FileNameStableFlag = false;
        private int WaitForStartingRecord = 1;
        private ushort[] measureDepthArray = new ushort[1];
        private ushort[] centerDepthArray = new ushort[1];
        private ushort[] measureIrArray = new ushort[1];
        private ushort[] centerIrArray = new ushort[1];
        private ushort[] IrGlobalArray = new ushort[1];
        private ushort[] DepthGlobalArray = new ushort[1];

        private Point FrameSizePoint;
        private DateTime timestamp = new DateTime();
        private System.Windows.Controls.Label[] ValueLabels;
        private const int MapDepthToByte = 8000 / 256;
        private bool mapIsIR = true;


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the depth frames
            this.infraredFrameReader = this.kinectSensor.InfraredFrameSource.OpenReader();

            // wire handler for frame arrival
            this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;

            // get FrameDescription from InfraredFrameSource
            this.infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;

            // create the bitmap to display
            this.infraredBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            this.DataContext = this;

            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            // wire handler for frame arrival
            this.depthFrameReader.FrameArrived += this.Reader_FrameArrived;

            // get FrameDescription from DepthFrameSource
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            // create the bitmap to display
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);


            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example

            // initialize the components (controls) of the window
            this.InitializeComponent();
            this.ButtonWriteDown.IsEnabled = false;

            this.ValueLabels = new System.Windows.Controls.Label[9];

            this.ValueLabels[0] = this.Label0;
            this.ValueLabels[1] = this.Label1;
            this.ValueLabels[2] = this.Label2;
            this.ValueLabels[3] = this.Label3;
            this.ValueLabels[4] = this.Label4;
            this.ValueLabels[5] = this.Label5;
            this.ValueLabels[6] = this.Label6;
            this.ValueLabels[7] = this.Label7;
            this.ValueLabels[8] = this.Label8;
            this.Picture.Source = depthBitmap;
            Array.Resize(ref IrGlobalArray, this.infraredFrameDescription.Width * this.infraredFrameDescription.Height);
            Array.Resize(ref DepthGlobalArray, this.depthFrameDescription.Width * this.depthFrameDescription.Height);

            
        }
/*
        private void RecordInitializer()
        {
            cursol_locked = true;
            writeDownedCounter = 0;
            ButtonWriteDown.IsEnabled = false;
            textXlock.IsEnabled = false;
            textYlock.IsEnabled = false;
            timestamp = DateTime.Now;  //timestamp is the time when the record is started
            WritingFlag = true;
        }
        */
        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.infraredFrameReader != null)
            {
                // InfraredFrameReader is IDisposable
                this.infraredFrameReader.Dispose();
                this.infraredFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }


        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>

        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            // InfraredFrame is IDisposable
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    // the fastest way to process the infrared frame data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                    {
                        // verify data and write the new infrared frame data to the display bitmap
                        if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / this.infraredFrameDescription.BytesPerPixel)) &&
                            (this.infraredFrameDescription.Width == this.infraredBitmap.PixelWidth) && (this.infraredFrameDescription.Height == this.infraredBitmap.PixelHeight))
                        {
                            this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                        }
                    }
                }
            }
        }

        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame()) //こいつを調べる
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue; // ushort.MaxValue is 65535

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {

            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;
            if (!mapIsIR)
            {
                TextGenerate(frameData);
            }

            for (int i = 0; i < this.depthFrameDescription.Width* this.depthFrameDescription.Height; i++)
            {
                DepthGlobalArray[i] = frameData[i];
            }
            
            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }
        /// <summary>
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrameData">Pointer to the InfraredFrame image data</param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;
            if (mapIsIR)
            {
                TextGenerate(frameData);
            }

            for (int i = 0; i < this.infraredFrameDescription.Width * this.infraredFrameDescription.Height; i++)
            {
                IrGlobalArray[i] = frameData[i];
            }
                // lock the target bitmap
                this.infraredBitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this.infraredBitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / this.infraredFrameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            this.infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, this.infraredBitmap.PixelWidth, this.infraredBitmap.PixelHeight));

            // unlock the bitmap
            this.infraredBitmap.Unlock();
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
        private unsafe void TextGenerate(ushort* ProcessData)
        {
            int VerticalCheckDistance = 150;
            int HorizontalCheckDistance = 150;
            int HorizontalError = 0;
            int VerticalError = 0;
            Point roop = new Point();
            if (cursol_locked)
            {
                if (WritingFlag)
                {
                    writeToArrayRectangle(ProcessData, getLockPosition());
                }

                else
                {
                    TimeStampFrag = false;
                }
                targetPosition = getLockPosition();
                if(targetPosition.X==256 && targetPosition.Y == 212 && !WritingFlag)
                {
                    for (int indexValueX = -1; indexValueX < 2; indexValueX++)
                    {
                        for (int indexValueY = -1; indexValueY < 2; indexValueY++)
                        {
                            roop.X = targetPosition.X + HorizontalCheckDistance * indexValueX;
                            roop.Y = targetPosition.Y + VerticalCheckDistance * indexValueY;
                            this.ValueLabels[(indexValueX+1)+3*(indexValueY+1)].Content = roop.ToString()+ "\r\n" + shiburinkawaiiyoo(ProcessData, roop);
                            HorizontalError = (shiburinkawaiiyoo(ProcessData, targetPosition.X - HorizontalCheckDistance, targetPosition.Y) - shiburinkawaiiyoo(ProcessData, targetPosition.X + HorizontalCheckDistance, targetPosition.Y));
                            VerticalError = (shiburinkawaiiyoo(ProcessData, targetPosition.X, targetPosition.Y - VerticalCheckDistance) - shiburinkawaiiyoo(ProcessData, targetPosition.X, targetPosition.Y + VerticalCheckDistance));

                        }
                    }
                }
                this.filenameLabel.Content = "X error " + HorizontalError.ToString() + "\r\nY error " + VerticalError.ToString();
                this.StatusText = targetPosition.X + " " + targetPosition.Y +" "+shiburinkawaiiyoo(ProcessData,targetPosition.X,targetPosition.Y)+ " Writing is " +WritingFlag+ " Writed sample number =" + writeDownedCounter.ToString();
            }
            else
            {
                this.StatusText = "unlocked";
            }
        }
        
        private unsafe ushort shiburinkawaiiyoo(ushort* ProcessData, double X,double Y)
        {
            return ProcessData[(int)(Y * this.infraredFrameDescription.Width + X)];
        }
        private unsafe ushort shiburinkawaiiyoo(ushort* ProcessData, Point location)
        {
            return ProcessData[(int)(location.Y * this.infraredFrameDescription.Width + location.X)];
        }

        private unsafe ushort shiburinkawaiiyoo(ushort[] ProcessData, double X, double Y)
        {
            return ProcessData[(int)(Y * this.infraredFrameDescription.Width + X)];

        }

        private unsafe void writeToText(ushort[] measureArray, ushort[] centerArray, string type)
        {

            string StartedTime = makeTimestampFilename(timestamp);
            string filenamePartialIR = FileNameStableFlag ? System.IO.Path.Combine(@"V:\Eng\FrameData\", type + "Measure" + this.FileNameTextbox.GetLineText(0) + ".dat") : System.IO.Path.Combine(@"V:\KinectIR\capturedData\", StartedTime + type + ".dat");
            this.filenameLabel.Content = filenamePartialIR;
            string filenameCenterIR = FileNameStableFlag ? System.IO.Path.Combine(@"V:\Eng\FrameData\", type + "Center" + this.FileNameTextbox.GetLineText(0) + ".dat") : System.IO.Path.Combine(@"V:\KinectIR\capturedData\", StartedTime + "IRcenter.dat");
            string framesizedataFile = System.IO.Path.Combine(@"V:\Eng\FrameData\", "sizeofframe" + this.FileNameTextbox.GetLineText(0) + ".dat");
            
            System.IO.StreamWriter writingSwIR = new System.IO.StreamWriter(filenamePartialIR, false, System.Text.Encoding.GetEncoding("shift_jis"));
            System.IO.StreamWriter writingCenterIR = new System.IO.StreamWriter(filenameCenterIR, false, System.Text.Encoding.GetEncoding("shift_jis"));

            System.IO.StreamWriter FramesizeData = new System.IO.StreamWriter(framesizedataFile, false, System.Text.Encoding.GetEncoding("shift_jis"));
            
            
            if (!TimeStampFrag && IsTimestampNeeded)
            {
                writingSwIR.Write("\nwriting start\n" + timestamp.ToString() + "\r\n"); //time stamp writelinedeyokune?
                writingCenterIR.Write("\nwriting start\n" + timestamp.ToString() + "\r\n"); //time stamp
                TimeStampFrag = true;
            }
            for (int i = 0; i < measureArray.Length; i++)
            {
                writingSwIR.Write(measureArray[i] + "\r\n");
            }
            for (int j = 0; j < centerArray.Length; j++ )
            {
                writingCenterIR.Write(centerArray[j].ToString() + "\r\n");
            }
            
            if (IsTimestampNeeded)
            {
                DateTime dtnow = DateTime.Now;
                writingSwIR.Write(dtnow.ToString() + "redord ended");
                writingCenterIR.Write(dtnow.ToString() + "redord ended");
            }
            timestamp = DateTime.Now;
            writingSwIR.Close();
            writingCenterIR.Close();

            FramesizeData.Write(FrameSizePoint.X + "\r\n" + FrameSizePoint.Y + "\r\n" + RECORD_SIZE + "\r\n");
            FramesizeData.Close();
            

        }
        private Point getLockPosition()
        {
            double temp;
            Point LockPosition = new Point();
            Point InvalidNum = new Point();
            InvalidNum.X = 256;
            InvalidNum.Y = 212;

            if (double.TryParse(this.textXlock.Text, out temp))
            {
                LockPosition.X = temp;
            }
            else
            {
                return InvalidNum;
            }

            if (double.TryParse(this.textYlock.Text, out temp))
            {
                LockPosition.Y = temp;
            }
            else
            {
                return InvalidNum;
            }
            if ((0 <= LockPosition.X && LockPosition.X < this.infraredFrameDescription.Width) && (0 <= LockPosition.Y && LockPosition.Y < this.infraredFrameDescription.Height))
            {
                return LockPosition;
            }
            else
            {
                return InvalidNum;
            }
            
        }

        private void CheckNonTimeStamp_Checked(object sender, RoutedEventArgs e)
        {
            IsTimestampNeeded = false;
        }

        private void CheckNonTimeStamp_Unchecked(object sender, RoutedEventArgs e)
        {
            IsTimestampNeeded = true;
        }

        private void ButtonWriteDown_Click(object sender, RoutedEventArgs e)
        {
            
            DispatcherTimer  ButtonEditorTimer = new DispatcherTimer(DispatcherPriority.Normal);
            ButtonEditorTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            ButtonEditorTimer.Tick += new EventHandler(ButtonEdit);
            ButtonEditorTimer.Start();
            writeDownedCounter = 0;
            ButtonWriteDown.IsEnabled = false;
            textXlock.IsEnabled = false;
            textYlock.IsEnabled = false;
            timestamp = DateTime.Now;  //timestamp is the time when the record is started
            
            
        }

        private void ButtonEdit(object sender, EventArgs e)
        {
            
            this.ButtonWriteDown.Content = (WaitForStartingRecord).ToString();
            WaitForStartingRecord--;
            if (WaitForStartingRecord == -1)
            {
                WritingFlag = true;
            }
        }

        private void CheckLockCenter_Checked(object sender, RoutedEventArgs e)
        {
            cursol_locked = true;
            this.ButtonWriteDown.IsEnabled = true;
        }

        private void CheckLockCenter_Unchecked(object sender, RoutedEventArgs e)
        {
            cursol_locked = false;
            this.ButtonWriteDown.IsEnabled = false;
        }
        /*
        private unsafe void writeToArrayHorizontalPixels(ushort* ProcessData, Point location)
        {
            int horizonalLength = 3; //記録する長さ　片側
            int pixelMargin = 1; //ピクセル間隔
                
            if (!ArrayResized)
            {
                Array.Resize(ref measureDepthArray, RECORD_SIZE * (2 * horizonalLength + 1));
                Array.Resize(ref centerDepthArray, RECORD_SIZE);
            }
            ArrayResized = true;
            int index_value = 0;
            double pointX;
            double pointY;
            for (int i = 0; i < horizonalLength * 2 + 1; i++)
            {

                index_value = i;
                pointX = location.X + (index_value - horizonalLength) * pixelMargin;
                pointY = location.Y;
                measureDepthArray[index_value + writeDownedCounter * (horizonalLength * 2 + 1)] = shiburinkawaiiyoo(ProcessData, pointX, pointY);

            }
            centerDepthArray[writeDownedCounter] = shiburinkawaiiyoo(ProcessData, location.X, location.Y);

            writeDownedCounter++;
            if (writeDownedCounter == measureDepthArray.Length / 7)
            {
                WritingFlag = false;
                writeToText(measureDepthArray, centerDepthArray, "Depth");
                ButtonWriteDown.IsEnabled = true;            }
        }
        */
        private unsafe void writeToArrayRectangle(ushort* ProcessData, Point location)
        {
            int recordPixelX = 201; //水平方向の記録ピクセル数 odd
            int recordPixelY = 201; //垂直方向の記録ピクセル数 odd
            int marginX = 1; // 記録するピクセルの間隔　1=連続
            int marginY = 1; // 記録するピクセルの間隔　1=連続

            FrameSizePoint.X = recordPixelX;
            FrameSizePoint.Y = recordPixelY;

            int x = (int)(recordPixelX / 2);
            int y = (int)(recordPixelY / 2);
            if (!ArrayResized)
            {
                Array.Resize(ref measureDepthArray, RECORD_SIZE * (recordPixelX * recordPixelY));
                Array.Resize(ref centerDepthArray, RECORD_SIZE);
                Array.Resize(ref measureIrArray, RECORD_SIZE * (recordPixelX * recordPixelY));
                Array.Resize(ref centerIrArray, RECORD_SIZE);
            }
            ArrayResized = true;
            int index_value = 0;
            for (int j = -y ; j <= y ; j++)
            {
                for (int i = -x; i <= x; i++)
                {
                    index_value = (j + y) * recordPixelY + (i + x);
                    measureDepthArray[index_value + writeDownedCounter * recordPixelX * recordPixelY] = shiburinkawaiiyoo(DepthGlobalArray, location.X + i * marginX, location.Y + j * marginY);
                    measureIrArray[index_value + writeDownedCounter * recordPixelX * recordPixelY] = shiburinkawaiiyoo(IrGlobalArray, location.X + i * marginX, location.Y + j * marginY);

                }
            }
            centerDepthArray[writeDownedCounter] = shiburinkawaiiyoo(DepthGlobalArray, location.X, location.Y);
            centerIrArray[writeDownedCounter] = shiburinkawaiiyoo(IrGlobalArray, location.X, location.Y);

            writeDownedCounter++;
            if (writeDownedCounter == centerDepthArray.Length)
            {
                WritingFlag = false;
                writeToText(measureDepthArray,centerDepthArray,"Depth");
                writeToText(measureIrArray, centerIrArray, "Infrared");
                ButtonWriteDown.IsEnabled = true;
            }
        }


        private string makeTimestampFilename(DateTime printTime)
        {
            string mikachan = printTime.ToString().Replace(@"/", "");
            mikachan = mikachan.Replace(@":", "");
            mikachan = mikachan.Replace(" ", "");
            return mikachan;
        }

        private void ChangeData_Click(object sender, RoutedEventArgs e)
        {
            if (mapIsIR)
            {
                this.ChangeData.Content = "ToInfraRed";
                this.Picture.Source = depthBitmap;
            }
            else
            {
                this.ChangeData.Content = "ToDepth";
                this.Picture.Source = infraredBitmap;
            }
            mapIsIR = !mapIsIR;
        }

        private void CheckFileNameStable_Checked(object sender, RoutedEventArgs e)
        {
            FileNameStableFlag = true;
            IsTimestampNeeded = false;
            this.CheckNonTimeStamp.IsEnabled = false;
        }

        private void CheckFileNameStable_Unchecked(object sender, RoutedEventArgs e)
        {
            FileNameStableFlag = false;
            IsTimestampNeeded = true;
            this.CheckNonTimeStamp.IsEnabled = true;
        }

        private void Picture_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!WritingFlag)
            {
                this.textXlock.Text = e.GetPosition(Picture).X.ToString();
                this.textYlock.Text = e.GetPosition(Picture).Y.ToString();
            }


        }

        private void ButtonXup_Click(object sender, RoutedEventArgs e)
        {
            Point pointNow = getLockPosition();
            pointNow.X++;
            this.textXlock.Text = pointNow.X.ToString();
            
        }

        private void ButtonXdown_Click(object sender, RoutedEventArgs e)
        {
            Point pointNow = getLockPosition();
            pointNow.X--;
            this.textXlock.Text = pointNow.X.ToString();
        }

        private void ButtonYup_Click(object sender, RoutedEventArgs e)
        {
            Point pointNow = getLockPosition();
            pointNow.Y++;
            this.textYlock.Text = pointNow.Y.ToString();
        }

        private void ButtonYdown_Click(object sender, RoutedEventArgs e)
        {
            Point pointNow = getLockPosition();
            pointNow.Y--;
            this.textYlock.Text = pointNow.Y.ToString();
        }
    }
}
