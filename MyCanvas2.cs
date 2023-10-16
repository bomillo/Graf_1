using Font = Microsoft.Maui.Graphics.Font;

namespace Graf
{
    internal class MyCanvas2 : IDrawable
    {
        public System.Drawing.Color color;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeSize = 0;
            canvas.FillColor = Color.FromUint((uint)color.ToArgb());
            canvas.FillRectangle(dirtyRect);

            canvas.FontSize = 11;
            canvas.Font = Font.Default;
            canvas.FontColor = Colors.Black;
            canvas.SetShadow(new SizeF(0, 0), 0, Colors.White);
            canvas.DrawString($"R:{(byte)(color.R)} G:{(byte)(color.G )} B:{(byte)(color.B )}", 0, 300, HorizontalAlignment.Left);
        }

    }
}
