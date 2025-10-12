using ImageGlass;
using Newtonsoft.Json;
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
using System.Windows.Forms;
using System.Xml.Serialization;
using Cursors = System.Windows.Forms.Cursors;

namespace DispensePath
{    
    public partial class EditFiguer : UserControl
    {
        private Image _imgBackground = null;

        public EventHandler<GraphicEventArgs> EventFiguerClicked;
        public EventHandler<PositionEventArgs> EventPositionClicked;
        public EventHandler<EvenMousePosArgs> EventMouseClicked;

        [XmlIgnore, Browsable(false)]
        public string Position { get; set; } = "";

        public CParameter_DefectsMap Param = new CParameter_DefectsMap();

        [XmlIgnore, Browsable(false)]
        public EventHandler<EventArgs> EventClickedGrid;

        //public Recipe_GraphicObject _recipe = null;
        //public Figure_Info _figuers = new Figure_Info("");    
        public GraphicPolyLine _figure = new GraphicPolyLine();

        private int _fieldWidth = 0;
        private int _fieldHeight = 0;

        public Point MousePos = new System.Drawing.Point();
        public bool ViewOrder = true;
        public int curFiguerNum = 0;

        public Setting_Recipe Recipe = null;

        public System.Drawing.Point SelectedPos = new System.Drawing.Point(-1, -1);
        public System.Drawing.Point DragStartPos = new System.Drawing.Point(-1, -1);
        public System.Drawing.Point MoveStartPos = new System.Drawing.Point(-1, -1);
        public System.Drawing.Point MoveContinuePos = new System.Drawing.Point(-1, -1);

        public bool _isSelectDrag = false;
        public bool _isMoveObjectDrag = false;

        public List<Recipe_GraphicObject> CommandList { get; set; } = new List<Recipe_GraphicObject>();
        public int CtrlZ_Index = 0;
        public int Current_Index = 0;

        public void Init(GraphicPolyLine figuer, string back_path = "", int width = 6520, int height = 2080)
        {
            _figure = figuer;
            string currentDirectory = Environment.CurrentDirectory;

            // 시표 실제 크키를 0.1mm 단위까지 입력
            _fieldWidth = 2432;
            _fieldHeight = 2040;
            string path = $"D:\\[SGMachineFDP]\\Recipe\\_config_recipe\\2432x2040_gray_Dispe.png";
            _imgBackground = _imgBackground = Image.FromFile(path);
            _imgBackground = _imgBackground.GetThumbnailImage(_fieldWidth, _fieldHeight, null, IntPtr.Zero);
        }

        public void SetList(Recipe_GraphicObject ob)
        {
            //_recipe = (Recipe_GraphicObject)ob.Clone();
            //this.Invalidate();
        }

        public void ZoomIn() => ibMap.ZoomIn();

        public void ZoomOut() => ibMap.ZoomOut();

        public void ZoomFit() => ibMap.ZoomToFit();

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
            NumberOfDrawTools
        };

        public DrawToolType ActiveTool { get; set; } = DrawToolType.Pointer;

