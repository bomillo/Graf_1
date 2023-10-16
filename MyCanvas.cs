namespace Graf
{
    internal class MyCanvas : IDrawable
    {
        public Shape shapes;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeSize = 0;
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            shapes?.DrawOnCanvas(canvas);
        }

    }
}
