using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics.Win2D;
using Newtonsoft.Json;

using Color = Microsoft.Maui.Graphics.Color;
using PointF = Microsoft.Maui.Graphics.PointF;

namespace Graf
{
    public abstract class Shape {
        public Microsoft.Maui.Graphics.Color Color { get; set; }

        public abstract void DrawOnCanvas(ICanvas canvas);

        public abstract bool IsInside(PointF point);

        public void Move(PointF point)
        {
            Type type = GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == typeof(PointF))
                {
                    PointF currentPoint = (PointF)property.GetValue(this);
                    PointF newPoint = new PointF(currentPoint.X + point.X, currentPoint.Y + point.Y);
                    property.SetValue(this, newPoint);
                }
            }
        }
    }

    [Serializable]
    public class Rectangle : Shape
    {
        public PointF Start { get; set; }

        public PointF End { get; set; }

        public Microsoft.Maui.Graphics.IImage Image { get; set; }

        public override void DrawOnCanvas(ICanvas canvas)
        {
            canvas.FillColor = Color;
            canvas.StrokeColor = Color;
            canvas.StrokeSize = 0;
            if (Image is not null) { 
                canvas.DrawImage(Image, Start.X, Start.Y, End.X - Start.X, End.Y - Start.Y);
            }
        }

        public override bool IsInside(PointF point)
        {
            bool isInsideX = point.X >= Math.Min(Start.X, End.X) && point.X <= Math.Max(Start.X, End.X);
            bool isInsideY = point.Y >= Math.Min(Start.Y, End.Y) && point.Y <= Math.Max(Start.Y, End.Y);

            return isInsideX && isInsideY;
        }
    }

    public partial class MainPage : ContentPage
    {
        private Rectangle Shapes => ShapeObj;
        private Rectangle ShapeObj = new Rectangle() { Start = new PointF(10, 10) , End = new PointF(40, 40) } ;

        public MainPage()
        {
            using Bitmap bitmap = new Bitmap(30, 30, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            byte[] bytes = new byte[30*30*3];

            for (byte i = 0; i < 90; i++)
            {
                for (byte j = 0; j < 90; j++)
                {
                    bytes[i + j * 90] = i;
                }
            }

            try
            {

                IntPtr ptr = bitmapData.Scan0;
                System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ptr, bytes.Length);

            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            var stream = new MemoryStream();


            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            ShapeObj.Image = new W2DImageLoadingService().FromStream(stream);

            InitializeComponent();
            ((MyCanvas)Canvas.Drawable).shapes = Shapes;
            Canvas.Invalidate();
        }

        bool isDraging = false;

        PointF dragPrev;

        (Shape closest, MethodInfo propInfo) dragingOn = (null, null);

        private void Canvas_DragInteraction(object sender, TouchEventArgs e)
        {
            if (!isDraging) {
                return;
            }

            var  val = new PointF(e.Touches[0].X - dragPrev.X, e.Touches[0].Y - dragPrev.Y);
            Shapes.Move(val);
            Canvas.Invalidate();
            dragPrev = e.Touches[0];
        }

        private void Canvas_StartInteraction(object sender, TouchEventArgs e)
        {
            dragPrev = e.Touches[0];
        }

        private void Canvas_EndInteraction(object sender, TouchEventArgs e)
        {
            isDraging = false;
            dragingOn = (dragingOn.closest, null);
        }
    }
}