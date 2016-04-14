using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;


namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        


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
            this.fbd.ShowDialog();
            //System.Windows.MessageBox.Show(this.fbd.SelectedPath);
            DispatcherTimer ButtonEditorTimer = new DispatcherTimer(DispatcherPriority.Normal);
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


        private string makeTimestampFilename(DateTime printTime)
        {
            string mikachan = printTime.ToString().Replace(@"/", "");
            mikachan = mikachan.Replace(@":", "");
            mikachan = mikachan.Replace(" ", "");
            return mikachan;
        }

        private string makeFilePassForUnix(string pass)
        {
            string passUnix = "mika";
            if (pass.IndexOf(@"V:\") < 0)
            {
                return passUnix;
            }
            pass = pass.Replace(@"V:\", @"/home/mkuser/");
            pass = pass.Replace(@"\", @"/");
            return pass;
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
                this.textXlock.Text = e.GetPosition(this.Picture).X.ToString();
                this.textYlock.Text = e.GetPosition(this.Picture).Y.ToString();
            }


        }

        private void ButtonXup_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Point pointNow = getLockPosition();
            pointNow.X++;
            this.textXlock.Text = pointNow.X.ToString();

        }

        private void ButtonXdown_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Point pointNow = getLockPosition();
            pointNow.X--;
            this.textXlock.Text = pointNow.X.ToString();
        }

        private void ButtonYup_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Point pointNow = getLockPosition();
            pointNow.Y++;
            this.textYlock.Text = pointNow.Y.ToString();
        }

        private void ButtonYdown_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Point PointNow = getLockPosition();
            PointNow.Y--;
            this.textYlock.Text = PointNow.Y.ToString();
        }
    }    
}


    