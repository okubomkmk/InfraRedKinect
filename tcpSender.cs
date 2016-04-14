using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Windows;
using Microsoft.Kinect;
　
namespace Microsoft.Samples.Kinect.InfraredBasics
{

    class tcpSender
    {
        //IPアドレスとポート番号を指定
            //string型とint型なのが不思議
            //勿論送信先のIPアドレスとポート番号です
        private string ipAddress = "172.16.2.22";
        private IPAddress ip = IPAddress.Loopback;
        private int port = 22222;
        public TcpClient client;
        public NetworkStream stream;
        private bool senderCaughedException = false;
        //IPアドレスとポート番号を渡してサーバ側へ接続
        public tcpSender()
        {
            try{
                client = new TcpClient(ipAddress, port);
                stream = client.GetStream();

            }
            catch(Exception e){
                System.Windows.Forms.MessageBox.Show(e.Message);
                senderCaughedException = true;
            }
           
        } 


         

        public unsafe void sendLockedPoint(ushort[] frame, Point point)
        {
            sendData(shiburin(frame, point));
        }

        public unsafe void sendAveragedDataForDepth(ushort[] frame, Point point, int area)
        {
            double data = 0;            
            for (int i = -area; i <= area; i++)
            {
                for (int j = -area; j <= area; j++)
                {
                    data += shiburin(frame, point.X - 3 * area * i, point.Y - 3 * area * j) / Math.Pow(area ,2);
                }
            }
            
            sendData(data);
        }

        public void close()
        {
            client.Close();
        }
        
        private ushort shiburin(ushort[] frame, double x, double y)
        {
            ushort data = frame[(int)(y * 512 + x)];
            return data;    
        }

        private ushort shiburin(ushort[] frame, Point point)
        {
            ushort data = frame[(int)(point.Y * 512 + point.X)];
            return data;
        }

        private unsafe void sendData(double data)
        {
            if (!senderCaughedException)
            {
                try
                {

                    //表示するのは「Hello! C#」
                    //これを送信用にbyte型へ直します

                    byte[] tmp = Encoding.UTF8.GetBytes(data.ToString() + "\r\n");

                    stream.Write(tmp, 0, tmp.Length);



                    //NWのデータを扱うストリームを作成


                    //送信
                    //引数は（データ , データ書き込み開始位置 , 書き込むバイト数）
                    //だそうです


                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message);
                    senderCaughedException = true;
                }
            }
        }

        public void sendEndCode()
        {
            sendData(99999);
        }

    }
    
}
