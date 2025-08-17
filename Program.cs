using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

public class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

    [STAThread]
    public static void Main()
    {
        Application.Run(new WindowControllerForm());
    }
}

class WindowControllerForm : Form
{
    // Win32 API
    [DllImport("user32.dll")]
    private static extern int MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, int bRepaint);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    private const int SW_SHOWMINIMIZED = 2;
    private const int SW_SHOWMAXIMIZED = 3;
    private const int SW_RESTORE = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    // ウィンドウ調整の種類
    private enum WindowAction
    {
        SizeUp,
        SizeDown,
        MoveUp,
        MoveDown,
        MoveRight,
        MoveLeft
    }

    // UI
    private ListView windowListView = new ListView();
    private const int OFFSET = 50;

    public WindowControllerForm()
    {
        this.Load += WindowControllerForm_Load;
    }

    private void WindowControllerForm_Load(object sender, EventArgs e)
    {
        // フォーム設定
        this.Text = "ウインドウ操作";
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(100, 100);
        this.Size = new Size(875, 226);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = true;
        this.TopMost = true;

        // ListView 設定
        windowListView.Location = new Point(12, 12);
        windowListView.Size = new Size(709, 114);
        windowListView.View = View.Details;
        windowListView.FullRowSelect = true;
        windowListView.MultiSelect = false;
        windowListView.GridLines = true;
        windowListView.Columns.Add("ハンドル", 100);
        windowListView.Columns.Add("プロセス名", 150);
        windowListView.Columns.Add("タイトル名", 450);
        Controls.Add(windowListView);

        // ボタン配置
        AddButton("ウインドウ取得", new Point(12, 141), GetHandleButton_Click);
        AddButton("最大化", new Point(132, 141), MaximizeButton_Click);
        AddButton("最小化", new Point(252, 141), MinimizeButton_Click);
        AddButton("元のサイズ", new Point(372, 141), RestoreButton_Click);

        AddButton("大きくする", new Point(750, 12), (s, ev) => AdjustWindow(WindowAction.SizeUp), new Size(84, 28));
        AddButton("小さくする", new Point(750, 58), (s, ev) => AdjustWindow(WindowAction.SizeDown), new Size(84, 28));
        AddButton("↑", new Point(778, 103), (s, ev) => AdjustWindow(WindowAction.MoveUp), new Size(28, 23));
        AddButton("↓", new Point(778, 155), (s, ev) => AdjustWindow(WindowAction.MoveDown), new Size(28, 23));
        AddButton("→", new Point(806, 129), (s, ev) => AdjustWindow(WindowAction.MoveRight), new Size(28, 23));
        AddButton("←", new Point(750, 129), (s, ev) => AdjustWindow(WindowAction.MoveLeft), new Size(28, 23));
    }

    private void AddButton(string text, Point location, EventHandler handler, Size? size = null)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = size ?? new Size(112, 34)
        };
        button.Click += handler;
        Controls.Add(button);
    }

    private IntPtr? GetSelectedHandle()
    {
        if (windowListView.SelectedItems.Count == 0) return null;
        return new IntPtr(Convert.ToInt32(windowListView.SelectedItems[0].Text));
    }

    // ボタンクリック処理
    private void GetHandleButton_Click(object sender, EventArgs e)
    {
        windowListView.Items.Clear();
        windowListView.BeginUpdate();

        foreach (Process process in Process.GetProcesses())
        {
            if (process.MainWindowTitle.Length > 0)
            {
                string[] row =
                {
                    process.MainWindowHandle.ToString(),
                    process.ProcessName,
                    process.MainWindowTitle
                };
                windowListView.Items.Add(new ListViewItem(row));
            }
        }

        windowListView.EndUpdate();
        windowListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
    }

    private void MaximizeButton_Click(object sender, EventArgs e) => ChangeWindowState(SW_SHOWMAXIMIZED);
    private void MinimizeButton_Click(object sender, EventArgs e) => ChangeWindowState(SW_SHOWMINIMIZED);

    private void RestoreButton_Click(object sender, EventArgs e)
    {
        var handle = GetSelectedHandle();
        if (handle != null)
        {
            ShowWindow(handle.Value, SW_SHOWMAXIMIZED);
            ShowWindow(handle.Value, SW_RESTORE);
        }
    }

    private void ChangeWindowState(int state)
    {
        var handle = GetSelectedHandle();
        if (handle != null) ShowWindow(handle.Value, state);
    }

    private void AdjustWindow(WindowAction action)
    {
        var handle = GetSelectedHandle();
        if (handle == null) return;

        GetWindowRect(handle.Value, out RECT rect);
        int x = rect.left, y = rect.top;
        int width = rect.right - rect.left;
        int height = rect.bottom - rect.top;

        switch (action)
        {
            case WindowAction.SizeUp:
                MoveWindow(handle.Value, x, y, width + OFFSET, height + OFFSET, 1);
                break;
            case WindowAction.SizeDown:
                MoveWindow(handle.Value, x, y, width - OFFSET, height - OFFSET, 1);
                break;
            case WindowAction.MoveUp:
                MoveWindow(handle.Value, x, y - OFFSET, width, height, 1);
                break;
            case WindowAction.MoveDown:
                MoveWindow(handle.Value, x, y + OFFSET, width, height, 1);
                break;
            case WindowAction.MoveRight:
                MoveWindow(handle.Value, x + OFFSET * 2, y, width, height, 1);
                break;
            case WindowAction.MoveLeft:
                MoveWindow(handle.Value, x - OFFSET * 2, y, width, height, 1);
                break;
        }
    }
}
