using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
namespace Microsoft.Samples.Kinect.InfraredBasics
{
    class readFromcppdata
    {
        private string fileName;
        private string location;
        private string[] separeted = new string [4];
        private Point beginPoint, endPoint;
        private int frameWidth = 512;
        private int frameHeight = 424;
        private bool isRead = false;
        public readFromcppdata()
        {
            try
            {
                using (StreamReader sr = new StreamReader(@"V:\KinectIR\cplusplus\areadescripcioncplus.dat", Encoding.GetEncoding("Shift_JIS")))
                {
                    location = sr.ReadToEnd();
                    separeted = location.Split(' ');
                    beginPoint.X = checkValue(0);
                    beginPoint.Y = checkValue(1);
                    endPoint.X = checkValue(2);
                    endPoint.Y = checkValue(3);
                    if (beginPoint.X != -1 && beginPoint.Y != -1 && endPoint.X != -1 && endPoint.Y != -1)
                    {
                        isRead = true;
                    }


                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                isRead = false;
            }
        }

        private int checkValue(int i)
        {
            int result = -1;
            if (i % 2 == 0)
            {
                if(int.TryParse(separeted[i], out result))
                {
                    if (0 <= result && result < frameWidth)
                    {
                        return result;
                    }
                    else
                    {
                        return -1;
                    }

                }
            }
            else
            {
                if (int.TryParse(separeted[i], out result))
                {
                    if (0 <= result && result < frameHeight)
                    {
                        return result;
                    }
                    else
                    {
                        return -1;
                    }

                }
            }
            return result;
        }

        public Point BeginPoint
        {
            get
            {
                return this.beginPoint;
            }
        }

        public Point EndPoint
        {
            get
            {
                return this.endPoint;
            }
        }

        public bool IsRead
        {
            get
            {
                return this.isRead;
            }
        }

        
    }
}
