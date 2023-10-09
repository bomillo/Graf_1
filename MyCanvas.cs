namespace Graf
{
    internal class MyCanvas : IDrawable
    {
        public List<Shape> shapes;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeSize = 0;
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            /*
            canvas.FillColor = Colors.Red;
            canvas.FillRectangle(0, 0, 5, 5);
            canvas.FillRectangle(100, 0, 5, 5);
            canvas.FillRectangle(0, 100, 5, 5);
            canvas.FillRectangle(100, 100, 5, 5);
            */

            shapes?.ForEach(s => s.DrawOnCanvas(canvas));
        }

    }
}
