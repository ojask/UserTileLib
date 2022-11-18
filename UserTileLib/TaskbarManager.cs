namespace UserTileLib
{
    using System;
    using System.Threading;
    using System.Windows.Forms;

    public class TaskbarManager
    {
        private Control control;
        private TaskbarPosition currentTaskbarPos;
        private readonly IntPtr minimizeHwnd;
        private readonly IntPtr rebarHwnd;
        private int reservedWidth = 0x20;
        private bool spaceReserved;
        private readonly IntPtr taskbarHwnd = WinAPI.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
        private readonly IntPtr trayHwnd;

        private event EventHandler RebarResized;

        private event EventHandler TrayResized;

        public TaskbarManager()
        {
            this.rebarHwnd = WinAPI.FindWindowEx(this.taskbarHwnd, IntPtr.Zero, "ReBarWindow32", null);
            this.trayHwnd = WinAPI.FindWindowEx(this.taskbarHwnd, IntPtr.Zero, "TrayNotifyWnd", null);
            this.minimizeHwnd = WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "TrayShowDesktopButtonWClass", null);
            this.currentTaskbarPos = this.DetectTaskbarPos();
            this.TrayResized += new EventHandler(this.TaskbarManagerTrayResized);
            this.RebarResized += new EventHandler(this.TaskbarManagerRebarResized);
        }

        public void AddControl(UserControl control)
        {
            WinAPI.RECT trayRect = this.GetTrayRect();
            int num = trayRect.Right - trayRect.Left;
            WinAPI.RECT minimizeRect = this.GetMinimizeRect();
            int num2 = minimizeRect.Right - minimizeRect.Left;
            this.reservedWidth = control.Width + 5;
            control.BackColor = System.Drawing.Color.Transparent;
            control.Left = num - num2;
            WinAPI.SetParent(control.Handle, this.trayHwnd);
            this.control = control;
        }

        public void CheckTaskbar()
        {
            if (this.spaceReserved)
            {
                if (!this.CheckTrayWidth())
                {
                    this.currentTaskbarPos = this.DetectTaskbarPos();
                    if (this.currentTaskbarPos == TaskbarPosition.Bottom)
                    {
                        this.TrayResized(null, EventArgs.Empty);
                    }
                }
            }
            else
            {
                this.ReserveSpace(0x24);
            }
        }

        public bool CheckTrayWidth()
        {
            WinAPI.RECT rect3;
            WinAPI.RECT rect4;
            WinAPI.RECT rect5;
            WinAPI.RECT trayRect = this.GetTrayRect();
            int num = trayRect.Right - trayRect.Left;
            WinAPI.RECT minimizeRect = this.GetMinimizeRect();
            int num2 = minimizeRect.Right - minimizeRect.Left;
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "TrayClockWClass", null), out rect3);
            int num3 = rect3.Right - rect3.Left;
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "SysPager", null), out rect4);
            int num4 = rect4.Right - rect4.Left;
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "Button", null), out rect5);
            int num5 = rect5.Right - rect5.Left;
            if (num != ((((num2 + num3) + num4) + num5 + this.reservedWidth)))
            {
                return false;
            }
            return true;
        }

        public TaskbarPosition DetectTaskbarPos()
        {
            WinAPI.RECT rect;
            WinAPI.GetWindowRect(this.taskbarHwnd, out rect);
            if (((rect.Left == 0) && (rect.Bottom == SystemInformation.VirtualScreen.Bottom)) && (rect.Top == SystemInformation.WorkingArea.Top))
            {
                return TaskbarPosition.Left;
            }
            if (((rect.Left == 0) && (rect.Top == 0)) && (rect.Bottom != SystemInformation.VirtualScreen.Bottom))
            {
                return TaskbarPosition.Top;
            }
            if (((rect.Left != 0) && (rect.Top == 0)) && (rect.Bottom == SystemInformation.VirtualScreen.Bottom))
            {
                return TaskbarPosition.Right;
            }
            if (((rect.Left == 0) && (rect.Top != 0)) && (rect.Bottom == SystemInformation.VirtualScreen.Bottom))
            {
                return TaskbarPosition.Bottom;
            }
            return TaskbarPosition.Unknown;
        }

        public void Dispose()
        {
            if (this.spaceReserved)
            {
                this.FreeSpace();
            }
        }

        public void FreeSpace()
        {
            WinAPI.RECT rect4;
            WinAPI.RECT rect5;
            WinAPI.RECT rect6;
            this.reservedWidth = 0;
            WinAPI.RECT trayRect = this.GetTrayRect();
            WinAPI.RECT rebarRect = this.GetRebarRect();
            WinAPI.RECT minimizeRect = this.GetMinimizeRect();
            int cx = minimizeRect.Right - minimizeRect.Left;
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "TrayClockWClass", null), out rect4);
            int num2 = rect4.Right - rect4.Left;
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "SysPager", null), out rect5);
            int num3 = rect5.Right - rect5.Left;
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "Button", null), out rect6);
            int num4 = rect6.Right - rect6.Left;
            int num5 = ((cx + num2) + num3) + num4;
            WinAPI.SetWindowPos(this.trayHwnd, IntPtr.Zero, SystemInformation.WorkingArea.Right - num5, 0, num5, trayRect.Bottom - trayRect.Top, 0);
            WinAPI.SetWindowPos(this.rebarHwnd, IntPtr.Zero, rebarRect.Left, 0, (SystemInformation.WorkingArea.Right - num5) - rebarRect.Left, trayRect.Bottom - trayRect.Top, WinAPI.SetWindowPosFlags.SWP_NOMOVE);
            WinAPI.SetWindowPos(this.minimizeHwnd, IntPtr.Zero, num5 - cx, 0, cx, minimizeRect.Bottom - minimizeRect.Top, WinAPI.SetWindowPosFlags.SWP_NOSIZE);
        }

        public WinAPI.RECT GetMinimizeRect()
        {
            WinAPI.RECT rect;
            WinAPI.GetWindowRect(this.minimizeHwnd, out rect);
            return rect;
        }

        public WinAPI.RECT GetRebarRect()
        {
            WinAPI.RECT rect;
            WinAPI.GetWindowRect(this.rebarHwnd, out rect);
            return rect;
        }

        public WinAPI.RECT GetTrayRect()
        {
            WinAPI.RECT rect;
            WinAPI.GetWindowRect(this.trayHwnd, out rect);
            return rect;
        }

        public bool IsTaskbarSmall()
        {
            WinAPI.RECT rect;
            WinAPI.GetWindowRect(this.taskbarHwnd, out rect);
            int num = rect.Bottom - rect.Top;
            return (num < 0x23);
        }

        public void MoveTrayToLeft()
        {
            WinAPI.RECT rect4;
            WinAPI.RECT rect5;
            WinAPI.RECT rect6;
            WinAPI.RECT trayRect = this.GetTrayRect();
            WinAPI.RECT minimizeRect = this.GetMinimizeRect();
            int cx = minimizeRect.Right - minimizeRect.Left;
            WinAPI.RECT rebarRect = this.GetRebarRect();
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "TrayClockWClass", null), out rect4);
            int num2 = rect4.Right - rect4.Left;
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "SysPager", null), out rect5);
            int num3 = rect5.Right - rect5.Left;
            WinAPI.GetWindowRect(WinAPI.FindWindowEx(this.trayHwnd, IntPtr.Zero, "Button", null), out rect6);
            int num4 = rect6.Right - rect6.Left;
            WinAPI.SetWindowPos(this.trayHwnd, IntPtr.Zero, ((((SystemInformation.VirtualScreen.Right - cx) - this.reservedWidth) - num2) - num3) - num4, 0, (((cx + this.reservedWidth) + num2) + num3) + num4, trayRect.Bottom - trayRect.Top, 0);
            trayRect = this.GetTrayRect();
            int num5 = trayRect.Right - trayRect.Left;
            WinAPI.SetWindowPos(this.minimizeHwnd, IntPtr.Zero, num5 - cx, 0, cx, trayRect.Bottom - trayRect.Top, WinAPI.SetWindowPosFlags.SWP_NOSIZE);
            if (cx == 0)
            {
                cx = 15;
            }
            this.control.Left = ((num5 - this.control.Width) - cx) - 4;
        }

        public void PlaceMinimizeOnTaskbar()
        {
            WinAPI.SetParent(this.minimizeHwnd, this.taskbarHwnd);
            WinAPI.RECT minimizeRect = this.GetMinimizeRect();
            int cx = minimizeRect.Right - minimizeRect.Left;
            WinAPI.SetWindowPos(this.minimizeHwnd, IntPtr.Zero, SystemInformation.WorkingArea.Right - cx, 0, cx, minimizeRect.Bottom - minimizeRect.Top, WinAPI.SetWindowPosFlags.SWP_NOSIZE);
            WinAPI.RECT trayRect = this.GetTrayRect();
            int num2 = trayRect.Right - trayRect.Left;
            WinAPI.SetWindowPos(this.trayHwnd, IntPtr.Zero, ((SystemInformation.WorkingArea.Right - this.reservedWidth) - num2) - cx, 0, num2 - cx, trayRect.Bottom - trayRect.Top, 0);
        }

        public void PlaceMinimizeOnTray()
        {
            WinAPI.SetParent(this.minimizeHwnd, this.trayHwnd);
        }

        public void ReduceRebarWidth()
        {
            WinAPI.RECT rebarRect = this.GetRebarRect();
            WinAPI.RECT trayRect = this.GetTrayRect();
            int num = trayRect.Right - trayRect.Left;
            WinAPI.RECT minimizeRect = this.GetMinimizeRect();
            int num2 = minimizeRect.Right - minimizeRect.Left;
            int num3 = rebarRect.Right - rebarRect.Left;
            WinAPI.SetWindowPos(this.rebarHwnd, IntPtr.Zero, rebarRect.Left, 0, (((SystemInformation.WorkingArea.Right - num) - this.reservedWidth) - num2) - 3, rebarRect.Bottom - rebarRect.Top, WinAPI.SetWindowPosFlags.SWP_NOMOVE);
        }

        public void ReserveSpace(int width)
        {
            if (!this.spaceReserved)
            {
                this.reservedWidth = width;
                this.spaceReserved = true;
            }
        }

        private void TaskbarManagerRebarResized(object sender, EventArgs e)
        {
            this.ReduceRebarWidth();
        }

        private void TaskbarManagerTrayResized(object sender, EventArgs e)
        {
            this.MoveTrayToLeft();
            this.ReduceRebarWidth();
        }
    }
}

