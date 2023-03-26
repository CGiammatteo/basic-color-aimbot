using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace BOTTIEST_BOT
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        private const int MOUSEEVENTF_MOVE = 0x0001;

        const int BUTTON = 0x20; //space

        static Color targetColor = Color.FromArgb(255,200, 0, 0); //bright-ish red
        static int colorThreshold = 30;

        static bool done = false;

        static bool scanningEnabled = false;

        static void Main(string[] args)
        {
            Console.Title = "BOTTY BOT AIM ZONE";
            Console.WriteLine("Program started, press SPACE to toggle");
            while (true)
            {
                if ((GetAsyncKeyState(BUTTON) & 0x8000) != 0)
                {
                    scanningEnabled = !scanningEnabled;
                    Console.WriteLine(scanningEnabled ? "Scanning enabled." : "Scanning disabled.");
                    Thread.Sleep(300); // Simple debounce for mouse button press
                }

                if (scanningEnabled)
                {
                    Point? targetPosition = FindCloseColorPositionOnScreen(targetColor, colorThreshold);

                    if (targetPosition.HasValue && done == false)
                    {
                        Console.WriteLine($"A color close to {targetColor} was found at {targetPosition.Value}.");
                        Move(targetPosition.Value.X - 40, targetPosition.Value.Y - 30);
                    }
                }

                Thread.Sleep(1);
            }
        }

        static Point? FindCloseColorPositionOnScreen(Color targetColor, int colorThreshold)
        {
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;

            int scanWidth = 100;
            int scanHeight = 100;

            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            int startX = (screenBounds.Width - scanWidth) / 2;
            int startY = (screenBounds.Height - scanHeight) / 2;

            Rectangle captureArea = new Rectangle(startX, startY, scanWidth, scanHeight);

            Bitmap screenshot = new Bitmap(captureArea.Width, captureArea.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(captureArea.Left, captureArea.Top, 0, 0, captureArea.Size, CopyPixelOperation.SourceCopy);
                using (Graphics p = Graphics.FromHwnd(IntPtr.Zero))
                {
                    p.CopyFromScreen(captureArea.Left, captureArea.Top, (screenWidth - captureArea.Width) / 2, screenHeight - captureArea.Height - 50, captureArea.Size, CopyPixelOperation.SourceCopy);
                }
            }

            //DisplayCapture(screenshot);

            for (int x = 0; x < screenshot.Width; x++)
            {
                for (int y = 0; y < screenshot.Height; y++)
                {
                    Color pixelColor = screenshot.GetPixel(x, y);
                    if (IsColorClose(targetColor, pixelColor, colorThreshold))
                    {
                        return new Point(x, y);
                    }
                }
            }

            return null;
        }

        static bool IsColorClose(Color targetColor, Color pixelColor, int colorThreshold)
        {
            int rDiff = targetColor.R - pixelColor.R;
            int gDiff = targetColor.G - pixelColor.G;
            int bDiff = targetColor.B - pixelColor.B;
            int colorDistance = rDiff * rDiff + gDiff * gDiff + bDiff * bDiff;

            return colorDistance <= colorThreshold * colorThreshold;
        }

        public static void Move(int xDelta, int yDelta, int duration = 60, int steps = 100)
        {
            Point currentMousePosition = Cursor.Position;
            int startX = currentMousePosition.X;
            int startY = currentMousePosition.Y;

            done = true;
            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                int newX = (int)(startX + xDelta * progress);
                int newY = (int)(startY + yDelta * progress);

                int xMove = newX - currentMousePosition.X;
                int yMove = newY - currentMousePosition.Y;

                mouse_event(MOUSEEVENTF_MOVE, xMove, yMove, 0, 0);

                currentMousePosition = new Point(newX, newY);
                Thread.Sleep(duration / steps);
            } 
            done = false;
        }
    }
}