        public EditFiguer(string recipe_name, string file_name)
        {
            InitializeComponent();

            Recipe = new Setting_Recipe(recipe_name, file_name);
            Recipe = Recipe.Load(recipe_name, file_name);

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

        private void MouseWheelEvent(object sender, MouseEventArgs e)
        {
            if ((e.Delta / 120) > 0) ibMap.ZoomIn();
            else ibMap.ZoomOut();
        }

        public void AddPoint(PointF pos)
        {
            if (_figure != null)
            {
                _figure.Nodes.Add(new DispensePointNode(pos));
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

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                if (_imgBackground == null) return;

                double zoom = 200 / ibMap.Zoom;

                using (Bitmap imgBackground = new Bitmap(_imgBackground))
                using (Graphics g = Graphics.FromImage(imgBackground))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    using (Font font_9 = new Font("Arial", (int)(9 * zoom), FontStyle.Bold))
                    using (Font font_25 = new Font("Arial", (int)(25 * zoom), FontStyle.Bold))
                    using (Font font_100 = new Font("Arial", 100))
                    using (SolidBrush brushWhite = new SolidBrush(Color.White))
                    using (Pen penDashLine_Yellow = new Pen(Color.Yellow, (int)(1 * zoom)))
                    using (Pen penDashLine_NoUse = new Pen(Color.Silver, (int)(1 * zoom)))
                    using (Pen penDashLine_Silver = new Pen(Color.Silver, (int)(1 * zoom)))
                    using (Pen penDashLine_Red = new Pen(Color.Red, (int)(2 * zoom)))
                    using (Pen penDashLine_Green = new Pen(Color.Green, (int)(1 * zoom)))
                    using (Pen penDashLine_White = new Pen(Color.White, (int)(2 * zoom)))
                    {
                        //g.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), new Rectangle(0, 0, nDrawingWidth, nDrawingHeight));
                        //g.DrawImage(_imgBackground, 0, 0);
                        Param.GridRegion = new List<Rectangle>();

                        if (Param.GridNo.Count == 0) SetLayout(Param.GridColumns, Param.GridRows);

                        penDashLine_Red.DashStyle = DashStyle.Dash;
                        penDashLine_Green.DashStyle = DashStyle.Dash;
                        penDashLine_White.DashStyle = DashStyle.Dash;

                        float fX_Center = _fieldWidth / 2;
                        float fY_Center = _fieldHeight / 2;

                        g.DrawLine(penDashLine_White, 0, fY_Center, _fieldWidth, fY_Center);
                        g.DrawLine(penDashLine_White, fX_Center, 0, fX_Center, _fieldHeight);

                        if (_IsPointDrag)
                        {
                            g.FillEllipse(new SolidBrush(Color.Silver), _PointDragPrev.X - (int)(1 * zoom), _PointDragPrev.Y - (int)(1 * zoom), (int)(4 * zoom), (int)(4 * zoom));
                        }

                        if (_figure != null)
                        {
                            //foreach (var ob in _recipe.GraphicList)
                            //{
                            //    if (ob is GraphicPolyLine)
                            //    {
                                    GraphicPolyLine graphic = _figure;

                                    if (graphic.Selected && _isMoveObjectDrag)
                                    {
                                        graphic.Nodes.ForEach(pt => pt.Position = new PointF(pt.Position.X + (MousePos.X - MoveContinuePos.X), pt.Position.Y + (MousePos.Y - MoveContinuePos.Y)));

                                        MoveContinuePos = MousePos;
                                    }

                                    if (graphic.Nodes != null && graphic.Nodes.Count > 0)
                                    {
                                        for (int i = 0; i < graphic.Nodes.Count; i++)
                                        {
                                            int x = (int)graphic.Nodes[i].Position.X - 10;
                                            int y = (int)graphic.Nodes[i].Position.Y - 10;

                                            Rectangle hitTest = new Rectangle(x, y, 20, 20);
                                            if (hitTest.Contains(MousePos))
                                            {
                                                Cursor = Cursors.Hand;
                                            }

                                            float fX = graphic.Nodes[i].Position.X;
                                            float fY = graphic.Nodes[i].Position.Y;

                                            if (i > 0)
                                            {
                                                float fX_Next = graphic.Nodes[i - 1].Position.X;
                                                float fY_Next = graphic.Nodes[i - 1].Position.Y;

                                                if (i == graphic.SelectedPoint && graphic.Selected && _IsPointDrag)
                                                {
                                                    g.DrawLine(penDashLine_Silver, fX_Next, fY_Next, _PointDragPrev.X, _PointDragPrev.Y);
                                                }

                                                if (graphic.Selected && _isMoveObjectDrag)
                                                {
                                                    g.DrawLine(penDashLine_Silver, fX_Next, fY_Next, fX, fY);
                                                }
                                                else if (graphic.Selected)
                                                {
                                                    if (graphic.Nodes[i].Use == true)
                                                    {
                                                        g.DrawLine(penDashLine_Red, fX_Next, fY_Next, fX, fY);
                                                    }
                                                    else
                                                    {
                                                        g.DrawLine(penDashLine_NoUse, fX_Next, fY_Next, fX, fY);
                                                    }
                                                }
                                                else
                                                {
                                                    if (graphic.Nodes[i].Use == true)
                                                    {
                                                        g.DrawLine(penDashLine_Yellow, fX_Next, fY_Next, fX, fY);
                                                    }
                                                    else
                                                    {
                                                        g.DrawLine(penDashLine_NoUse, fX_Next, fY_Next, fX, fY);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                //}

                                //if (ob is GraphicPolyLine)
                                //{
                                    //GraphicPolyLine graphic = _figure;

                                    if (graphic.Nodes != null && graphic.Nodes.Count > 0)
                                    {
                                        for (int i = 0; i < graphic.Nodes.Count; i++)
                                        {
                                            if (i == graphic.SelectedPoint && graphic.Selected && _IsPointDrag)
                                            {
                                                graphic.Nodes[i].Position = MousePos;
                                            }

                                            float fX = graphic.Nodes[i].Position.X;
                                            float fY = graphic.Nodes[i].Position.Y;

                                            if (ViewOrder)
                                            {
                                                g.DrawString((i + 1).ToString(), new Font("Arial", 16, FontStyle.Bold), new SolidBrush(Color.Orange), new PointF(fX - 5, fY - 10));
                                            }

                                            if (graphic.Selected && _isMoveObjectDrag)
                                            {
                                                g.FillEllipse(new SolidBrush(Color.Silver), fX - (int)(1 * zoom), fY - (int)(1 * zoom), (int)(4 * zoom), (int)(4 * zoom));
                                            }
                                            else if (i == graphic.SelectedPoint)
                                            {
                                                g.FillEllipse(new SolidBrush(Color.Orange), fX - (int)(1 * zoom), fY - (int)(1 * zoom), (int)(4 * zoom), (int)(4 * zoom));
                                            }
                                            else
                                            {
                                                g.FillEllipse(new SolidBrush(Color.Yellow), fX - (int)(1 * zoom), fY - (int)(1 * zoom), (int)(2 * zoom), (int)(2 * zoom));
                                            }
                                        }
                                    }
                            //    }
                            //}
                        }

                        if (_isSelectDrag)
                        {
                            int startX = Math.Min(MousePos.X, DragStartPos.X);
                            int startY = Math.Min(MousePos.Y, DragStartPos.Y);

                            int W = Math.Abs(MousePos.X - DragStartPos.X);
                            int H = Math.Abs(MousePos.Y - DragStartPos.Y);

                            g.DrawRectangle(penDashLine_Green, new Rectangle(startX, startY, W, H));
                        }

                        g.DrawLine(penDashLine_Green, new Point(0, MousePos.Y), new Point(_fieldWidth, MousePos.Y));
                        g.DrawLine(penDashLine_Green, new Point(MousePos.X, 0), new Point(MousePos.X, _fieldHeight));

                        if (ActiveTool == DrawToolType.PolyLine)
                        {
                            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                            {
                                g.FillEllipse(new SolidBrush(Color.Red), MousePos.X - (int)(2.5 * zoom), MousePos.Y - (int)(2.5 * zoom), (int)(5 * zoom), (int)(5 * zoom));
                            }
                        }

                        string actualPos = $"{(MousePos.X * Recipe.Align.PixelSize_X)}mm, {(MousePos.Y * Recipe.Align.PixelSize_Y)}mm";
                        //temp

                        SizeF size = g.MeasureString(actualPos, font_9);
                        if (MousePos.X + size.Width + 5 > _fieldWidth && MousePos.Y + size.Height + 5 > _fieldHeight)
                            g.DrawString(actualPos, font_9, Brushes.Yellow, MousePos.X - size.Width - 5, MousePos.Y - size.Height - 5);
                        else if (MousePos.X + size.Width + 5 > _fieldWidth)
                            g.DrawString(actualPos, font_9, Brushes.Yellow, MousePos.X - size.Width - 5, MousePos.Y + 5);
                        else if (MousePos.Y + size.Height + 5 > _fieldHeight)
                            g.DrawString(actualPos, font_9, Brushes.Yellow, MousePos.X + 5, MousePos.Y - size.Height - 5);
                        else
                            g.DrawString(actualPos, font_9, Brushes.Yellow, MousePos.X + 5, MousePos.Y + 5);

                        penDashLine_Red.Dispose();
                        penDashLine_Green.Dispose();
                    }

                    ibMap.Image = (Bitmap)imgBackground.Clone();
                }
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
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

        private void ibMap_MouseDown(object sender, MouseEventArgs e)
        {
            //System.Drawing.Point ptMouse = ibMap.PointToImage(e.Location);

            //if (ActiveTool == DrawToolType.Pointer)
            //{
            //    if ((e.Button == MouseButtons.Left))
            //    {
            //        ibMap.AutoPan = true;
            //        Cursor = Cursors.Cross;
            //    }
            //    else if ((e.Button == MouseButtons.Right))
            //    {
            //        CtrlZ_Index = 0;

            //        //if (e.Button == MouseButtons.Right) CommandList.Add((Recipe_GraphicObject)_recipe.Clone());

            //        ibMap.AutoPan = false;

            //        if (_figure != null)
            //        {
            //            //_recipe.GraphicList.ForEach(ob => ob.Selected = false);
            //            //foreach (var ob in _recipe.GraphicList)
            //            //{
            //            //    if (ob is GraphicPolyLine)
            //            //    {
            //                    GraphicPolyLine graphic = _figure;

            //                    if (graphic.Nodes != null && graphic.Nodes.Count > 0)
            //                    {
            //                        for (int i = 0; i < graphic.Nodes.Count; i++)
            //                        {
            //                            int x = (int)graphic.Nodes[i].Position.X - 10;
            //                            int y = (int)graphic.Nodes[i].Position.Y - 10;

            //                            Rectangle hitTest = new Rectangle(x, y, 20, 20);
            //                            if (hitTest.Contains(MousePos))
            //                            {
            //                                graphic.SelectedPoint = i;
            //                                graphic.Selected = true;

            //                                if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
            //                                {
            //                                    _IsPointDrag = true;
            //                                    _PointDragPrev = graphic.Nodes[i].Position;
            //                                }

            //                                EventPositionClicked?.Invoke(this, new PositionEventArgs(graphic.Nodes[i]));

            //                                return;
            //                            }
            //                        }
            //                    }

            //                    if (graphic.HitTest(ptMouse))
            //                    {
            //                        _isMoveObjectDrag = true;
            //                        graphic.Selected = true;

            //                        MoveStartPos = ptMouse;
            //                        MoveContinuePos = ptMouse;

            //                        EventFiguerClicked?.Invoke(this, new GraphicEventArgs(graphic));
            //                        return;
            //                    }
            //            //    }
            //            //}
            //        }

            //        _isSelectDrag = true;
            //        DragStartPos = ptMouse;
            //        Cursor = Cursors.Default;
            //    }
            //}
            //else if (ActiveTool == DrawToolType.PolyLine)
            //{
            //    if (e.Button == MouseButtons.Right)
            //    {
            //        ActiveTool = DrawToolType.Pointer;
            //    }
            //    else
            //    {
            //        if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            //        {
            //            AddPoint(ptMouse);
            //        }
            //    }
            //}
            //else if (ActiveTool == DrawToolType.Rectangle)
            //{
            //    if (e.Button == MouseButtons.Right)
            //    {
            //        ActiveTool = DrawToolType.Pointer;
            //    }
            //    else
            //    {
            //        if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            //        {
            //            AddPoint(ptMouse);
            //        }
            //    }
            //}
        }

        private void ibMap_MouseUp(object sender, MouseEventArgs e)
        {
            System.Drawing.Point ptMouse = ibMap.PointToImage(e.Location);
            if ((e.Button == MouseButtons.Right))
            {
                EventMouseClicked?.Invoke(this, new EvenMousePosArgs(ptMouse));
            }
            //if (ActiveTool == DrawToolType.Pointer)
            //{
            //    _isSelectDrag = false;
            //    _IsPointDrag = false;
            //    _isMoveObjectDrag = false;

            //    ibMap.AutoPan = false;
            //    Cursor = Cursors.Default;

            //    int startX = Math.Min(MousePos.X, DragStartPos.X);
            //    int startY = Math.Min(MousePos.Y, DragStartPos.Y);

            //    int W = Math.Abs(MousePos.X - DragStartPos.X);
            //    int H = Math.Abs(MousePos.Y - DragStartPos.Y);

            //    RectangleF DragRect = new RectangleF(startX, startY, W, H);

            //    if (_figure != null)
            //    {
            //        //if (e.Button == MouseButtons.Right) CommandList.Add((Recipe_GraphicObject)_recipe.Clone());
            //        //else return;
            //        //_recipe.GraphicList.ForEach(ob => ob.Selected = false);

            //        //foreach (var ob in _recipe.GraphicList)
            //        //{
            //        //    if (ob is GraphicPolyLine)
            //        //    {
            //                GraphicPolyLine graphic = _figure;

            //                if (DragRect.IntersectsWith(graphic.GetBoundary()))
            //                {
            //                    graphic.Selected = true;
            //                    EventFiguerClicked?.Invoke(this, new GraphicEventArgs(graphic));
            //                }
            //        //    }
            //        //}
            //    }

            //    DragStartPos = new Point();
            //}
        }

        private bool _IsPointDrag = false;
        private PointF _PointDragPrev = new PointF();

        private void ibMap_MouseClick(object sender, MouseEventArgs e)
        {
            System.Drawing.Point ptMouse = ibMap.PointToImage(e.Location);

            //for (int i = 0; i < Param.GridRegion.Count; i++)
            //{
            //    if (Param.GridRegion[i].Contains(ptMouse))
            //    {
            //        SelectedPos = new System.Drawing.Point(i % Param.GridColumns, i / Param.GridColumns);
            //        EventClickedGrid?.Invoke(this, new EventArgs());
            //        break;
            //    }
            //}

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
            System.Drawing.Point ptMouse = MousePos = ibMap.PointToImage(e.Location);
        }

        private void ibMap_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //System.Drawing.Point ptMouse = ibMap.PointToImage(e.Location);

            //bool bContain = false;

            //int nMarginX = (int)(Param.MarginX * Param.DrawingScale);
            //int nMarginY = (int)(Param.MarginY * Param.DrawingScale);

            //int nStartX = nMarginX;
            //int nStartY = nMarginY;

            //for (int i = 0; i < Param.defects.Count; i++)
            //{
            //    //float fX = nStartX + Param.defects[i].GetRealPos(Param.DrawingScale).X / 1000;
            //    //float fY = nStartY + Param.defects[i].GetRealPos(Param.DrawingScale).Y / 1000;

            //    //RectangleF rtBoudary = new RectangleF(fX - 5, fY - 5, 10, 10);
            //    //if (rtBoudary.Contains(ptMouse))
            //    //{
            //    //    FormSettings_PropertyGrid form = new FormSettings_PropertyGrid(Param.defects[i]);
            //    //    form.Show();
            //    //    return;
            //    //}
            //}
        }
    }
}

//끝