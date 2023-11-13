using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Graf
{
    public class Shape {
        public Color Color { get; set; } = Colors.Blue;

        public List<PointF> BezierPoints { get; set; } = new();
        public List<PointF> LinePoints { get; set; } = new();

        int segments = 192;

        public void DrawOnCanvas(ICanvas canvas) {

            if (LinePoints.Any()) {
                PathF path = new PathF();
                path.MoveTo(LinePoints.First());

                foreach (var item in LinePoints.Skip(1))
                {
                    path.LineTo(item);
                }
                canvas.FillColor = Color;
                canvas.StrokeColor = Color;
                canvas.StrokeSize = 2;
                canvas.DrawPath(path);
            }

            foreach (var item in BezierPoints) {

                canvas.FillColor = Colors.White;
                canvas.StrokeColor = Colors.Black;
                canvas.StrokeSize = 1;
                canvas.FillRectangle(new RectF(item.X - 1, item.Y - 1, 3, 3));
                canvas.DrawRectangle(new RectF(item.X - 1, item.Y - 1, 3, 3));
            }
        }

        public bool IsInside(PointF point) => true;

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

        public (float, int) DistanceToClick(PointF point)
        {
            
            float closestDistance = float.MaxValue;
            int closestPropertyIndex = 0;
            foreach (var _point in BezierPoints)
            {
                
                    float distance = CalculateDistance(point, _point);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPropertyIndex = BezierPoints.IndexOf(_point);
                    }
                
            }

            return (closestDistance, closestPropertyIndex);
        }

        private float CalculateDistance(PointF point1, PointF point2)
        {
            float dx = point1.X - point2.X;
            float dy = point1.Y - point2.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public void Calculate()
        {
            LinePoints.Clear();
            if (BezierPoints.Count < 2)
            {
                return;
            }

            for (float t = 0; t <= 1; t += 1.0f / segments)
            {
                PointF point = CalculateLinePoint(t, BezierPoints.ToArray());
                LinePoints.Add(point);
            }
        }

        private PointF CalculateLinePoint(float t, PointF[] points)
        {
            if (points.Length == 1)
            {
                return points[0];
            }

            PointF[] newPoints = new PointF[points.Length - 1];

            for (int i = 0; i < points.Length - 1; i++)
            {
                float x = (1 - t) * points[i].X + t * points[i + 1].X;
                float y = (1 - t) * points[i].Y + t * points[i + 1].Y;
                newPoints[i] = new PointF(x, y);
            }

            return CalculateLinePoint(t, newPoints);
        }
    }

    public class Shapes {
        
        public List<PointF> Circles = new List<PointF>();
    }

    public partial class MainPage : ContentPage
    {
        private Shape Shape;

        public MainPage()
        {
            Shape = new Shape();
            InitializeComponent();
        }

        bool isDraging;
        bool isScaling;

        bool subPrev;
        PointF? dragPrev;
        PointF? pivotHandle;

        Action<PointF> onClick;

        int dragingOn = 0;

        public static double CalculateAngleBetweenVectors(PointF vector1, PointF vector2)
        {
            double dotProduct = (vector1.X * vector2.X) + (vector1.Y * vector2.Y);
            
            double magnitude1 = Math.Sqrt((vector1.X * vector1.X) + (vector1.Y * vector1.Y));
            double magnitude2 = Math.Sqrt((vector2.X * vector2.X) + (vector2.Y * vector2.Y));

            double cosineTheta = dotProduct / (magnitude1 * magnitude2);
            
            double angleInRadians = Math.Acos(cosineTheta);

            double angleInDegrees = angleInRadians * (180.0 / Math.PI);
            if (vector2.Y < 0) {
                return  - angleInDegrees;
            }
            return angleInDegrees;
        }

        private void Canvas_DragInteraction(object sender, TouchEventArgs e)
        {
            if (isDraging)
            {
                var val = e.Touches[0];
                
                //var a = Shape.Points.ElementAt(dragingOn).Offset(val.X,val.Y);
                Shape.BezierPoints.RemoveAt(dragingOn);
                Shape.BezierPoints.Insert(dragingOn, val);
                Shape.Calculate();
                Canvas.Invalidate();
            }
            else if (dragPrev.HasValue) {
                
                var val2 = new PointF(e.Touches[0].X - dragPrev.Value.X, e.Touches[0].Y - dragPrev.Value.Y);

                
                    for (int i = 0; i < Shape.BezierPoints.Count; i++)
                    {
                        var a = Shape.BezierPoints.ElementAt(i).Offset(val2.X,val2.Y);
                        Shape.BezierPoints.RemoveAt(i);
                        Shape.BezierPoints.Insert(i, a);
                    }

                Shape.Calculate();
                Canvas.Invalidate();
            }
            dragPrev = e.Touches[0];
        }

        private void Canvas_StartInteraction(object sender, TouchEventArgs e)
        {
            onClick?.Invoke(e.Touches[0]);
            onClick = null;
  
            (float minDistance, int propInfo) = Shape.DistanceToClick(e.Touches[0]);
             

            if (minDistance < 5)
            {
                isDraging = true;
                subPrev = false;
                dragingOn = propInfo;
            }
            else 
            {
                isDraging = false;
                subPrev = false;
                dragingOn = -1;

            }

        }

        private void Canvas_EndInteraction(object sender, TouchEventArgs e)
        {
            isDraging = false;
            dragPrev = null;
            pivotHandle = null;
            dragingOn = -1;
            Shape.Calculate();
            UpdatePoints();
            Canvas.Invalidate();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            string json = JsonConvert.SerializeObject(Shape);

            var fileResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Save"
            });

            if (fileResult != null)
            {
                string filePath = fileResult.FullPath;
                File.WriteAllText(filePath, json);
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
                string json = File.ReadAllText(filePath);

                Shape = JsonConvert.DeserializeObject<Shape>(json);
                ((MyCanvas)Canvas.Drawable).shape = Shape;
                Canvas.Invalidate();
            }
        }

        private void SetLineStart(object sender, EventArgs e)
        {
            onClick = p =>
            {
                Shape.BezierPoints.Add(p);
                ((MyCanvas)Canvas.Drawable).shape = Shape;
                Shape.Calculate();
                Canvas.Invalidate();
                UpdatePoints();
            };
        }

        private void SetPivot(object sender, EventArgs e)
        {
            Shape.Calculate();
            Canvas.Invalidate();

        }

        private void UpdatePoints()
        {
            Points.Clear();
            for (int i = 0; i < Shape.BezierPoints.Count; i++)
            {
                var index = i;
                var p = Shape.BezierPoints[i];

                var ex = new Entry() { Text = p.X.ToString() };
                var ey = new Entry() { Text = p.Y.ToString() };
                var hsl = new HorizontalStackLayout
                {
                    
                    new Label() { Text = "X:", VerticalTextAlignment = TextAlignment.Center },ex,
                    new Label() { Text = "Y:", VerticalTextAlignment = TextAlignment.Center },ey,
                    new Button(){ Text = "Update", Command = new Command(_ => { Shape.BezierPoints.RemoveAt(index); Shape.BezierPoints.Insert(index,new PointF(float.Parse(ex.Text),float.Parse(ey.Text)));Shape.Calculate();Canvas.Invalidate();UpdatePoints(); }) },
                    new Button(){ Text = "Remove", Command = new Command(_ => { Shape.BezierPoints.RemoveAt(index); Shape.Calculate();Canvas.Invalidate();UpdatePoints(); }) }
                };
                hsl.WidthRequest = 285;
                Points.Add(hsl);
            }

            Points.Add(new Button() { Text = "Add", Command = new Command(_ => { Shape.BezierPoints.Add(new PointF(0,0)); Shape.Calculate(); Canvas.Invalidate(); UpdatePoints(); }) });
        }
    }
}