using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Markdig;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ready_shortcut
{

    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);


        private delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private KeyboardProc proc;
        private IntPtr hookId = IntPtr.Zero;


        private NotifyIcon trayIcon;


        public MainForm()
        {
            InitializeComponent();
            proc = HookCallback;

            // トレイアイコンの設定
            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", Exit)
            }),
                Visible = true
            };
        }
        protected override void OnLoad(EventArgs e)
        {
            // フォームを表示せずにトレイに格納
            Visible = false;
            ShowInTaskbar = false;

            base.OnLoad(e);

            hookId = SetHook(proc);
        }

        private void Exit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(hookId);
            base.OnFormClosing(e);
        }

        private IntPtr SetHook(KeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(13, proc, LoadLibrary("user32"), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)0x100)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool control = ModifierKeys.HasFlag(Keys.Control);
                bool shift = ModifierKeys.HasFlag(Keys.Shift);

                if (control && shift && vkCode == 0x41) // Ctrl + Shift + A
                {
                    IntPtr foregroundWindow = GetForegroundWindow();
                    string windowTitle = GetActiveWindowTitle(foregroundWindow);
                    string processName = GetActiveWindowProcessName(foregroundWindow);

                    for (int i = 0; i < Application.OpenForms.Count; i++)
                    {
                     //   Form f = Application.OpenForms[i];
                       // listBox1.Items.Add(f.Text);
                    }

                    Form webForm = new Form();
                    WebView2 web = new WebView2
                    {
                        Source = new Uri("htttp://localhost"),
                    };
                    web.NavigationCompleted += WebView_NavigationCompleted;
                    web.Height = 600;
                    web.Width = 600;

                    web.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);

                    webForm.Controls.Add(web);
                    webForm.Height = 600;
                    webForm.Width = 600;
                    webForm.Show();

                }
            }
            return CallNextHookEx(hookId, nCode, (int)wParam, lParam);
        }
        private string GetActiveWindowTitle(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private string GetActiveWindowProcessName(IntPtr hWnd)
        {
            uint processId;
            GetWindowThreadProcessId(hWnd, out processId);
            Process process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string html = Markdown.ToHtml(System.IO.File.ReadAllText("C:\\Users\\30046483\\Desktop\\Work\\workspace\\ready-shortcut\\ready-shortcut\\markdowns\\chrome.txt"));
            WebView2 web = (WebView2)sender;
            web.ExecuteScriptAsync($"document.documentElement.innerHTML = `{html}`;");
        }
    }
}
