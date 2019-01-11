using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MonitorRemoteListener {
    public class Program {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        private static int MONITOR_ON = -1;
        private static int MONITOR_OFF = 2;
        private static int MONITOR_STANBY = 1;

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        private static IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private static UInt32 WM_SYSCOMMAND = 0x0112;
        private static IntPtr SC_MONITORPOWER = new IntPtr(0xF170);

        private static System.Timers.Timer _idleTimer;

		[FlagsAttribute]
		public enum EXECUTION_STATE :uint
		{
		     ES_AWAYMODE_REQUIRED = 0x00000040,
		     ES_CONTINUOUS = 0x80000000,
		     ES_DISPLAY_REQUIRED = 0x00000002,
		     ES_SYSTEM_REQUIRED = 0x00000001
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto,SetLastError = true)]
		static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        public static void Main(string[] args) {
            if ((args.Length == 0) || (args[0] != "show")) {
                Hide();
            }

            int bytesRead;

            int port = 820;
            TcpListener server = new TcpListener(IPAddress.Any, port);

            try {
                server.Start();

                byte[] bytes = new Byte[256];
                string data = null;

                while (true) {
                    data = null;

                    TcpClient client = server.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();

                    while ((bytesRead = stream.Read(bytes, 0, bytes.Length)) != 0) {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytesRead).Trim(new char[] { '\n' });
                    }

                    client.Close();

                    if (data != null && data.Length > 0) {
                        Console.WriteLine(data);
                         
                        switch (data) {
                            case "on":
                                SetMonitorState(MONITOR_ON);
                                break;
                            case "standby":
                                SetMonitorState(MONITOR_STANBY);
                                break;
                            case "off":
                                SetMonitorState(MONITOR_OFF);
                                break;
                            case "shake":
                                MouseShake();
                                break;
                            case "slash":
                                MouseSlash();
                                break;
                        }
                    }
                }
            }
            catch (SocketException e) {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally {
                server.Stop();
            }
        }

        private static void Hide() {
            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, SW_MINIMIZE);
            ShowWindow(handle, SW_HIDE);
        }

        private static void PreventSleep(){
        	_idleTimer = new System.Timers.Timer(5 * 1000);
        	
        	_idleTimer.Elapsed += ResetSystemIdleTimer;

	        _idleTimer.AutoReset = true;
	        _idleTimer.Enabled = true;
        }

        //https://stackoverflow.com/questions/2577720/how-to-turn-on-monitor-after-wake-up-from-suspend-mode/42393472#42393472
        internal static void SetMonitorState(int state) {
            SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, (IntPtr)state);
        }

        internal static void MouseShake() {
            mouse_event(MOUSEEVENTF_MOVE, 0, 10, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_MOVE, 0, -10, 0, UIntPtr.Zero);
        }

        internal static void MouseSlash() {
            mouse_event(MOUSEEVENTF_ABSOLUTE, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_ABSOLUTE, 0, 65535, 65535, UIntPtr.Zero);
        }

        internal static void ResetSystemIdleTimer (Object source, ElapsedEventArgs e){
		    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
		}
    }
}