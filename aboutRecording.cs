using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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
                    writeToArrayPoint(ProcessData, getLockPosition());
                }

                else
                {
                    TimeStampFrag = false;
                    
                    tcpsender.sendAveragedDataForDepth(DepthGlobalArray, (int)(R1.X / 2 + R2.X/2), (int)(R1.Y/ 2 + R2.Y/2), 2);

                }
                targetPosition = getLockPosition();
                if (targetPosition.X == 256 && targetPosition.Y == 212 && !WritingFlag)
                {
                    for (int indexValueX = -1; indexValueX < 2; indexValueX++)
                    {
                        for (int indexValueY = -1; indexValueY < 2; indexValueY++)
                        {
                            roop.X = targetPosition.X + HorizontalCheckDistance * indexValueX;
                            roop.Y = targetPosition.Y + VerticalCheckDistance * indexValueY;
                            this.ValueLabels[(indexValueX + 1) + 3 * (indexValueY + 1)].Content = roop.ToString() + "\r\n" + shiburinkawaiiyoo(ProcessData, roop);
                            HorizontalError = (shiburinkawaiiyoo(ProcessData, targetPosition.X - HorizontalCheckDistance, targetPosition.Y) - shiburinkawaiiyoo(ProcessData, targetPosition.X + HorizontalCheckDistance, targetPosition.Y));
                            VerticalError = (shiburinkawaiiyoo(ProcessData, targetPosition.X, targetPosition.Y - VerticalCheckDistance) - shiburinkawaiiyoo(ProcessData, targetPosition.X, targetPosition.Y + VerticalCheckDistance));

                        }
                    }
                }
                this.filenameLabel.Content = "X error " + HorizontalError.ToString() + "\r\nY error " + VerticalError.ToString();
                this.StatusText = targetPosition.X + " " + targetPosition.Y + " " + shiburinkawaiiyoo(ProcessData, targetPosition.X, targetPosition.Y) + " Writing is " + WritingFlag + " Writed sample number =" + writeDownedCounter.ToString();
            }
            else
            {
                this.StatusText = "unlocked";
            }
        }

        private unsafe ushort shiburinkawaiiyoo(ushort* ProcessData, double X, double Y)
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
            string passSaveFolder = this.fbd.SelectedPath;
            string StartedTime = makeTimestampFilename(timestamp);
            string filenamePartialIR = FileNameStableFlag ? System.IO.Path.Combine(passSaveFolder, type + "Measure" + this.FileNameTextbox.GetLineText(0) + ".dat") : System.IO.Path.Combine(passSaveFolder, StartedTime + type + ".dat");
            this.filenameLabel.Content = filenamePartialIR;
            string filenameCenterIR = FileNameStableFlag ? System.IO.Path.Combine(passSaveFolder, type + "Center" + this.FileNameTextbox.GetLineText(0) + ".dat") : System.IO.Path.Combine(passSaveFolder, StartedTime + "IRcenter.dat");
            string framesizedataFile = System.IO.Path.Combine(passSaveFolder, "sizeofframe" + this.FileNameTextbox.GetLineText(0) + type + ".dat");
            string imageFileName = System.IO.Path.Combine(passSaveFolder, "image" + this.FileNameTextbox.GetLineText(0) + type + ".bmp");
            System.IO.StreamWriter writingSwIR = new System.IO.StreamWriter(filenamePartialIR, false, System.Text.Encoding.GetEncoding("shift_jis"));
            System.IO.StreamWriter writingCenterIR = new System.IO.StreamWriter(filenameCenterIR, false, System.Text.Encoding.GetEncoding("shift_jis"));

            FramesizeData = new System.IO.StreamWriter(framesizedataFile, false, System.Text.Encoding.GetEncoding("shift_jis"));


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
            for (int j = 0; j < centerArray.Length; j++)
            {
                writingCenterIR.Write(centerArray[j].ToString() + "\r\n");
            }
            DateTime dtnow = DateTime.Now;

            if (IsTimestampNeeded)
            {
                writingSwIR.Write(dtnow.ToString() + "redord ended");
                writingCenterIR.Write(dtnow.ToString() + "redord ended");
            }
            timestamp = DateTime.Now;
            writingSwIR.Close();
            writingCenterIR.Close();

            FramesizeData.Write(FrameSizePoint.X + "\r\n" + FrameSizePoint.Y + "\r\n" + RECORD_SIZE + "\r\n");
            FramesizeData.Write(type + "\r\n" + filenamePartialIR + "\r\n" + "Unix" + "\r\n" + makeFilePassForUnix(filenamePartialIR) + "\r\n");
            FramesizeData.Write(dtnow.ToString() + "\r\n" + "Xleft = " + R1.X.ToString() + " Yleft = " + R1.Y.ToString() + "\r\nXright = " + R2.X.ToString() + " Yright = " + R2.Y.ToString() + "\r\n");
            FramesizeData.Write(this.CommentTextBox.Text + "\r\n");

            using (FileStream imageStream = new FileStream(imageFileName, FileMode.Create, FileAccess.Write))
            {
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                if(type.Equals("Depth"))
                {
                    encoder.Frames.Add(BitmapFrame.Create(depthBitmap));
                    encoder.Save(imageStream);
                }
                else if (type.Equals("Infrared"))
                {
                    encoder.Frames.Add(BitmapFrame.Create(infraredBitmap));
                    encoder.Save(imageStream);
                }
                else
                {
                    FramesizeData.Write("image was not created");
                }
            }
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
            for (int j = -y; j <= y; j++)
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
                writeToText(measureDepthArray, centerDepthArray, "Depth");
                writeToText(measureIrArray, centerIrArray, "Infrared");
                ButtonWriteDown.IsEnabled = true;
            }
        }

        private unsafe void writeToArrayPoint(ushort* ProcessData, Point location)
        {
            int leftX = 0, leftY = 0, rightX = 511, rightY = 423;
            if (areaReader.IsRead)
            {
                R1 = areaReader.BeginPoint;
                R2 = areaReader.EndPoint;
            }
            else
            {
                R1 = new Point(leftX, leftY);
                R2 = new Point(rightX, rightY);
            }

            FrameSizePoint.X = R2.X - R1.X + 1;
            FrameSizePoint.Y = R2.Y - R1.Y + 1;
            int Framesize = (int)(FrameSizePoint.X * FrameSizePoint.Y);
            if (!ArrayResized)
            {
                Array.Resize(ref measureDepthArray, RECORD_SIZE * (int)(FrameSizePoint.X * FrameSizePoint.Y));
                Array.Resize(ref centerDepthArray, RECORD_SIZE);
                Array.Resize(ref measureIrArray, RECORD_SIZE * (int)(FrameSizePoint.X * FrameSizePoint.Y));
                Array.Resize(ref centerIrArray, RECORD_SIZE);
            }

            ArrayResized = true;
            int index_value = 0;
            for (int j = (int)R1.Y; j <= R2.Y; j++)
            {
                for (int i = (int)R1.X; i <= R2.X; i++)
                {
                    measureDepthArray[writeDownedCounter * Framesize + index_value] = shiburinkawaiiyoo(DepthGlobalArray, i, j);
                    measureIrArray[writeDownedCounter * Framesize + index_value] = shiburinkawaiiyoo(IrGlobalArray, i, j);
                    index_value++;
                }
            }
            centerDepthArray[writeDownedCounter] = shiburinkawaiiyoo(DepthGlobalArray, location.X, location.Y);
            centerIrArray[writeDownedCounter] = shiburinkawaiiyoo(IrGlobalArray, location.X, location.Y);

            writeDownedCounter++;
            if (writeDownedCounter == centerDepthArray.Length)
            {
                WritingFlag = false;
                writeToText(measureDepthArray, centerDepthArray, "Depth");
                writeToText(measureIrArray, centerIrArray, "Infrared");
                ButtonWriteDown.IsEnabled = true;
            }



        }
        private unsafe void writeFullFrameToArray(ushort* ProcessData)
        {
            FrameSizePoint.X = depthFrameDescription.Width;
            FrameSizePoint.Y = depthFrameDescription.Height;
            if (!ArrayResized)
            {
                Array.Resize(ref measureDepthArray, RECORD_SIZE * (int)(FrameSizePoint.X * FrameSizePoint.Y));
                Array.Resize(ref centerDepthArray, RECORD_SIZE);

            }

            ArrayResized = true;
            int Framesize = (int)(FrameSizePoint.X * FrameSizePoint.Y);

            int index_value = 0;
            for (int j = 0; j < depthFrameDescription.Height; j++)
            {
                for (int i = 0; i < depthFrameDescription.Width; i++)
                {
                    measureDepthArray[writeDownedCounter * Framesize + index_value] = shiburinkawaiiyoo(ProcessData, i, j);
                    index_value++;
                }
            }

        }    

    }
}
