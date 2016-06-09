using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TakeScreenshot
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            
        }

        private void btnTakeScreen_Click(object sender, EventArgs e)
        {
            Bitmap memoryBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                             Screen.PrimaryScreen.Bounds.Height);
            Graphics memoryGraphics = Graphics.FromImage(memoryBitmap);
            memoryGraphics.CopyFromScreen(new Point(0, 0), new Point(0, 0), memoryBitmap.Size);
            pictureBox1.Image = memoryBitmap;
            memoryBitmap.Save("screenshot.bmp");
        }
    }
}
