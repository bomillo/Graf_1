using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.UI.Xaml.Controls.Primitives;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using Windows.Storage.Pickers;
using Color = Microsoft.Maui.Graphics.Color;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
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
            canvas.FillColor = Colors.White;
            canvas.StrokeColor = Colors.White;
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
        private Bitmap Bitmap = null;
        private Rectangle ShapeObj = new Rectangle() { Start = new PointF(10, 10) , End = new PointF(10 + 300* 1, 10 + 200 * 1) } ;

        int size = 3;

        public MainPage()
        {

            

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
            isDraging = true;
            dragPrev = e.Touches[0];
        }

        private void Canvas_EndInteraction(object sender, TouchEventArgs e)
        {
            isDraging = false;
            dragingOn = (dragingOn.closest, null);
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
#if WINDOWS
            var view = Canvas.Handler.PlatformView as Microsoft.Maui.Platform.PlatformTouchGraphicsView;
            view.PointerWheelChanged += (sender, e) =>
            {

                var point = e.GetCurrentPoint(sender as Microsoft.Maui.Platform.ContentPanel);
                var delta = point.Properties.MouseWheelDelta;

                size += delta > 0 ? 1 : -1;
                size = size <= 0 ? 1 : size;
                ShapeObj.End = new PointF(ShapeObj.Start.X + (int)(ShapeObj.Image.Width * size/6.0f), ShapeObj.Start.Y + (int)(ShapeObj.Image.Height * size / 6.0f));
                Canvas.Invalidate();
                e.Handled = true;

            };
#endif
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            
        


               
                    ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

                    System.Drawing.Imaging.Encoder myEncoder =
                        System.Drawing.Imaging.Encoder.Quality;

                    EncoderParameters myEncoderParameters = new EncoderParameters(1);

                    EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder,
                        int.Parse(Compression.Text));
                    myEncoderParameters.Param[0] = myEncoderParameter;
            using var stream = new MemoryStream();       
            Bitmap.Save(stream, jgpEncoder,
                        myEncoderParameters);
            await FileSaver.Default.SaveAsync("file.jpg", stream, CancellationToken.None);


            ImageCodecInfo GetEncoder(ImageFormat format)
            {
                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
                foreach (ImageCodecInfo codec in codecs)
                {
                    if (codec.FormatID == format.Guid)
                    {
                        return codec;
                    }
                }
                return null;
            }

        }

        private async void OnLoadClicked(object sender, EventArgs e)
        {
            var fileResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Load"
            });

            if (fileResult != null)
            {
                string filePath = fileResult.FullPath;
               
                var sw = new Stopwatch();
                sw.Start();
                //using (FileStream fs = File.OpenRead(@"C:\Users\mimik\Desktop\ppm-obrazy-testowe\ppm-test-07-p3-big.ppm"))
                try
                {
                    using (FileStream fs = File.OpenRead(filePath))
                    {
                        (ShapeObj.Image, Bitmap)= FileLoader.LoadPPM(fs);
                        ShapeObj.Start = new PointF(10, 10);
                        ShapeObj.End = new PointF(10 + ShapeObj.Image.Width, 10 + ShapeObj.Image.Height);
                        size = 6;
                    }
                }
                catch (Exception ex) {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
                sw.Stop();
                LoadedInText.Text = sw.Elapsed.ToString();
                Canvas.Invalidate();

            }
        }

        private void Canvas_MoveHoverInteraction(object sender, TouchEventArgs e)
        {
            if (Bitmap is null) {
                return;
            } 

           // try
            {

                int x = (int)((Bitmap.Width ) * float.Clamp(e.Touches[0].X - ShapeObj.Start.X, 0, ShapeObj.End.X - ShapeObj.Start.X) / (ShapeObj.End.X - ShapeObj.Start.X));
                int y = (int)((Bitmap.Height ) * float.Clamp(e.Touches[0].Y - ShapeObj.Start.Y, 0, ShapeObj.End.Y - ShapeObj.Start.Y) /( ShapeObj.End.Y - ShapeObj.Start.Y));


                ((MyCanvas2)c_11.Drawable).color = Bitmap.GetPixel(int.Clamp(x,    0, Bitmap.Width-1), (int)float.Clamp(y,    0, Bitmap.Height-1));

                c_11.Invalidate();
            }
           // catch { }


        }

        private void OnResetClicked(object sender, EventArgs e)
        {
            ShapeObj.Start = new PointF(10, 10);
            ShapeObj.End = new PointF(10 + ShapeObj.Image.Width, 10 + ShapeObj.Image.Height);
            size = 6;
            Canvas.Invalidate();
        }

    }
}