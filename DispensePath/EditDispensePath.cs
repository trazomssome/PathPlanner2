using ImageGlass;
//using Newtonsoft.Json;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Xml.Serialization;
using Cursors = System.Windows.Forms.Cursors;

namespace DispensePath
{
    public static class ListExtensions
    {
        public static bool checkOrder(this List<GraphicPolyLine> list)
        {
            list.Sort((list1, list2) => list1.Order.CompareTo(list2.Order));
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (list[i].Order == list[i + 1].Order || list[i].Order == 0)
                    return false;
            }
            return true;
        }
    }

    public class Point3D
    {
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Z { get; set; } = 0;

        public Point3D()
        {
        }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
    public class Plane
    {
        private double A { get; set; } = 0;
        private double B { get; set; } = 0;
        private double C { get; set; } = 0;
        private double D { get; set; } = 0;

        public Point3D p1 { get; set; } = new Point3D();
        public Point3D p2 { get; set; } = new Point3D();
        public Point3D p3 { get; set; } = new Point3D();

        public Plane()
        {
        }

        public double GetZ(double x, double y)
        {
            return -(A * x + B * y + D) / C;
        }
        public double AdjustHeight(Point3D result, double master)
        {

            result.Z = GetZ(result.X, result.Y);
            double offsetZ = master - result.Z;
            return offsetZ;
        }
    }

    // [2025-8-21]
    public class Recipe_DispensingPoints
    {
        public List<DispensePointNode> Nodes { get; set; } = new List<DispensePointNode>();
        public string BackgroundImagePath { get; set; } = "";
        public double PixelSizeX { get; set; } = 0.01;
        public double PixelSizeY { get; set; } = 0.01;

        // [2025-8-22]
        public List<DispensePolyline> Polylines { get; set; } = new List<DispensePolyline>();
        //public List<DispenseLine> Lines { get; set; } = new List<DispenseLine>(); // [2025-8-26] No Use


        public Recipe_DispensingPoints()
        {
        }

        public Recipe_DispensingPoints Load(string recipeName)
        {
            string path = $"D:\\test\\DispensingPath.json";

            Recipe_DispensingPoints newData = null;

            if (File.Exists(path))
            {
                try
                {
                    newData = JsonSerializer.Deserialize<Recipe_DispensingPoints>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                }

                if (newData != null) return newData;
            }

            newData = new Recipe_DispensingPoints();
            return newData;
        }

        public Recipe_DispensingPoints LoadAs(string path)
        {
            Recipe_DispensingPoints newData = null;

            if (File.Exists(path))
            {
                try
                {
                    newData = JsonSerializer.Deserialize<Recipe_DispensingPoints>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                }

                if (newData != null) return newData;
            }

            newData = new Recipe_DispensingPoints();
            return newData;
        }

        public void Save(string recipeName)
        {
            string path = $"D:\\test\\DispensingPath.json";
            //Directory.CreateDirectory($"D:\\[SGMachineFDP]\\Recipe\\_config_recipe\\");

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };

            string currRecipe;

            try
            {
                currRecipe = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, currRecipe);
            }
            catch (JsonException ex)
            {
                options.IgnoreNullValues = true;
                currRecipe = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, currRecipe);

                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }

        public void SaveAs(string path)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };

            string currRecipe;

            try
            {
                currRecipe = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, currRecipe);
            }
            catch (JsonException ex)
            {
                options.IgnoreNullValues = true;
                currRecipe = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, currRecipe);

                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
    }

    public class Recipe_GraphicObject
    {
        public bool bUseExBackground { set; get; } = true;

        // 동작 정보 저장
        public List<ActionInfo> actionInfos { get; set; } = new List<ActionInfo>();
        public List<GraphicPolyLine> GraphicList { get; set; } = new List<GraphicPolyLine>();

        public int nBackGroundWidth { set; get; } = 1;
        public int nBackGroundHeight { set; get; } = 1;

        Plane LVDTPlane = new Plane();

        public Recipe_GraphicObject()
        {
        }

        public Recipe_GraphicObject Clone()
        {
            Recipe_GraphicObject to = new Recipe_GraphicObject();

            for (int i = 0; i < actionInfos.Count; i++)
            {
                to.actionInfos.Add(actionInfos[i]);
            }

            return to;
        }

    }

    public class CParameter_DefectsMap
    {
        public enum MAP_ORIGIN : int
        { LT = 0, LB, RT, RB, CENTER };

        [Category("00. Parameter"), Description(""), DisplayName("Draw Scale")]
        public float DrawingScale { get; set; } = 1.0F;

        [Category("01. Map"), Description(""), DisplayName("Actual Width (mm)")]
        public int ActualWidth_mm { get; set; } = 5000;

        [Category("01. Map"), Description(""), DisplayName("Actual Height (mm)")]
        public int ActualHeight_mm { get; set; } = 5000;

        [Category("01. Map"), Description(""), DisplayName("Origin")]
        public MAP_ORIGIN Origin { get; set; } = MAP_ORIGIN.LT;

        [Category("02. Grid"), Description(""), DisplayName("Columns"), Browsable(false)]
        public int GridColumns { get; set; } = 10;

        [Category("02. Grid"), Description(""), DisplayName("Rows"), Browsable(false)]
        public int GridRows { get; set; } = 10;

        [Category("02. Grid"), Description(""), DisplayName("Line Thickness (px)")]
        public int GridThickness { get; set; } = 10;

        [Category("03. Layout"), Description(""), DisplayName("Margin X (px)")]
        public int MarginX { get; set; } = 50;

        [Category("03. Layout"), Description(""), DisplayName("Margin Y (px)")]
        public int MarginY { get; set; } = 50;

        [Category("04. Data"), Description(""), DisplayName("Grids")]
        public List<PointF> defects { get; set; } = new List<PointF>();

        public List<Rectangle> GridRegion { get; set; } = new List<Rectangle>();
        public List<string> GridNo { get; set; } = new List<string>();

        #region CONFIG BY XML

        public CParameter_DefectsMap LoadConfig(string strRecipeName, string strPosition)
        {
            string savePath = $"{Application.StartupPath}\\RECIPE\\{strRecipeName}\\DefectsMap_{strPosition}.xml";
            CParameter_DefectsMap newData = null;

            if (File.Exists(savePath))
            {
                newData = SerializeHelper.FromXmlFile<CParameter_DefectsMap>(savePath);
                if (newData != null)
                    return newData;
            }

            newData = new CParameter_DefectsMap();
            newData.SaveConfig(strRecipeName, strPosition);

            return newData;
        }

        public void SaveConfig(string strRecipeName, string strPosition)
        {
            string savePath = $"{Application.StartupPath}\\RECIPE\\{strRecipeName}\\DefectsMap_{strPosition}.xml";
            SerializeHelper.ToXmlFile(savePath, this);
        }

        #endregion CONFIG BY XML
    }

    public class GraphicObject
    {
        public string Name { get; set; }
        public bool Selected { get; set; } = false;
    }

    public class ActionInfo
    {
        public PointF Position { get; set; } = new PointF();

        public ActionInfo()
        {
            Position = new PointF();
        }

        public ActionInfo(PointF position)
        {
            Position = position;
        }
    }

    [Serializable]
    public class DispensePointNode
    {
        public PointF Position { get; set; } = new PointF();
        public double OffsetZ { get; set; } = 0;
        public double Speed { get; set; } = 50; //mm/s
        public bool Use { get; set; } = true;

        [JsonIgnore]
        public bool IsComplete = false;

        public DispensePointNode()
        { }

        public DispensePointNode(PointF pos)
        {
            Position = pos;
        }

        public DispensePointNode(PointF pos, double offset_z, double speed, bool use)
        {
            Position = pos;
            OffsetZ = offset_z;
            Speed = speed;
            Use = use;
        }

        public DispensePointNode Clone(bool isPositionChange = true)
        {
            DispensePointNode clone = new DispensePointNode();

            if (isPositionChange) clone.Position = new PointF(Position.X + 50, Position.Y + 50);
            else clone.Position = new PointF(Position.X, Position.Y);
            clone.Speed = Speed;
            clone.Use = Use;
            clone.OffsetZ = OffsetZ;

            return clone;
        }
    }

    public class  GraphicPolyLine : GraphicObject
    {
        public int Order { get; set; }
        public int SelectedPoint { get; set; } = 0;

        public string szLVDTOffSet { get; set; } = "";
        public List<DispensePointNode> Nodes { get; set; } = new List<DispensePointNode>();
        
        public void Move(float offsetX, float offsetY)
        {
            foreach (var pos in Nodes)
            {
                pos.Position = new PointF(pos.Position.X + offsetX, pos.Position.Y + offsetY);
            }
        }

        public void HorzFlip()
        {
            RectangleF temp = GetBoundary();
            PointF center = temp.Center();
            foreach (var pos in Nodes)
            {
                pos.Position = new PointF(center.X * 2 - pos.Position.X, pos.Position.Y);
            }
        }

        public void VertFlip()
        {
            RectangleF temp = GetBoundary();
            PointF center = temp.Center();
            foreach (var pos in Nodes)
            {
                pos.Position = new PointF(pos.Position.X, center.Y * 2 - pos.Position.Y);
            }
        }

        public Color LineColor { get; set; }

        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float OffsetW { get; set; }

        public RectangleF GetBoundary()
        {
            if (Nodes == null || Nodes.Count < 2) return new RectangleF();

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (DispensePointNode node in Nodes)
            {
                PointF point = node.Position;

                if (point.X < minX)
                    minX = point.X;
                if (point.X > maxX)
                    maxX = point.X;
                if (point.Y < minY)
                    minY = point.Y;
                if (point.Y > maxY)
                    maxY = point.Y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            return new RectangleF(minX - 10, minY - 10, width + 20, height + 20);
        }

        public bool HitTest(PointF pt)
        {
            return GetBoundary().Contains(pt);
        }

        public void Copy(GraphicPolyLine copy)
        {
            copy.Order = Order;
            copy.SelectedPoint = SelectedPoint;

            copy.Nodes = new List<DispensePointNode>(Nodes);

            copy.OffsetX = OffsetX;
            copy.OffsetY = OffsetY;
            copy.OffsetW = OffsetW;
            copy.Name = Name;
            copy.Selected = Selected;
        }

        public GraphicPolyLine Clone()
        {
            GraphicPolyLine clone = new GraphicPolyLine();

            foreach (var item in Nodes)
            {
                clone.Nodes.Add(item.Clone(false));
            }
            //clone.Nodes = new List<DispensePointNode>(Nodes);
            clone.Order = Order;
            clone.LineColor = this.LineColor;

            clone.OffsetX = OffsetX;
            clone.OffsetY = OffsetY;
            clone.OffsetW = OffsetW;
            clone.Name = Name;
            clone.szLVDTOffSet = szLVDTOffSet;
            return clone;
        }
    }

    public class GraphicRectangle : GraphicObject
    {
        public int Order { get; set; }
        public int SelectedPoint { get; set; } = 0;
        public List<DispensePointNode> Nodes { get; set; } = new List<DispensePointNode>();

        public void Move(float offsetX, float offsetY)
        {
            foreach (var pos in Nodes)
            {
                pos.Position = new PointF(pos.Position.X + offsetX, pos.Position.Y + offsetY);
            }
        }

        public void HorzFlip()
        {
            RectangleF temp = GetBoundary();
            PointF center = temp.Center();
            foreach (var pos in Nodes)
            {
                pos.Position = new PointF(center.X * 2 - pos.Position.X, pos.Position.Y);
            }
        }

        public void VertFlip()
        {
            RectangleF temp = GetBoundary();
            PointF center = temp.Center();
            foreach (var pos in Nodes)
            {
                pos.Position = new PointF(pos.Position.X, center.Y * 2 - pos.Position.Y);
            }
        }

        public Color LineColor { get; set; }

        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float OffsetW { get; set; }

        public RectangleF GetBoundary()
        {
            if (Nodes == null || Nodes.Count < 2) return new RectangleF();

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (DispensePointNode node in Nodes)
            {
                PointF point = node.Position;

                if (point.X < minX)
                    minX = point.X;
                if (point.X > maxX)
                    maxX = point.X;
                if (point.Y < minY)
                    minY = point.Y;
                if (point.Y > maxY)
                    maxY = point.Y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            return new RectangleF(minX - 10, minY - 10, width + 20, height + 20);
        }

        public bool HitTest(PointF pt)
        {
            return GetBoundary().Contains(pt);
        }

        public void Copy(GraphicPolyLine copy)
        {
            copy.Order = Order;
            copy.SelectedPoint = SelectedPoint;

            copy.Nodes = new List<DispensePointNode>(Nodes);

            copy.OffsetX = OffsetX;
            copy.OffsetY = OffsetY;
            copy.OffsetW = OffsetW;
            copy.Name = Name;
            copy.Selected = Selected;
        }

        public GraphicPolyLine Clone()
        {
            GraphicPolyLine clone = new GraphicPolyLine();

            foreach (var item in Nodes)
            {
                clone.Nodes.Add(item.Clone(false));
            }
            //clone.Nodes = new List<DispensePointNode>(Nodes);
            clone.Order = Order;
            clone.LineColor = this.LineColor;

            clone.OffsetX = OffsetX;
            clone.OffsetY = OffsetY;
            clone.OffsetW = OffsetW;
            clone.Name = Name;

            return clone;
        }
    }

    public class GraphicEventArgs : EventArgs
    {
        public GraphicPolyLine Graphic { get; set; }

        public GraphicEventArgs(GraphicPolyLine g)
        {
            Graphic = g;
        }
    }

    public class EvenMousePosArgs : EventArgs
    {
        public Point mousePos { get; set; }

        public EvenMousePosArgs(Point pos)
        {
            mousePos = pos;
        }
    }

    public class PositionEventArgs : EventArgs
    {
        public DispensePointNode Node { get; set; }

        public PositionEventArgs(DispensePointNode node)
        {
            Node = node;
        }
    }

    public class DispenseLine
    {
        [Browsable(false)] public bool IsSelected { get; set; } = true;
        public PointF P0 { get; set; }
        public PointF P1 { get; set; }
        public bool Use { get; set; } = true;
        public int Order { get; set; }
        public float StrokeWidth { get; set; } = 2f;
        public Color Stroke { get; set; } = Color.Orange;
        public IEnumerable<(PointF, PointF)> Segments()
        {
            yield return (P0, P1);
        }
    }

    public class DispensePolyline
    {
        [Browsable(false)] public bool IsSelected { get; set; } = true; // [2025-9-4]
        public List<PointF> Points { get; set; } = new List<PointF>();

        // [2025-9-29] Dispense Polyline 속성
        public double OpenTimeMs { get; set; }   // ms
        public double CloseTimeMs { get; set; }  // ms
        public int NumOfPulse { get; set; }   // 개수

        public bool Use { get; set; } = true;
        public int Order { get; set; }
        public float StrokeWidth { get; set; } = 2f;
        public Color Stroke { get; set; } = Color.DeepSkyBlue;
        public IEnumerable<(PointF, PointF)> Segments()
        {
            for (int i = 0; i < Points.Count - 1; i++)
                yield return (Points[i], Points[i + 1]);
        }
    }

    // [2025-8-26] PointAdded
    public class PointAddedEventArgs : EventArgs
    {
        public int Index { get; set; }
        public PointF Position { get; set; }
        public EditDispensePath.DrawToolType Tool { get; set; }
    }

    public partial class EditDispensePath : UserControl
    {
        private Image _imgBackground = null;

        public EventHandler<GraphicEventArgs> EventGraphicClicked;
        public EventHandler<EvenMousePosArgs> EventMouseClicked;
        public EventHandler<PositionEventArgs> EventPositionClicked;
        public event EventHandler<PointAddedEventArgs> PointAdded; // [2025-8-26] PointAdded
        public event EventHandler<EventArgs> RecipeChanged;

        [XmlIgnore, Browsable(false)]
        public string Position { get; set; } = "";

        public CParameter_DefectsMap Param = new CParameter_DefectsMap();

        [XmlIgnore, Browsable(false)]
        public EventHandler<EventArgs> EventClickedGrid;

        public Recipe_GraphicObject _recipe = null;
        // 도형 정보 저장용
        public GraphicPolyLine[] figuerList = new GraphicPolyLine[4];

        private int _fieldWidth = 0;
        private int _fieldHeight = 0;

        public Point MousePos = new System.Drawing.Point();
        public bool ViewOrder = true;

        public System.Drawing.Point SelectedPos = new System.Drawing.Point(-1, -1);
        public System.Drawing.Point DragStartPos = new System.Drawing.Point(-1, -1);
        public System.Drawing.Point MoveStartPos = new System.Drawing.Point(-1, -1);
        public System.Drawing.Point MoveContinuePos = new System.Drawing.Point(-1, -1);

        public bool _isSelectDrag = false;
        public bool _isMoveObjectDrag = false;

        public List<Recipe_GraphicObject> CommandList { get; set; } = new List<Recipe_GraphicObject>();
        public int CtrlZ_Index = 0;
        public int Current_Index = 0;

        // [2025-8-21]
        public Recipe_DispensingPoints Recipe_DispPoints { get; set; } = new Recipe_DispensingPoints();


        // [2025-8-22]
        private bool _isDrawingPolyline = false;
        private PointF _lineStart;
        private DispensePolyline _polyTemp = null;
        private PointF _mouse;
        //private System.Drawing.Bitmap _bg;
        private const float HIT_DIST = 8f;

        // [2025-8-26]
        private DispensePointNode _nodeTemp = null;

        // [2025-8-27]
        // === Panning state ===
        private bool _isPanning = false;
        private Point _panStartScreen;          // 컨트롤 좌표계 기준 시작점(e.Location)
        private PointF _panStartOffset = PointF.Empty; // 팬 시작 시점의 오프셋(이미지 px)
        private PointF _viewOffset = PointF.Empty;     // 현재 뷰 오프셋(이미지 px) - 모든 그리기에 적용

        // [2025-8-22]
        public void UpdateBackgroundImage()
        {
            if (File.Exists(Recipe_DispPoints.BackgroundImagePath))
            {
                _imgBackground?.Dispose();
                _imgBackground = new Bitmap(Recipe_DispPoints.BackgroundImagePath);
                RedrawComposite(); // 배경 바뀌면 1번만 합성
            }
        }

        // [2025-8-21]
        //public void UpdateBackgroundImage()
        //{
        //    if (File.Exists(Recipe_DispPoints.BackgroundImagePath))
        //    {
        //        _imgBackground = new Bitmap(Recipe_DispPoints.BackgroundImagePath);
        //    }
        //}

        public void Init(Recipe_GraphicObject rcp, string modelName, string fileName)
        {
            // 도형 정보 읽기
            figuerList = LoadFiguerData(modelName);

            _recipe = rcp;
            string currentDirectory = Environment.CurrentDirectory;
            if (!_recipe.bUseExBackground)
            {
                //_fieldWidth = 3778;
                //_fieldHeight = 1108;
                _fieldWidth = rcp.nBackGroundWidth * 10;
                _fieldHeight = rcp.nBackGroundHeight * 10;
            }
            else
            {
                // 시표 실제 크키를 0.1mm 단위까지 입력
                _fieldWidth = 2432;
                _fieldHeight = 2040;
            }

            string path = $@"D:\[SGMachineFDP]\Recipe\_config_recipe\2431x2040_PAMD2_Pallet_Image.bmp";
            //string path = $"{Application.StartupPath}\\RECIPE\\{modelName}\\2432x2040_gray_Dispe.png";
            _imgBackground = _imgBackground = Image.FromFile(path);
            _imgBackground = _imgBackground.GetThumbnailImage(_fieldWidth, _fieldHeight, null, IntPtr.Zero);
        }

        // 도형 정보 읽어오기
        private GraphicPolyLine[] LoadFiguerData(string model_name)
        {
            string path = $"D:\\[SGMachineFDP]\\Recipe\\_config_recipe\\Figuer.json";
            GraphicPolyLine[] loadedList = null;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // 객체로 역직렬화
                loadedList = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphicPolyLine[]>(json);
            }
            else
            {
                loadedList = new GraphicPolyLine[4];
                for (int i = 0; i < figuerList.Length; i++)
                {
                    loadedList[i] = new GraphicPolyLine();
                }
            }
            return loadedList;
        }

        public void SetList(Recipe_GraphicObject ob)
        {
            _recipe = (Recipe_GraphicObject)ob.Clone();
            this.Invalidate();
        }

        public void ZoomIn() => ibMap.ZoomIn();

        public void ZoomOut() => ibMap.ZoomOut();

        //public void ZoomFit() => ibMap.ZoomToFit();

        // [2025-8-27]
        public void ZoomFit()
        {
            // A) 팬 리셋
            _isPanning = false;
            _panStartOffset = PointF.Empty;
            _viewOffset = PointF.Empty;

            // B) 리셋된 상태로 새 이미지 생성/할당
            RedrawComposite(); // 내부에서 _viewOffset=0 기준으로 그려야 함

            // C) 컨트롤이 새 이미지/레이아웃을 반영한 다음에 Fit + 중앙정렬
            this.BeginInvoke(new Action(() =>
            {
                if (ibMap.Image == null) return;

                try
                {
                    ibMap.ZoomToFit();     // 화면맞춤 줌
                    ibMap.CenterToImage(); // ← 중요! 뷰포트를 이미지 중앙으로 강제
                    ibMap.Invalidate();
                }
                catch
                {
                    ibMap.Zoom = 100;      // 혹시 모를 방어
                }
            }));
        }


        public enum DrawToolType
        {
            Pointer,
            Rectangle,
            Ellipse,
            Line,
            PolyLine,
            Polygon,
            Text,
            Image,
            Connector,
            MousePan,
            NumberOfDrawTools,
            AddPoint
        };

        public DrawToolType ActiveTool { get; set; } = DrawToolType.Pointer;

        private void RaiseRecipeChanged()
        {
            RecipeChanged?.Invoke(this, EventArgs.Empty);
        }

        public EditDispensePath(string equipmentName)
        {
            InitializeComponent();

            // 키 이벤트 구독
            ibMap.KeyDown += ibMap_KeyDown;
            // Delete/Arrow 같은 키가 누락될 경우 대비
            ibMap.PreviewKeyDown += (s, e) => e.IsInputKey = true;

            ibMap.TabStop = true; // 포커스 받을 수 있게

            // [2025-8-22]
            ActiveTool = DrawToolType.Pointer;

            ibMap.MouseWheel += MouseWheelEvent;
            ibMap.ShowPixelGrid = false;
            ibMap.BackColor = Color.Black;
            ibMap.GridDisplayMode = ImageBoxGridDisplayMode.None;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 33;
            timer.Tick += new System.EventHandler(this.OnTimerTick);
            timer.Enabled = true;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        public void Fit()
        {
            ibMap.ZoomToFit();
        }

        public void CopyParam(EditDispensePath from)
        {
            this.Position = from.Position;

            this.Param.DrawingScale = from.Param.DrawingScale;
            this.Param.ActualWidth_mm = from.Param.ActualWidth_mm;
            this.Param.ActualHeight_mm = from.Param.ActualHeight_mm;
            this.Param.GridColumns = from.Param.GridColumns;
            this.Param.GridRows = from.Param.GridRows;
            this.Param.GridThickness = from.Param.GridThickness;
            this.Param.MarginX = from.Param.MarginX;
            this.Param.MarginY = from.Param.MarginY;
            this.Param.defects = from.Param.defects;
            this.Param.GridRegion = from.Param.GridRegion;
            this.Param.GridNo = from.Param.GridNo;
            this.Param.Origin = from.Param.Origin;
        }

        public void SetLayout(int nCols, int nRows)
        {
            this.Param.GridColumns = nCols;
            this.Param.GridRows = nRows;

            for (int i = 0; i < Param.GridColumns * Param.GridRows; i++) Param.GridNo.Add((i + 1).ToString());

            this.Invalidate();
        }

        public int GetSelectedIndex()
        {
            return SelectedPos.Y * Param.GridColumns + SelectedPos.X;
        }

        public void SetGridNo(string strNo)
        {
            try
            {
                Param.GridNo[GetSelectedIndex()] = strNo;
                this.Invalidate();
            }
            catch
            {
            }
        }

        // [2025-8-27]
        private void ibMap_KeyDown(object sender, KeyEventArgs e)
        {
            // 폴리라인 그리는 중이 아니라면 무시
            if (!_isDrawingPolyline || _polyTemp == null) return;

            // ① Delete 또는 Backspace로 "마지막 점 제거"
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                if (_polyTemp.Points.Count > 0)
                {
                    _polyTemp.Points.RemoveAt(_polyTemp.Points.Count - 1);

                    // 점이 하나도 안 남으면 그리기 취소 상태로
                    if (_polyTemp.Points.Count == 0)
                    {
                        _polyTemp = null;
                        _isDrawingPolyline = false;
                    }

                    RedrawComposite();
                    e.Handled = true;
                    return;
                }
            }

            // ② Esc로 전체 취소 (선택사항)
            if (e.KeyCode == Keys.Escape)
            {
                _polyTemp = null;
                _isDrawingPolyline = false;
                RedrawComposite();
                e.Handled = true;
                return;
            }

            // ③ Enter로 확정(선택사항, 더블클릭과 동일)
            if (e.KeyCode == Keys.Enter)
            {
                if (_polyTemp != null && _polyTemp.Points.Count >= 2)
                {
                    Recipe_DispPoints.Polylines.Add(_polyTemp);
                    _polyTemp = null;
                    _isDrawingPolyline = false;
                    RedrawComposite();
                    RaiseRecipeChanged();
                    e.Handled = true;
                }
            }
        }

        private void MouseWheelEvent(object sender, MouseEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // 줌 전 마우스의 이미지 좌표
                var preImg = ibMap.PointToImage(e.Location);

                if (e.Delta > 0) ibMap.ZoomIn(); else ibMap.ZoomOut();

                // 줌 후 마우스의 이미지 좌표
                var postImg = ibMap.PointToImage(e.Location);

                // 줌 때문에 마우스 기준 점이 움직이지 않도록 오프셋 보정
                _viewOffset = new PointF(
                    _viewOffset.X + (postImg.X - preImg.X),
                    _viewOffset.Y + (postImg.Y - preImg.Y)
                );

                int canvasW = _imgBackground?.Width ?? (ibMap.Image?.Width ?? ibMap.Width);
                int canvasH = _imgBackground?.Height ?? (ibMap.Image?.Height ?? ibMap.Height);
                ClampViewOffset(canvasW, canvasH);

                RedrawComposite();
            }
        }


        private void MouseWheelEvent_old(object sender, MouseEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.Delta > 0) ibMap.ZoomIn(); else ibMap.ZoomOut();
                // RedrawComposite() 호출 불필요
            }
        }

        public void AddPoint(PointF pos)
        {
            if (_recipe != null)
            {
                _recipe.actionInfos.Add(new ActionInfo(pos));
                //if (_recipe.GraphicList.Count > 0)
                //{
                //    if (_recipe.GraphicList.Last() is GraphicPolyLine)
                //    {
                //        (_recipe.GraphicList.Last() as GraphicPolyLine).Nodes.Add(new DispensePointNode(pos));
                //    }
                //}
            }

            this.Invalidate();
        }

        public void AddDefects(List<PointF> dfs = null, bool bClear = false)
        {
            if (dfs != null)
            {
                if (bClear == true) Param.defects.Clear();
                for (int i = 0; i < dfs.Count; i++) Param.defects.Add(dfs[i]);
            }

            for (int idx_df = 0; idx_df < Param.defects.Count; idx_df++)
            {
            }

            this.Invalidate();
        }

        // 그리드 넘버가 비어 있을 때 채워주는 보조 함수(Invalidate 호출 없음)
        private void EnsureGridInitialized()
        {
            if (Param.GridNo == null) Param.GridNo = new List<string>();
            int need = Param.GridColumns * Param.GridRows;
            if (Param.GridNo.Count == 0 && need > 0)
            {
                for (int i = 0; i < need; i++) Param.GridNo.Add((i + 1).ToString());
            }
        }

        public void RedrawComposite()
        {
            // 1) 오프스크린 캔버스 준비 (배경의 원본 크기 기준)
            int canvasW = Math.Max(1, _imgBackground?.Width ?? ibMap.Image?.Width ?? ibMap.Width);
            int canvasH = Math.Max(1, _imgBackground?.Height ?? ibMap.Image?.Height ?? ibMap.Height);
            using (var back = new Bitmap(canvasW, canvasH))
            using (Graphics g = Graphics.FromImage(back))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Black); // 배경 없을 때 기본색 (원하면 다른 색)

                // 최종 안전망: 실제 캔버스 기준으로 한 번 더 클램프
                ClampViewOffset(canvasW, canvasH);

                // 2) 배경 먼저 (오프셋 적용)
                if (_imgBackground != null)
                {
                    // 배경을 오프셋 위치에 그리기
                    g.DrawImage(_imgBackground, _viewOffset.X, _viewOffset.Y,
                                _imgBackground.Width, _imgBackground.Height);
                }

                // 3) 이후 도형/노드/텍스트 등은 동일한 오프셋을 적용
                //    방법 A) 전체에 변환 적용
                g.TranslateTransform(_viewOffset.X, _viewOffset.Y);

                //    방법 B) 각각 좌표에 +_viewOffset 더해서 그리기
                //    (이미 TranslateTransform을 썼으면 A가 더 간단)


                // 2) 중앙 가이드 (선택)
                using (var p = new Pen(Color.FromArgb(50, Color.Blue), 3f))  // 3px 굵기
                {
                    g.DrawLine(p, 0, canvasH / 2, canvasW, canvasH / 2);
                    g.DrawLine(p, canvasW / 2, 0, canvasW / 2, canvasH);
                }

                // 3) Grid (Param 기반)
                //EnsureGridInitialized();
                //Param.GridRegion = new List<Rectangle>();

                //int nMarginX = (int)(Param.MarginX * Param.DrawingScale);
                //int nMarginY = (int)(Param.MarginY * Param.DrawingScale);
                //int nStartX = nMarginX;
                //int nStartY = nMarginY;

                //int cols = Math.Max(1, Param.GridColumns);
                //int rows = Math.Max(1, Param.GridRows);
                //int innerW = Math.Max(0, canvasW - 2 * nMarginX);
                //int innerH = Math.Max(0, canvasH - 2 * nMarginY);
                //int cellW = Math.Max(1, innerW / cols);
                //int cellH = Math.Max(1, innerH / rows);

                // AddPoint로 추가된 Nodes 표시 (노란색 채운 원 + 검정색 십자)
                if (Recipe_DispPoints?.Nodes != null && Recipe_DispPoints.Nodes.Count > 0)
                {
                    const float r = 14f;      // 원 반지름(px)
                    const float cross = 12f;  // 십자선 절반 길이(px)

                    foreach (var node in Recipe_DispPoints.Nodes)
                    {
                        var c = node.Position;

                        // 1) 원을 노란색으로 채움
                        using (var br = new SolidBrush(Color.Yellow))
                        {
                            g.FillEllipse(br, c.X - r, c.Y - r, r * 2f, r * 2f);
                        }

                        // 2) 십자는 검정색, 굵게
                        using (var pen = new Pen(Color.Black, 2.0f))
                        {
                            g.DrawLine(pen, c.X - cross, c.Y, c.X + cross, c.Y); // 가로
                            g.DrawLine(pen, c.X, c.Y - cross, c.X, c.Y + cross); // 세로
                        }
                    }
                }

                if (Recipe_DispPoints?.Polylines != null)
                {
                    foreach (var pl in Recipe_DispPoints.Polylines)
                    {
                        using (var pen = new Pen(pl.IsSelected ? Color.OrangeRed : pl.Stroke, pl.StrokeWidth))
                        {
                            // 세그먼트 루프: Points[i] ~ Points[i+1]
                            for (int i = 0; i < pl.Points.Count - 1; i++)
                            {
                                var a = pl.Points[i];
                                var b = pl.Points[i + 1];
                                g.DrawLine(pen, a, b);
                            }
                        }
                    }
                }

                if (_isDrawingPolyline && _polyTemp != null)
                {
                    // 확정된 임시 구간(실선)
                    if (_polyTemp.Points.Count >= 2)
                    {
                        using (var penTemp = new Pen(Color.DeepSkyBlue, 2f))
                        {
                            for (int i = 0; i < _polyTemp.Points.Count - 1; i++)
                                g.DrawLine(penTemp, _polyTemp.Points[i], _polyTemp.Points[i + 1]);
                        }
                    }
                    // 마지막 점 → 현재 마우스(점선 프리뷰)
                    if (_polyTemp.Points.Count > 0)
                    {
                        using (var penPreview = new Pen(Color.DeepSkyBlue, 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                        {
                            var last = _polyTemp.Points[_polyTemp.Points.Count - 1];
                            g.DrawLine(penPreview, last, _mouse);
                        }
                    }
                }


                // === 기존 폴리라인/노드/선택 렌더링 코드 ===
                // 예)
                // DrawPolylines(g);
                // DrawNodes(g);
                // DrawSelection(g);
                // ...
                // (내부에서 사용하는 모든 좌표는 "이미지 좌표" 그대로 두고,
                //  여기서 TranslateTransform만으로 팬을 적용하는 것이 핵심)

                g.ResetTransform(); // 필요하면 리셋
                                    // 4) 완료: 화면에 반영

                // 6) 크로스헤어 (선택)
                using (var p = new Pen(Color.FromArgb(120, Color.Yellow), 2f))
                {
                    g.DrawLine(p, 0, (int)_mouse.Y, canvasW, (int)_mouse.Y);
                    g.DrawLine(p, (int)_mouse.X, 0, (int)_mouse.X, canvasH);
                }

                var old = ibMap.Image;
                ibMap.Image = (Bitmap)back.Clone();
                old?.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); // 화면 그리기는 ImageBoxEx가 맡고, 여기서는 아무 것도 하지 않음
        }

        #region CONFIG BY XML

        public void LoadConfig(string strRecipeName)
        {
            Param = Param.LoadConfig(strRecipeName, Position);
        }

        public void SaveConfig(string strRecipeName)
        {
            Param.SaveConfig(strRecipeName, Position);
        }

        #endregion CONFIG BY XML

        // [2025-8-27]
        private void ClampViewOffset(int canvasW, int canvasH)
        {
            float minX = 0f;
            float minY = 0f;
            float maxX = 0f;
            float maxY = 0f;

            if (_imgBackground == null)
            {
                // 배경이 없으면 컨트롤/캔버스 기준으로만 제한
                minX = -canvasW; // 필요 시 완화/강화
                minY = -canvasH;
                maxX = 0f;
                maxY = 0f;

                _viewOffset = new PointF(
                    Math.Max(minX, Math.Min(maxX, _viewOffset.X)),
                    Math.Max(minY, Math.Min(maxY, _viewOffset.Y))
                );
                return;
            }

            int imgW = _imgBackground.Width;
            int imgH = _imgBackground.Height;

            // 이미지가 캔버스보다 크면 음수 방향으로 스크롤 가능, 작으면 (0,0) 고정
            minX = Math.Min(0, canvasW - imgW);
            minY = Math.Min(0, canvasH - imgH);
            maxX = 0f;
            maxY = 0f;

            _viewOffset = new PointF(
                Math.Max(minX, Math.Min(maxX, _viewOffset.X)),
                Math.Max(minY, Math.Min(maxY, _viewOffset.Y))
            );
        }

        private void ibMap_MouseDown(object sender, MouseEventArgs e)
        {
            var pt = ibMap.PointToImage(e.Location);
            _mouse = new PointF(pt.X, pt.Y);

            // [2025-8-27]
            // Ctrl + LeftDrag => Panning 시작
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _panStartScreen = e.Location;      // 컨트롤 좌표
                _panStartOffset = _viewOffset;     // 현재 오프셋 기억
                ibMap.Cursor = Cursors.Hand;
                return; // 다른 도구 로직은 무시
            }

            if (ActiveTool == DrawToolType.PolyLine)
            {
                // 포커스 확보(키 입력 받기 위함)
                if (!ibMap.Focused) ibMap.Focus();

                if (Control.ModifierKeys == Keys.None && e.Button == MouseButtons.Left) // [2025-8-25] 조건 수정
                {
                    if (!_isDrawingPolyline)
                    {
                        _isDrawingPolyline = true;
                        _polyTemp = new DispensePolyline { Order = Recipe_DispPoints.Polylines.Count };
                        _polyTemp.Points.Add(_mouse);
                    }
                    else
                    {
                        _polyTemp.Points.Add(_mouse);
                    }
                    RedrawComposite(); // ← 여기서도 즉시 합성
                    return;
                }
                else if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift && e.Button == MouseButtons.Left) // [2025-8-25] 종료 조건 추가
                {
                    if (_isDrawingPolyline && _polyTemp != null && _polyTemp.Points.Count >= 1)
                    {
                        _polyTemp.Points.Add(_mouse);

                        Recipe_DispPoints.Polylines.Add(_polyTemp);
                        _polyTemp = null;
                        _isDrawingPolyline = false;
                        RedrawComposite();
                        RaiseRecipeChanged();
                    }
                }
            }


            if (ActiveTool == DrawToolType.AddPoint)
            {
                if (Control.ModifierKeys == Keys.None && e.Button == MouseButtons.Left)
                {
                    Recipe_DispPoints.Nodes.Add(new DispensePointNode(_mouse));

                    // 이벤트 발생: No, 좌표, 도구 타입 전달
                    PointAdded?.Invoke(this, new PointAddedEventArgs
                    {
                        Index = Recipe_DispPoints.Nodes.Count,  // 1부터 번호 매김
                        Position = _mouse,
                        Tool = DrawToolType.AddPoint
                    });

                    RaiseRecipeChanged();

                    RedrawComposite(); // ← 여기서도 즉시 합성
                    return;
                }
            }

            // Pointer 모드 HitTest 등도 끝나면
            RedrawComposite();
        }

        private static bool HitSegment(PointF a, PointF b, PointF p)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len2 = dx * dx + dy * dy;
            if (len2 == 0) return false;
            float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
            t = Math.Max(0, Math.Min(1, t));
            float hx = a.X + t * dx, hy = a.Y + t * dy;
            float dist = (float)Math.Sqrt((p.X - hx) * (p.X - hx) + (p.Y - hy) * (p.Y - hy));
            return dist <= HIT_DIST;
        }

        private void ibMap_MouseUp(object sender, MouseEventArgs e)
        {
            // [2025-8-27]
            if (_isPanning)
            {
                _isPanning = false;
                ibMap.Cursor = Cursors.Default;
                return;
            }

            System.Drawing.Point ptMouse = ibMap.PointToImage(e.Location);
            if ((e.Button == MouseButtons.Right))
            {
                EventMouseClicked?.Invoke(this, new EvenMousePosArgs(ptMouse));
            }
        }

        private bool _IsPointDrag = false;
        private PointF _PointDragPrev = new PointF();

        private void ibMap_MouseClick(object sender, MouseEventArgs e)
        {
            System.Drawing.Point ptMouse = ibMap.PointToImage(e.Location);
            this.Invalidate();
        }

        public void ClearDefects()
        {
            Param.defects.Clear();
            this.Invalidate();
        }

        private int SelectedDefects = -1;


        private void ibMap_MouseMove(object sender, MouseEventArgs e)
        {
            // [2025-8-27]
            if (_isPanning)
            {
                // 컨트롤 좌표에서 움직인 거리
                int dxScreen = e.Location.X - _panStartScreen.X;
                int dyScreen = e.Location.Y - _panStartScreen.Y;

                // 현재 줌(%)를 이미지 스케일로 환산 (100% => 1.0)
                float scale = (float)(ibMap.Zoom / 100.0);
                if (scale <= 0f) scale = 1f;

                // 화면 이동량을 이미지 좌표계 이동량으로 변환
                _viewOffset = new PointF(
                    _panStartOffset.X + dxScreen / scale,
                    _panStartOffset.Y + dyScreen / scale
                );

                // 캔버스 크기 추정(팬 중에는 RedrawComposite 아직 안 들어갔으니 임시 계산)
                int canvasW = _imgBackground?.Width ?? (ibMap.Image?.Width ?? ibMap.Width);
                int canvasH = _imgBackground?.Height ?? (ibMap.Image?.Height ?? ibMap.Height);
                ClampViewOffset(canvasW, canvasH);

                RedrawComposite(); // ← 팬 반영
                return;
            }

            var pt = ibMap.PointToImage(e.Location);   // 이미지 좌표
            MousePos = pt;
            _mouse = new PointF(pt.X, pt.Y);
            RedrawComposite(); // ← 화면 갱신
        }

        private void ibMap_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_isDrawingPolyline && _polyTemp != null && _polyTemp.Points.Count >= 2)
            {
                Recipe_DispPoints.Polylines.Add(_polyTemp);
                _polyTemp = null;
                _isDrawingPolyline = false;
                RedrawComposite();
                RaiseRecipeChanged();
            }
        }
    }
}

//끝