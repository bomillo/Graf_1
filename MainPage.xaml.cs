using Microsoft.Maui.Graphics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Graf
{
    public abstract class Shape {
        public Color Color { get; set; }

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

        public (float, PropertyInfo) DistanceToClick(PointF point)
        {
            Type type = GetType();
            PropertyInfo[] properties = type.GetProperties();

            PropertyInfo closestProperty = null;
            float closestDistance = float.MaxValue;

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == typeof(PointF))
                {
                    PointF propertyValue = (PointF)property.GetValue(this);
                    float distance = CalculateDistance(point, propertyValue);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestProperty = property;
                    }
                }
            }

            return (closestDistance, closestProperty);
        }

        private float CalculateDistance(PointF point1, PointF point2)
        {
            float dx = point1.X - point2.X;
            float dy = point1.Y - point2.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    [Serializable]
    public class Circle : Shape
    {
        [JsonPropertyName("Center")]
        public PointF Center { get; set; }

        [JsonPropertyName("RadiusPoint")]
        public PointF RadiusPoint { get; set; }

        public override void DrawOnCanvas(ICanvas canvas)
        {
            canvas.FillColor = Color;
            canvas.StrokeColor = Color;
            canvas.StrokeSize = 4;
            canvas.DrawCircle(Center, Center.Distance(RadiusPoint));

            canvas.FillColor = Colors.White;
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.FillRectangle(new RectF(Center.X - 1, Center.Y - 1, 3, 3));
            canvas.DrawRectangle(new RectF(Center.X - 1, Center.Y - 1, 3, 3));
            canvas.FillRectangle(new RectF(RadiusPoint.X - 1, RadiusPoint.Y - 1, 3, 3));
            canvas.DrawRectangle(new RectF(RadiusPoint.X - 1, RadiusPoint.Y - 1, 3, 3));
        }

        public override bool IsInside(PointF point)
        {
            // Calculate the radius of the circle using the distance formula
            float radius = (float)Math.Sqrt(Math.Pow(RadiusPoint.X - Center.X, 2) + Math.Pow(RadiusPoint.Y - Center.Y, 2));

            // Calculate the distance between the center of the circle and the given point
            float distance = (float)Math.Sqrt(Math.Pow(point.X - Center.X, 2) + Math.Pow(point.Y - Center.Y, 2));

            // Check if the distance is less than or equal to the radius
            return distance <= radius;
        }
    }

    [Serializable]
    public class Line : Shape
    {
        [JsonPropertyName("LineStart")]
        public PointF Start { get; set; }

        [JsonPropertyName("LineEnd")]
        public PointF End { get; set; }

        public override void DrawOnCanvas(ICanvas canvas)
        {
            canvas.FillColor = Color;
            canvas.StrokeColor = Color;
            canvas.StrokeSize = 4;
            canvas.DrawLine(Start, End);

            canvas.FillColor = Colors.White;
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.FillRectangle(new RectF(Start.X - 1, Start.Y - 1, 3, 3));
            canvas.DrawRectangle(new RectF(Start.X - 1, Start.Y - 1, 3, 3));
            canvas.FillRectangle(new RectF(End.X - 1, End.Y - 1, 3, 3));
            canvas.DrawRectangle(new RectF(End.X - 1, End.Y - 1, 3, 3));
        }

        public override bool IsInside(PointF point)
        {
            // Calculate the vector from the start point to the end point
            float lineVectorX = End.X - Start.X;
            float lineVectorY = End.Y - Start.Y;

            // Calculate the vector from the start point to the given point
            float pointVectorX = point.X - Start.X;
            float pointVectorY = point.Y - Start.Y;

            // Calculate the dot product of the two vectors
            float dotProduct = (lineVectorX * pointVectorX) + (lineVectorY * pointVectorY);

            // Calculate the squared length of the line vector
            float lineLengthSquared = (lineVectorX * lineVectorX) + (lineVectorY * lineVectorY);

            // Calculate the parameter along the line where the closest point is
            float t = Math.Max(0, Math.Min(1, dotProduct / lineLengthSquared));

            // Calculate the closest point on the line
            float closestPointX = Start.X + t * lineVectorX;
            float closestPointY = Start.Y + t * lineVectorY;

            // Calculate the distance from the given point to the closest point on the line
            float distance = (float)Math.Sqrt(Math.Pow(point.X - closestPointX, 2) + Math.Pow(point.Y - closestPointY, 2));

            // Check if the distance is less than or equal to 2 units (half the thickness of the line)
            return distance <= 2;
        }
    }

    [Serializable]
    public class Rectangle : Shape
    {
        [JsonPropertyName("RectStart")]
        public PointF Start { get; set; }

        [JsonPropertyName("RectEnd")]
        public PointF End { get; set; }

        public override void DrawOnCanvas(ICanvas canvas)
        {
            canvas.FillColor = Color;
            canvas.StrokeColor = Color;
            canvas.StrokeSize = 0;
            canvas.FillRectangle(new RectF(Start.X, Start.Y, End.X - Start.X, End.Y - Start.Y));

            canvas.FillColor = Colors.White;
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.FillRectangle(new RectF(Start.X - 1, Start.Y - 1, 3, 3));
            canvas.DrawRectangle(new RectF(Start.X - 1, Start.Y - 1, 3, 3));
            canvas.FillRectangle(new RectF(End.X - 1, End.Y - 1, 3, 3));
            canvas.DrawRectangle(new RectF(End.X - 1, End.Y - 1, 3, 3));
        }

        public override bool IsInside(PointF point)
        {
            bool isInsideX = point.X >= Math.Min(Start.X, End.X) && point.X <= Math.Max(Start.X, End.X);
            bool isInsideY = point.Y >= Math.Min(Start.Y, End.Y) && point.Y <= Math.Max(Start.Y, End.Y);

            return isInsideX && isInsideY;
        }
    }

    public class Shapes {
        [Newtonsoft.Json.JsonIgnore]
        public List<Shape> ShapesList => Circles.Cast<Shape>().Concat(Lines.Cast<Shape>()).Concat(Rectangles.Cast<Shape>()).ToList();

        public List<Circle> Circles = new List<Circle>();
        public List<Line> Lines = new List<Line>();
        public List<Rectangle> Rectangles = new List<Rectangle>();
    }

    public partial class MainPage : ContentPage
    {
        private List<Shape> Shapes => ShapesObj.ShapesList;

        private Shapes ShapesObj = new Shapes();

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnAddRectButtonClicked(object sender, EventArgs e)
        {
            ShapesObj.Rectangles.Add(new Rectangle() { 
                Start = new PointF(float.Parse(RectStartX.Text), float.Parse(RectStartY.Text)),
                End = new PointF(float.Parse(RectEndX.Text), float.Parse(RectEndY.Text)),
                Color = Color.FromRgba(RectColor.Text)
            });

            ((MyCanvas)Canvas.Drawable).shapes = Shapes;
            Canvas.Invalidate();
        }

        private void OnAddLineButtonClicked(object sender, EventArgs e)
        {
            ShapesObj.Lines.Add(new Line()
            {
                Start = new PointF(float.Parse(LineStartX.Text), float.Parse(LineStartY.Text)),
                End = new PointF(float.Parse(LineEndX.Text), float.Parse(LineEndY.Text)),
                Color = Color.FromRgba(LineColor.Text)
            });

            ((MyCanvas)Canvas.Drawable).shapes = Shapes;
            Canvas.Invalidate();
        }

        private void OnAddCircleButtonClicked(object sender, EventArgs e)
        {
            ShapesObj.Circles.Add(new Circle()
            {
                Center = new PointF(float.Parse(CircleCenterX.Text), float.Parse(CircleCenterY.Text)),
                RadiusPoint = new PointF(float.Parse(CircleCenterX.Text) + float.Parse(CircleRadius.Text), float.Parse(CircleCenterY.Text)),
                Color = Color.FromRgba(CircleColor.Text)
            });

            ((MyCanvas)Canvas.Drawable).shapes = Shapes;
            Canvas.Invalidate();
        }

        bool isDraging;

        bool subPrev;
        PointF dragPrev;

        Action<PointF> onClick;

        (Shape closest, MethodInfo propInfo) dragingOn = (null, null);

        private void Canvas_DragInteraction(object sender, TouchEventArgs e)
        {
            if (isDraging)
            {
                var val = e.Touches[0];
                if (subPrev) {
                    val = new PointF(e.Touches[0].X - dragPrev.X, e.Touches[0].Y - dragPrev.Y);
                }
                dragingOn.propInfo.Invoke(dragingOn.closest, new object[] { val });
                Canvas.Invalidate();
                SerialiedText.Text = JsonConvert.SerializeObject(dragingOn.closest);
            }
            dragPrev = e.Touches[0];
        }

        private void Canvas_StartInteraction(object sender, TouchEventArgs e)
        {
            onClick?.Invoke(e.Touches[0]);
            onClick = null;
            Shape closest = null;
            (float minDistance, PropertyInfo propInfo) = (float.MaxValue, null);

            foreach (var shape in Shapes)
            {
                var tmp = shape.DistanceToClick(e.Touches[0]);
                if (tmp.Item1 < minDistance)
                {
                    (minDistance, propInfo) = tmp;
                    closest = shape;
                }
            }

            if (minDistance < 5)
            {
                isDraging = true;
                subPrev = false;
                dragingOn = (closest, propInfo.SetMethod);
            }
            else 
            {
                var dragingShape = Shapes.FirstOrDefault(s => s.IsInside(e.Touches[0]));
                if (dragingShape is not null) {
                    subPrev = true;
                    isDraging = true;
                    dragingOn = (closest, typeof(Shape).GetMethod("Move"));
                    dragPrev = e.Touches[0];
                }
            }

            SerialiedText.Text = JsonConvert.SerializeObject(closest);
        }

        private void Canvas_EndInteraction(object sender, TouchEventArgs e)
        {
            isDraging = false;
            dragingOn = (dragingOn.closest, null);

            SerialiedText.Text = JsonConvert.SerializeObject(dragingOn.closest);
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            string json = JsonConvert.SerializeObject(ShapesObj);

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

                ShapesObj = JsonConvert.DeserializeObject<Shapes>(json);
                ((MyCanvas)Canvas.Drawable).shapes = Shapes;
                Canvas.Invalidate();
            }
        }

        private void SetCircleCenter(object sender, EventArgs e)
        {
            onClick = p =>
            {
                CircleCenterX.Text = p.X.ToString();
                CircleCenterY.Text = p.Y.ToString();
            };
        }

        private void SetCircleRadius(object sender, EventArgs e)
        {
            onClick = p =>
            {
                try {
                    var center = new PointF(float.Parse(CircleCenterX.Text), float.Parse(CircleCenterY.Text));
                    var radiusPoint = p;
                    CircleRadius.Text = p.Distance(center).ToString();
                } catch { }
            };
        }

        private void SetLineStart(object sender, EventArgs e)
        {
            onClick = p =>
            {
                LineStartX.Text = p.X.ToString();
                LineStartY.Text = p.Y.ToString();
            };
        }

        private void SetLineEnd(object sender, EventArgs e)
        {
            onClick = p =>
            {
                LineEndX.Text = p.X.ToString();
                LineEndY.Text = p.Y.ToString();
            };
        }

        private void SetRectStart(object sender, EventArgs e)
        {
            onClick = p =>
            {
                RectStartX.Text = p.X.ToString();
                RectStartY.Text = p.Y.ToString();
            };
        }

        private void SetRectEnd(object sender, EventArgs e)
        {
            onClick = p =>
            {
                RectEndX.Text = p.X.ToString();
                RectEndY.Text = p.Y.ToString();
            };
        }

        private void ApplyClicked(object sender, EventArgs e)
        {
            try
            {
                ShapesObj.Circles.Remove((Circle)dragingOn.closest);
                dragingOn.closest = JsonConvert.DeserializeObject<Circle>(SerialiedText.Text);
                ShapesObj.Circles.Add((Circle)dragingOn.closest);
                ((MyCanvas)Canvas.Drawable).shapes = Shapes;
                Canvas.Invalidate();
                return;
            }
            catch { }

            try
            {
                ShapesObj.Lines.Remove((Line)dragingOn.closest);
                dragingOn.closest = JsonConvert.DeserializeObject<Line>(SerialiedText.Text);
                ShapesObj.Lines.Add((Line)dragingOn.closest);
                ((MyCanvas)Canvas.Drawable).shapes = Shapes;
                Canvas.Invalidate();
                return;
            }
            catch { }

            try
            {
                ShapesObj.Rectangles.Remove((Rectangle)dragingOn.closest);
                dragingOn.closest = JsonConvert.DeserializeObject<Rectangle>(SerialiedText.Text);
                ShapesObj.Rectangles.Add((Rectangle)dragingOn.closest);
                ((MyCanvas)Canvas.Drawable).shapes = Shapes;
                Canvas.Invalidate();
                return;
            }
            catch { }


        }
    }
}