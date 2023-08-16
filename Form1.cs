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

namespace udp_draw
{
    public struct CamData
    {
        public int counter;
        public int img_width;
        public int img_height;
        public bool error_flag;
        public int points_count;
        public int hor_count;
        public List<Point> curr_points;
        public List<int> curr_hor;        
    }

    public partial class Form1 : Form
    {
        const bool SHOW_MSGS = false;
        //
        const int Port = 8888;
        UdpClient udpServer;
        Queue<string> msgs = new Queue<string>();
        //
        const int top_offset = 50;
        const int left_offset = 10;
        //
        string curr_msg = "";

        CamData[] cam_data = new CamData[2];

        public Form1()
        {
            InitializeComponent();
            //
            udpServer = new UdpClient(Port);
            udpServer.BeginReceive(ReceiveCallback, null);
        }

        private async void Form1_Paint(object sender, PaintEventArgs e)
        {
            // Получить графический контекст формы
            Graphics g = e.Graphics;
            // Выполнить асинхронное рисование
            //await DrawAsync(g);
            Draw(g);
        }

        private async Task DrawAsync(Graphics g)
        {
            // Выполнить рисование на фоне потока
            await Task.Run(() => Draw(g));
        }

        private void Draw(Graphics g)
        {
            // Настройка шрифта
            Font font = new Font("Arial", 12);
            // Настройка координат текста
            // Рисование текста
            //  if (SHOW_MSGS)
            //      g.DrawString(curr_msg, font, Brushes.Black, 10, 10);
            //

            int offset = 0;

            for (int i = 0; i < cam_data.Length; i++) {

                CamData cd = cam_data[i];

                Pen borderPen = new Pen(Color.Yellow, 4);
                Brush borderBrush = new SolidBrush(Color.Black);
                g.FillRectangle(borderBrush, left_offset + offset, top_offset, cd.img_width, cd.img_height);
                //
                if (cd.error_flag)
                {
                    Brush errorBrush = new SolidBrush(Color.Red);
                    g.FillEllipse(errorBrush, left_offset + offset + 20, top_offset + 20, 30, 30);
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

                offset += cd.img_width + 10;

            }            
        }

        private void ProcessReceivedData(byte[] receivedBytes)
        {
            const int pack_size = 32;
            //
            string fullData = BitConverter.ToString(receivedBytes).Replace("-", " ");
            //
            int offset = 0;
            for (int i = 0; i < 2; i++)
            {
                int counter = BitConverter.ToInt16(receivedBytes, pack_size * i + 0);
                int img_width = BitConverter.ToInt16(receivedBytes, pack_size * i + 2);
                int img_height = BitConverter.ToInt16(receivedBytes, pack_size * i + 4);
                byte error_flag = receivedBytes[pack_size * i + 6];
                byte count = receivedBytes[pack_size * i + 7];
                byte points_count = (byte)(count >> 4);
                byte hor_count = (byte)(count & 0xF);
                //
                cam_data[i].counter = counter;
                cam_data[i].img_width = img_width;
                cam_data[i].img_height = img_height;
                cam_data[i].error_flag = (error_flag > 0);
                cam_data[i].points_count = points_count;
                cam_data[i].hor_count = hor_count;
                //
                string points_str = "";
                int x, y;
                List<Point> curr_points = new List<Point>();
                curr_points.Add(new Point((img_width / 2) + left_offset + offset, img_height + top_offset));
                for (int j = 0; j < points_count; j++)
                {
                    x = BitConverter.ToInt16(receivedBytes, pack_size * i + 8 + j * 4);
                    y = BitConverter.ToInt16(receivedBytes, pack_size * i + 10 + j * 4);
                    points_str += string.Format("({0}; {1}) ", x, y);
                    x += left_offset + offset;
                    y += top_offset;
                    curr_points.Add(new Point(x, y));
                }
                cam_data[i].curr_points = curr_points;
                //
                List<int> curr_hor = new List<int>();
                for (int j = 0; j < hor_count; j++)
                {
                    y = BitConverter.ToInt16(receivedBytes, pack_size * i + 24 + j * 2);
                    points_str += string.Format("({0}) ", y);
                    y += top_offset;
                    curr_hor.Add(y);
                }
                cam_data[i].curr_hor = curr_hor;
                //
                offset += img_width + 10;
            }
            //            
            lbMessages.Invoke((MethodInvoker)(() => {
                if (SHOW_MSGS)
                {
                    lbMessages.Items.Insert(0, fullData);
                    //curr_msg = string.Format("{0} {1} {2} {3} {4}", counter, img_width, img_height, points_count, points_str);
                }
                this.Invalidate();
            }));
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            udpServer.Close();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, Port);
                byte[] receivedBytes = udpServer.EndReceive(ar, ref clientEndPoint); 

                //msgs.Enqueue(receivedMessage);
                ProcessReceivedData(receivedBytes);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Продолжаем ожидать новые сообщения
            udpServer.BeginReceive(ReceiveCallback, null);
        }
        
    }
    
}
