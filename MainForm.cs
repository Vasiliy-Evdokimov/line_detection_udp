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

    public class CRC16_MODBUS
    {
        private readonly static ushort[] wCRCTable = {
            0X0000, 0XC0C1, 0XC181, 0X0140, 0XC301, 0X03C0, 0X0280, 0XC241, 0XC601, 0X06C0,
            0X0780, 0XC741, 0X0500, 0XC5C1, 0XC481, 0X0440, 0XCC01, 0X0CC0, 0X0D80, 0XCD41,
            0X0F00, 0XCFC1, 0XCE81, 0X0E40, 0X0A00, 0XCAC1, 0XCB81, 0X0B40, 0XC901, 0X09C0,
            0X0880, 0XC841, 0XD801, 0X18C0, 0X1980, 0XD941, 0X1B00, 0XDBC1, 0XDA81, 0X1A40,
            0X1E00, 0XDEC1, 0XDF81, 0X1F40, 0XDD01, 0X1DC0, 0X1C80, 0XDC41, 0X1400, 0XD4C1,
            0XD581, 0X1540, 0XD701, 0X17C0, 0X1680, 0XD641, 0XD201, 0X12C0, 0X1380, 0XD341,
            0X1100, 0XD1C1, 0XD081, 0X1040, 0XF001, 0X30C0, 0X3180, 0XF141, 0X3300, 0XF3C1,
            0XF281, 0X3240, 0X3600, 0XF6C1, 0XF781, 0X3740, 0XF501, 0X35C0, 0X3480, 0XF441,
            0X3C00, 0XFCC1, 0XFD81, 0X3D40, 0XFF01, 0X3FC0, 0X3E80, 0XFE41, 0XFA01, 0X3AC0,
            0X3B80, 0XFB41, 0X3900, 0XF9C1, 0XF881, 0X3840, 0X2800, 0XE8C1, 0XE981, 0X2940,
            0XEB01, 0X2BC0, 0X2A80, 0XEA41, 0XEE01, 0X2EC0, 0X2F80, 0XEF41, 0X2D00, 0XEDC1,
            0XEC81, 0X2C40, 0XE401, 0X24C0, 0X2580, 0XE541, 0X2700, 0XE7C1, 0XE681, 0X2640,
            0X2200, 0XE2C1, 0XE381, 0X2340, 0XE101, 0X21C0, 0X2080, 0XE041, 0XA001, 0X60C0,
            0X6180, 0XA141, 0X6300, 0XA3C1, 0XA281, 0X6240, 0X6600, 0XA6C1, 0XA781, 0X6740,
            0XA501, 0X65C0, 0X6480, 0XA441, 0X6C00, 0XACC1, 0XAD81, 0X6D40, 0XAF01, 0X6FC0,
            0X6E80, 0XAE41, 0XAA01, 0X6AC0, 0X6B80, 0XAB41, 0X6900, 0XA9C1, 0XA881, 0X6840,
            0X7800, 0XB8C1, 0XB981, 0X7940, 0XBB01, 0X7BC0, 0X7A80, 0XBA41, 0XBE01, 0X7EC0,
            0X7F80, 0XBF41, 0X7D00, 0XBDC1, 0XBC81, 0X7C40, 0XB401, 0X74C0, 0X7580, 0XB541,
            0X7700, 0XB7C1, 0XB681, 0X7640, 0X7200, 0XB2C1, 0XB381, 0X7340, 0XB101, 0X71C0,
            0X7080, 0XB041, 0X5000, 0X90C1, 0X9181, 0X5140, 0X9301, 0X53C0, 0X5280, 0X9241,
            0X9601, 0X56C0, 0X5780, 0X9741, 0X5500, 0X95C1, 0X9481, 0X5440, 0X9C01, 0X5CC0,
            0X5D80, 0X9D41, 0X5F00, 0X9FC1, 0X9E81, 0X5E40, 0X5A00, 0X9AC1, 0X9B81, 0X5B40,
            0X9901, 0X59C0, 0X5880, 0X9841, 0X8801, 0X48C0, 0X4980, 0X8941, 0X4B00, 0X8BC1,
            0X8A81, 0X4A40, 0X4E00, 0X8EC1, 0X8F81, 0X4F40, 0X8D01, 0X4DC0, 0X4C80, 0X8C41,
            0X4400, 0X84C1, 0X8581, 0X4540, 0X8701, 0X47C0, 0X4680, 0X8641, 0X8201, 0X42C0,
            0X4380, 0X8341, 0X4100, 0X81C1, 0X8081, 0X4040
        };

        public static byte[] fn_makeCRC16_byte(byte[] bytes)
        { // CRC-16/MODBUS
            int icrc = 0xFFFF;
            for (int i = 0; i < bytes.Length; i++)
            {
                icrc = (icrc >> 8) ^ wCRCTable[(icrc ^ bytes[i]) & 0xff];
            }
            byte[] ret = BitConverter.GetBytes(icrc);

            return ret;
        }
    }

    public struct CamData
    {
        public int img_width;
        public int img_height;
        public int error_flags;
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

    public partial class MainForm : Form
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

        const string serverIP = "192.168.1.5";    //  "192.168.49.22"
        const int serverPort = 8888;

        UdpClient client;
        IPEndPoint serverEndPoint;

        const string reqMessage = "camera";
        byte[] reqData;
        int reqDataLength;

        Font textFont;
        SolidBrush textBrush;
        SolidBrush camErrorBrush;
        SolidBrush camTimeoutBrush;

        public MainForm()
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
            camErrorBrush = new SolidBrush(Color.Red);
            camTimeoutBrush = new SolidBrush(Color.Magenta);
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
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
                if (cd.error_flags > 0) { 
                    if ((cd.error_flags & 1) > 0)
                    {
                        DrawFlagEllipse(g, left_offset + offset + 20, top_offset + 20, Color.Red);
                    }
                    if ((cd.error_flags & 4) > 0)
                    {
                        g.DrawString("Camera error!", textFont, camErrorBrush,
                            left_offset + offset + 170, top_offset + 50);
                    }
                    if ((cd.error_flags & 8) > 0)
                    {
                        g.DrawString("Camera timeout!", textFont, camTimeoutBrush,
                            left_offset + offset + 170, top_offset + 80);
                    }
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

            /* ToDo */
            byte[] buf = new byte[receivedBytes.Length - 2];
            int rcvd_crc = BitConverter.ToInt16(receivedBytes, receivedBytes.Length - 2);
            Array.Copy(receivedBytes, buf, buf.Length);
            int chck_crc = BitConverter.ToInt16(CRC16_MODBUS.fn_makeCRC16_byte(buf), 0);

            if (rcvd_crc != chck_crc) {
                lbMessages.Invoke((MethodInvoker)(() => {                    
                    lbMessages.Items.Insert(0, "CRC16 error!");                    
                }));
                return;
            }

            string fullData = BitConverter.ToString(receivedBytes).Replace("-", " ");
            //
            int img_offset = 0;
            int off = 2;

            int counter = BitConverter.ToInt16(receivedBytes, 0);

            int new_form_width = 0;
            int new_form_height = 0;

            for (int i = 0; i < 2; i++) 
            {
                int img_width = BitConverter.ToInt16(receivedBytes, off + 0);
                int img_height = BitConverter.ToInt16(receivedBytes, off + 2);
                int error_flags = BitConverter.ToInt16(receivedBytes, off + 4);
                //
                new_form_width += img_width;
                new_form_height += img_height;
                //
                int max_points_count_idx = off + 6;
                int max_points_count = BitConverter.ToInt16(receivedBytes, max_points_count_idx);
                int points_count = BitConverter.ToInt16(receivedBytes, max_points_count_idx + 2);
                //                
                int max_hor_count_idx = max_points_count_idx + 4 + max_points_count * 4;
                int max_hor_count = BitConverter.ToInt16(receivedBytes, max_hor_count_idx);
                int hor_count = BitConverter.ToInt16(receivedBytes, max_hor_count_idx + 2);
                //
                int flag_idx = max_hor_count_idx + 4 + max_hor_count * 2;
                int zone_flags = BitConverter.ToInt16(receivedBytes, flag_idx);
                int stop_distance = BitConverter.ToInt16(receivedBytes, flag_idx + 2);
                //
                int pack_size =
                    2 + 2 + 2 +
                    4 + max_points_count * 4 +
                    4 + max_hor_count * 2 + 
                    4;
                //
                cam_data[i].img_width = img_width;
                cam_data[i].img_height = img_height;
                //
                cam_data[i].error_flags = error_flags;
                //
                cam_data[i].max_points_count = max_points_count;
                cam_data[i].points_count = points_count;
                //
                cam_data[i].max_hor_count = max_hor_count;
                cam_data[i].hor_count = hor_count;
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
                    x = BitConverter.ToInt16(receivedBytes, max_points_count_idx + 4 + j * 4);
                    y = BitConverter.ToInt16(receivedBytes, max_points_count_idx + 6 + j * 4);
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
                    y = BitConverter.ToInt16(receivedBytes, max_hor_count_idx + 4 + j * 2);
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
            new_form_width += left_offset * 4;
            //
            if (this.Width < new_form_width)
                this.Width = new_form_width;
            if (this.Height < new_form_height)
                this.Height = new_form_height;
            //
            lbMessages.Invoke((MethodInvoker)(() => {
                if (SHOW_MSGS)                
                    lbMessages.Items.Insert(0, fullData);
                //
                this.Invalidate();
            }));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {            
            client.Close();
            //
            textFont.Dispose();
            textBrush.Dispose();
            camErrorBrush.Dispose();
            camTimeoutBrush.Dispose();
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
