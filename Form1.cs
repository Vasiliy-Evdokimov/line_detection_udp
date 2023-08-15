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
        public int width;
        public int height;
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
        byte error_flag = 0;
        int img_width = 0, img_height = 0;
        List<Point> curr_points = new List<Point>();
        List<int> curr_hor = new List<int>();

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
            float x = 10;
            float y = 10;
            // Рисование текста            
            if (SHOW_MSGS)
                g.DrawString(curr_msg, font, Brushes.Black, x, y);
            //
            Pen borderPen = new Pen(Color.Yellow, 4);
            Brush borderBrush = new SolidBrush(Color.Black);            
            g.FillRectangle(borderBrush, left_offset, top_offset, img_width, img_height);
            //
            if (error_flag != 0) {
                Brush errorBrush = new SolidBrush(Color.Red);
                g.FillEllipse(errorBrush, left_offset + 20, top_offset + 20, 30, 30);
            } else {
                Pen linePen = new Pen(Color.Green, 4);
                if (curr_points.Count > 1)                
                    for (int i = 1; i < curr_points.Count; i++)
                        g.DrawLine(linePen, curr_points[i - 1], curr_points[i]);
                //
                Pen horPen = new Pen(Color.Red, 4);
                for (int i = 0; i < curr_hor.Count; i++)
                    g.DrawLine(horPen, 
                        left_offset, curr_hor[i], 
                        left_offset + img_width, curr_hor[i]);
            }
        }

        private void ProcessReceivedData(byte[] receivedBytes)
        {
            const int pack_size = 32;
            //
            string fullData = BitConverter.ToString(receivedBytes).Replace("-", " ");
            //
            int counter = BitConverter.ToInt16(receivedBytes, 0);
            img_width = BitConverter.ToInt16(receivedBytes, 2);
            img_height = BitConverter.ToInt16(receivedBytes, 4);
            error_flag = receivedBytes[6];
            byte count = receivedBytes[7];
            byte points_count = (byte)(count >> 4);
            byte hor_count = (byte)(count & 0xF);
            //
            string points_str = "";
            int x, y;
            curr_points.Clear();
            curr_points.Add(new Point((img_width / 2) + left_offset, img_height + top_offset));
            for (int i = 0; i < points_count; i++) {
                x = BitConverter.ToInt16(receivedBytes, 8 + i * 4);
                y = BitConverter.ToInt16(receivedBytes, 10 + i * 4);
                points_str += string.Format("({0}; {1}) ", x, y);
                x += left_offset;
                y += top_offset;
                curr_points.Add(new Point(x, y));
            }
            //
            curr_hor.Clear();
            for (int i = 0; i < hor_count; i++) {                
                y = BitConverter.ToInt16(receivedBytes, 24 + i * 2);
                points_str += string.Format("({0}) ", y);                
                y += top_offset;
                curr_hor.Add(y);
            }
            //            
            lbMessages.Invoke((MethodInvoker)(() => {
                if (SHOW_MSGS)
                {
                    lbMessages.Items.Insert(0, fullData);
                    curr_msg = string.Format("{0} {1} {2} {3} {4}", counter, img_width, img_height, points_count, points_str);
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
