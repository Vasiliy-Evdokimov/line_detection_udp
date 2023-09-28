using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Reflection.Emit;
using System.Xml.Linq;
using System.Runtime.ConstrainedExecution;

namespace udp_draw
{
    public struct CamData
    {
        public int img_width;
        public int img_height;
        public bool error_flag;
        //
        public int max_points_count;
        public int points_count;
        public List<Point> curr_points;
        //
        public int max_hor_count;
        public int hor_count;
        public List<int> curr_hor;
        //
        public bool fl_slow_zone;
        public bool fl_stop_zone;
        public bool fl_stop_mark;
        public int stop_mark_distance;
    }

    public partial class Form1 : Form
    {
        const bool SHOW_MSGS = false;
        //
        Queue<string> msgs = new Queue<string>();
        //
        const int top_offset = 50;
        const int left_offset = 10;
        //
        string curr_msg = "";

        CamData[] cam_data = new CamData[2];

        const string serverIP = "192.168.1.5";
        const int serverPort = 8888;

        UdpClient client;
        IPEndPoint serverEndPoint;

        const string reqMessage = "WAITFORDATA";
        byte[] reqData;
        int reqDataLength;

        Font textFont;
        SolidBrush textBrush;

        public Form1()
        {
            InitializeComponent();
            //
            client = new UdpClient();
            IPAddress serverAddress = IPAddress.Parse(serverIP);
            serverEndPoint = new IPEndPoint(serverAddress, serverPort);
            //                        
            reqData = Encoding.ASCII.GetBytes(reqMessage);
            reqDataLength = reqData.Length;
            //
            textFont = new Font("Arial", 16);
            textBrush = new SolidBrush(Color.Cyan);
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Draw(e.Graphics);
        }

        private void DrawFlagEllipse(Graphics g, int aX, int aY, Color aColor)
        {
            Brush br = new SolidBrush(aColor);
            g.FillEllipse(br, aX, aY, 30, 30);
        }
        
        private void Draw(Graphics g)
        {
            
            int offset = 0;

            for (int i = 0; i < cam_data.Length; i++) {

                CamData cd = cam_data[i];

                Pen borderPen = new Pen(Color.Yellow, 4);
                Brush borderBrush = new SolidBrush(Color.Black);
                g.FillRectangle(borderBrush, left_offset + offset, top_offset, cd.img_width, cd.img_height);
                //
                if (cd.error_flag)
                {
                    DrawFlagEllipse(g, left_offset + offset + 20, top_offset + 20, Color.Red);
                }
                else
                {
                    Pen linePen = new Pen(Color.Green, 4);
                    if (cd.curr_points != null)
                    if (cd.curr_points.Count > 1)
                        for (int j = 1; j < cd.curr_points.Count; j++)
                            g.DrawLine(linePen, cd.curr_points[j - 1], cd.curr_points[j]);
                    //
                    Pen horPen = new Pen(Color.Red, 4);
                    if (cd.curr_hor != null)
                    for (int j = 0; j < cd.curr_hor.Count; j++)
                        g.DrawLine(horPen,
                            left_offset + offset, cd.curr_hor[j],
                            left_offset + offset + cd.img_width, cd.curr_hor[j]);
                }
                //
                if (cd.fl_slow_zone)
                    DrawFlagEllipse(g, left_offset + offset + 50, top_offset + 20, Color.Green);
                if (cd.fl_stop_zone)
                    DrawFlagEllipse(g, left_offset + offset + 80, top_offset + 20, Color.Blue);
                if (cd.fl_stop_mark)
                {
                    Color clr = Color.Cyan;
                    DrawFlagEllipse(g, left_offset + offset + 110, top_offset + 20, clr);
                    //
                    g.DrawString(cd.stop_mark_distance.ToString(), textFont, textBrush,
                        left_offset + offset + 140, top_offset + 20);
                }

                offset += cd.img_width + 10;

            }            
        }

