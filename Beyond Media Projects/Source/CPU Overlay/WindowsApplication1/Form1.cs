using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WindowsApplication1
{
    public partial class Form1 : Form
    {        
        protected System.Diagnostics.PerformanceCounter cpuCounter;
        protected System.Diagnostics.PerformanceCounter ramCounter; 
        private System.Timers.Timer timer = new System.Timers.Timer(500);
        // Window flags
        [DllImport("user32")]
        public static extern int ShowWindow(int hwnd, int nCmdShow);
        const Int32 HWND_TOPMOST = -1;
        const Int32 SWP_NOACTIVATE = 0x0010;
        const Int32 SW_SHOWNOACTIVATE = 4;
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(
            int hWnd,               // window handle
            int hWndInsertAfter,    // placement-order handle
            int X,                  // horizontal position
            int Y,                  // vertical position
            int cx,                 // width
            int cy,                 // height
            uint uFlags);           // window positioning flags
        public const int HWND_BOTTOM = 0x1;
        public const uint SWP_NOSIZE = 0x1;
        public const uint SWP_NOMOVE = 0x2;
        public const uint SWP_SHOWWINDOW = 0x40;

        public Form1()
        {
            InitializeComponent();

            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);

            cpuCounter = new System.Diagnostics.PerformanceCounter();

            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");

            ShowWindow(this.Handle.ToInt32(), SW_SHOWNOACTIVATE);
            SetWindowPos(this.Handle.ToInt32(), HWND_TOPMOST, 10, Screen.PrimaryScreen.Bounds.Height-40, this.Width, this.Height, SWP_NOACTIVATE);
            timer.Start();             
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {            
            timer.Stop();

            string text = "CPU: ";
            text += (int)cpuCounter.NextValue() +"%, ";
            text += ramCounter.NextValue() + "MB Free";
            label1.Text = text;

            timer.Start();
        }
    }
}