using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace GTA5_ProcessStopper
{
    public partial class Form1 : Form
    {
        private readonly int _delayInSeconds;
        
        public Form1()
        {
            _delayInSeconds = 10;
            InitializeComponent();
            HotKeyManager.RegisterHotKey(Keys.End, KeyModifiers.Control);
            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(button1_Click);
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var t =  new Thread( new ThreadStart(PauseProcess));
            t.Start();
            t.Join();
        }
        
        [Flags]
        private enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        private void PauseProcess()
        {
            var process = Process.GetProcessesByName("GTA5")
                ?.FirstOrDefault(x => x.ProcessName == "GTA5");
                
            if (process == null)
            {
                label1.Text = @"GTA 5 process not found";
                return;
            }
            var pid = process.Id;
            button1.Text = @"Stopping process";
            SuspendProcess(pid);
            Thread.Sleep(_delayInSeconds * 1000);
            ResumeProcess(pid);
            button1.Text = @"Ok";
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto,SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);
        
        private void SuspendProcess(int pid)
        {
            var process = Process.GetProcessById(pid);
            
            foreach (ProcessThread pT in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }
        }

        private void ResumeProcess(int pid)
        {
            var process = Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }
        public class HotKeyManager
        {
            public static event EventHandler<HotKeyEventArgs> HotKeyPressed;

            public static int RegisterHotKey(Keys key, KeyModifiers modifiers)
            {
                var id = System.Threading.Interlocked.Increment(ref _id);
                RegisterHotKey(_wnd.Handle, id, (uint)modifiers, (uint)key);
                return id;
            }

            public static bool UnregisterHotKey(int id)
            {
                return UnregisterHotKey(_wnd.Handle, id);
            }

            protected static void OnHotKeyPressed(HotKeyEventArgs e)
            {
                if (HotKeyManager.HotKeyPressed != null)
                {
                    HotKeyManager.HotKeyPressed(null, e);
                }
            }

            private static MessageWindow _wnd = new MessageWindow();

            private class MessageWindow : Form
            {
                protected override void WndProc(ref Message m)
                {
                    if (m.Msg == WM_HOTKEY)
                    {
                        HotKeyEventArgs e = new HotKeyEventArgs(m.LParam);
                        HotKeyManager.OnHotKeyPressed(e);
                    }

                    base.WndProc(ref m);
                }

                private const int WM_HOTKEY = 0x312;
            }

            [DllImport("user32")]
            private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

            [DllImport("user32")]
            private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

            private static int _id = 0;
        }
        
        public class HotKeyEventArgs : EventArgs
        {
            public readonly Keys Key;
            public readonly KeyModifiers Modifiers;

            public HotKeyEventArgs(Keys key, KeyModifiers modifiers)
            {
                this.Key = key;
                this.Modifiers = modifiers;
            }

            public HotKeyEventArgs(IntPtr hotKeyParam)
            {
                uint param = (uint)hotKeyParam.ToInt64();
                Key = (Keys)((param & 0xffff0000) >> 16);
                Modifiers = (KeyModifiers)(param & 0x0000ffff);
            }
        }
        
        [Flags]
        public enum KeyModifiers
        {
            Alt = 1,
            Control = 2,
            Shift = 4,
            Windows = 8,
            NoRepeat = 0x4000
        }
    }
}