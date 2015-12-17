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

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        private int RECORD_SIZE = 10;
        private int counter = 0;
        private int writeDownedCounter = 0;
        private bool cursol_locked = true;
        private Point p = new Point();
        private Point targetPosition = new Point();
        private List<KeyValuePair<string, ushort>> MyTimeValue = new List<KeyValuePair<string, ushort>>();

        private bool TimeStampFrag = false;
        private bool IsTimestampNeeded = true;
        private bool WritingFlag = false;
        private bool ArrayResized = false;
        private int WaitForStartingRecord = 1;
        private ushort[] fukuisan = new ushort[1];
        private ushort[] old_fukuisan = new ushort[1];
        private DateTime timestamp = new DateTime();
        private System.Windows.Controls.Label[] ValueLabels;

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

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;

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
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.infraredBitmap;
            }
        }

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
            TextGenerate(frameData);

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
            int VerticalCheckDistance = 15;
            int HorizontalCheckDistance = 15;
            Point roop = new Point();
            if (cursol_locked)
            {
                if (WritingFlag)
                {
                    writeToArrayHorizontalPixels(ProcessData, getLockPosition());
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
                        }
                    }
                }
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


        private void writeToText()
        {
            string StartedTime = makeTimestampFilename(timestamp);
            string filenamePartialIR = System.IO.Path.Combine(@"C:\Users\mkuser\Documents\capturedData\",StartedTime+"IR.dat");
            string filenameCenterIR = System.IO.Path.Combine(@"C:\Users\mkuser\Documents\capturedData\",StartedTime+"IRcenter.dat");
            System.IO.StreamWriter writingSwIR = new System.IO.StreamWriter(filenamePartialIR, true, System.Text.Encoding.GetEncoding("shift_jis"));
            System.IO.StreamWriter writingCenterIR = new System.IO.StreamWriter(filenameCenterIR, true, System.Text.Encoding.GetEncoding("shift_jis"));
            if (!TimeStampFrag && IsTimestampNeeded)
            {
                writingSwIR.Write("\nwriting start\n" + timestamp.ToString() + "\r\n"); //time stamp writelinedeyokune?
                writingCenterIR.Write("\nwriting start\n" + timestamp.ToString() + "\r\n"); //time stamp
            }
            for (int i = 0; i < fukuisan.Length; i++)
            {
                writingSwIR.Write(fukuisan[i] + "\r\n");
            }
            for (int j = 0; j < old_fukuisan.Length; j++ )
            {
                writingCenterIR.Write(old_fukuisan[j].ToString() + "\r\n");
            }
            
            if (IsTimestampNeeded)
            {
                DateTime dtnow = DateTime.Now;
                writingSwIR.Write(dtnow.ToString() + "redord ended\r\n");
                writingCenterIR.Write(dtnow.ToString() + "redord ended\r\n");
            }
            timestamp = DateTime.Now;
            writingSwIR.Close();
            writingCenterIR.Close();
            

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

        private unsafe void writeToArrayHorizontalPixels(ushort* ProcessData, Point location)
        {
            int horizonalLength = 3; //記録する長さ　片側
            int pixelMargin = 1; //ピクセル間隔
                
            if (!ArrayResized)
            {
                Array.Resize(ref fukuisan, RECORD_SIZE * (2 * horizonalLength + 1));
                Array.Resize(ref old_fukuisan, RECORD_SIZE);
            }
            int index_value = 0;
            double pointX;
            double pointY;

            TimeStampFrag = true;
            for (int i = 0; i < horizonalLength * 2 + 1; i++)
            {

                index_value = i;
                pointX = location.X + (index_value - horizonalLength) * pixelMargin;
                pointY = location.Y;
                fukuisan[index_value + writeDownedCounter * (horizonalLength * 2 + 1)] = shiburinkawaiiyoo(ProcessData, pointX, pointY);

            }
            old_fukuisan[writeDownedCounter] = shiburinkawaiiyoo(ProcessData, location.X, location.Y);

            writeDownedCounter++;
            if (writeDownedCounter == fukuisan.Length / 7)
            {
                finished();
            }
        }

        private unsafe void writeToArrayRectangle(ushort* ProcessData, Point location)
        {
            int recordPixelX = 11; //水平方向の記録ピクセル数
            int recordPixelY = 11; //垂直方向の記録ピクセル数
            int marginX = 1; // 記録するピクセルの間隔　1=連続
            int marginY = 1; // 記録するピクセルの間隔　1=連続

            int index_value = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    index_value = i * 3 + j;
                    fukuisan[index_value + writeDownedCounter * 9] = shiburinkawaiiyoo(ProcessData, location.X - marginX + j * marginX, location.Y - marginY + i * marginY);
                    this.ValueLabels[index_value].Content = fukuisan[index_value + writeDownedCounter * 9];
                }
            }
            old_fukuisan[writeDownedCounter] = shiburinkawaiiyoo(ProcessData, location.X, location.Y);

            writeDownedCounter++;
            if (writeDownedCounter == fukuisan.Length / 9)
            {
                finished();
            }
        }
        private void finished()
        {
            WritingFlag = false;
            writeToText();
            ButtonWriteDown.IsEnabled = true;
        }

        private string makeTimestampFilename(DateTime printTime)
        {
            string mikachan = printTime.ToString().Replace(@"/", "");
            mikachan = mikachan.Replace(@":", "");
            mikachan = mikachan.Replace(" ", "");
            return mikachan;
        }
    }
}
