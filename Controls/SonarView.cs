﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MissionPlanner.Utilities;
using ZedGraph;

namespace MissionPlanner.Controls
{
    public partial class SonarView : Form
    {
        private MAVLinkInterface mav;
        string data_txt = "";
        public SonarView(MAVLinkInterface mav)
        {
            InitializeComponent();
            this.mav = mav;
            ThemeManager.ApplyThemeTo(this);

            // Get a random number generator
            //Random rand = new Random();

            //GraphPane myPane = zedGraphControl1.GraphPane;
            //PointPairList list = new PointPairList();
            //for (int i = 0; i < 200; i++)
            //{
            //    double x = rand.NextDouble() * 20.0 + 1;
            //    double y = Math.Log(10.0 * (x - 1.0) + 1.0) * (rand.NextDouble() * 0.2 + 0.9);
            //    list.Add(x, y);
            //}

            //// Add the curve
            //LineItem myCurve = myPane.AddCurve("Performance", list, Color.White, SymbolType.Diamond);
            //// Don't display the line (This makes a scatter plot)
            //myCurve.Line.IsVisible = false;
            //zedGraphControl1.AxisChange();
        }

        

        private void but_start_Click(object sender, EventArgs e)
        {
            GraphPane myPane = zedGraphControl1.GraphPane;
            var line = new LineItem("ScanPoints", new RollingPointPairList(20), Color.White, SymbolType.Diamond);
            var sonar = new LineItem("Sonar", new PointPairList(), Color.Red, SymbolType.Circle);
            sonar.AddPoint(0, 0);
            line.Line.IsVisible = false;
            myPane.Title.Text = "ST-1000 Sonar";
            myPane.CurveList.Add(line);
            myPane.CurveList.Add(sonar);
            myPane.YAxis.Scale.IsReverse=true;
            myPane.YAxis.Title.Text = "Depth (m)";
            myPane.YAxis.Title.Text = "Distance (m)";
            myPane.XAxis.Scale.Max = 2; myPane.XAxis.Scale.Min = -2;
            myPane.YAxis.Scale.Max = 5; myPane.YAxis.Scale.Min = 0;
            myPane.XAxis.MajorGrid.Color = Color.DimGray;
            myPane.YAxis.MajorGrid.Color = Color.DimGray;
            myPane.XAxis.MajorGrid.IsVisible = true;
            myPane.YAxis.MajorGrid.IsVisible = true;


            var subscribeToPacket =
              mav.SubscribeToPacketType(MAVLink.MAVLINK_MSG_ID.SCANNING_SONAR,
                  message =>
                  {

                      MAVLink.mavlink_scanning_sonar_t S = (MAVLink.mavlink_scanning_sonar_t)message.data;
                      data_txt = $"{S.time_boot_ms},{S.range / 1000.0},{S.angle / 10.0},{S.roll / 10.0},{S.pitch / 10.0},{S.yaw / 10.0}";

                      data_txt += Environment.NewLine;

                     
                      double deg2rad = Math.PI / 180;
                      double Y = S.range / 1000.0 * Math.Cos((double)(S.angle / 10.0) * deg2rad);
                      double X = S.range / 1000.0 * Math.Sin((double)(S.angle / 10.0) * deg2rad);
                      line.AddPoint(X, Y);

                      double Total = 0;
                      for (int I = 0; I < line.NPts; I++) {Total += line.Points[I].Y; }
                      double AvgY = Total / line.NPts;
                      try
                      {
                          textBox1.Invoke(new Action(() => textBox1.AppendText(data_txt)));
                          textBoxDepth.Invoke(new Action(() => textBoxDepth.Text = AvgY.ToString("F1")));
                          textBoxRoll.Invoke(new Action(() => textBoxRoll.Text = (S.roll / 10.0).ToString("F1")));
                      }
                      catch(ObjectDisposedException ) { }
                      //zedGraphControl1.AxisChange();
                      zedGraphControl1.Invalidate();
                      return true;
                  });
           // timer.Enabled = true;
        }

        private void timer_Tick(object sender, EventArgs e)
        {

            textBox1.Invoke(new Action(() => textBox1.Text += data_txt));
        }

        private void SonarView_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer.Enabled = false;
        }

        private void Ready() { textBox1.Invoke(new Action(() => textBox1.Text +="Init OK!\r\n")); }

        private void but_init_Click(object sender, EventArgs e)
        {
            try
            {
                MainV2.comPort.doCommand((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent,
                    MAVLink.MAV_CMD.USER_1, 0, 0, 0, 0, 0, 0, 0, false);
                
            }
            catch
            {
                CustomMessageBox.Show(Strings.ErrorNoResponce, Strings.ERROR);
            }
        }

        private void splitContainer2_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