        private void ProcessReceivedData(byte[] receivedBytes)
        {
            string fullData = BitConverter.ToString(receivedBytes).Replace("-", " ");
            //
            int img_offset = 0;
            int off = 2;

            int counter = BitConverter.ToInt16(receivedBytes, 0);

            for (int i = 0; i < 2; i++) 
            {
                int img_width = BitConverter.ToInt16(receivedBytes, off + 0);
                int img_height = BitConverter.ToInt16(receivedBytes, off + 2);
                int error_flag = BitConverter.ToInt16(receivedBytes, off + 4);
                //
                int max_points_count_idx = off + 6;
                byte max_points_count = receivedBytes[max_points_count_idx];
                byte points_count = receivedBytes[max_points_count_idx + 1];
                //
                int max_hor_count_idx = off + 8 + max_points_count * 4;
                byte max_hor_count = receivedBytes[max_hor_count_idx];
                byte hor_count = receivedBytes[max_hor_count_idx + 1];
                //
                int flag_idx = max_hor_count_idx + 2 + max_hor_count * 2;
                byte zone_flags = receivedBytes[flag_idx];
                byte stop_distance = receivedBytes[flag_idx + 1];
                //
                int pack_size =
                    2 + 2 + 2 +
                    2 + max_points_count * 4 +
                    2 + max_hor_count * 2 + 
                    2;
                //
                cam_data[i].img_width = img_width;
                cam_data[i].img_height = img_height;
                cam_data[i].error_flag = (error_flag > 0);
                //
                cam_data[i].max_points_count = max_points_count;
                cam_data[i].points_count = points_count;
                //
                cam_data[i].max_points_count = max_hor_count;
                cam_data[i].points_count = hor_count;
                //
                cam_data[i].fl_slow_zone = ((zone_flags & 4) > 0);
                cam_data[i].fl_stop_zone = ((zone_flags & 2) > 0);
                cam_data[i].fl_stop_mark = ((zone_flags & 1) > 0);
                cam_data[i].stop_mark_distance = stop_distance;
                //
                string points_str = "";
                int x, y;
                List<Point> curr_points = new List<Point>();
                curr_points.Add(new Point((img_width / 2) + left_offset + img_offset, img_height + top_offset));
                for (int j = 0; j < points_count; j++)
                {
                    x = BitConverter.ToInt16(receivedBytes, max_points_count_idx + 2 + j * 4);
                    y = BitConverter.ToInt16(receivedBytes, max_points_count_idx + 4 + j * 4);
                    points_str += string.Format("({0}; {1}) ", x, y);
                    x += left_offset + img_offset;
                    y += top_offset;
                    curr_points.Add(new Point(x, y));
                }
                cam_data[i].curr_points = curr_points;
                //
                List<int> curr_hor = new List<int>();
                for (int j = 0; j < hor_count; j++)
                {
                    y = BitConverter.ToInt16(receivedBytes, max_hor_count_idx + 2 + j * 2);
                    points_str += string.Format("({0}) ", y);
                    y += top_offset;
                    curr_hor.Add(y);
                }
                cam_data[i].curr_hor = curr_hor;
                //
                off += pack_size;
                img_offset += img_width + 10;
            }
            //
            lbMessages.Invoke((MethodInvoker)(() => {
                if (SHOW_MSGS)                
                    lbMessages.Items.Insert(0, fullData);
                //
                this.Invalidate();
            }));
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {            
            client.Close();
            //
            textFont.Dispose();
            textBrush.Dispose();
        }        

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {                

                // Отправка сообщения по UDP
                client.Send(reqData, reqDataLength, serverEndPoint);
                Console.WriteLine("Сообщение отправлено: {0}", reqMessage);

                IPEndPoint remoteEP = null;
                byte[] receivedBytes = client.Receive(ref remoteEP);
                ProcessReceivedData(receivedBytes);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: {0}", ex.Message);
            }
            finally
            {
                //
            }
        }
    }
    
}
