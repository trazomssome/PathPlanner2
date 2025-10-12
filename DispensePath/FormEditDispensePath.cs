using Newtonsoft.Json;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;
using Point = System.Drawing.Point;

namespace DispensePath
{
    public partial class FormEditDispensePath : UserControl
    {
        // [2025-9-29]
        // gridGraphicNodes의 각 행이 무엇을 가리키는지 메타 저장
        private sealed class GridPointMeta
        {
            public bool IsPolylineStart;
            public EditDispensePath.DrawToolType Tool;
            public int PolylineOrder; // pl.Order
            public int PointIndex;    // pl.Points 내 인덱스
        }

        // 현재 “시작점 편집 중”인 대상
        private GridPointMeta _editingStartMeta;

        #region Members

        private bool _controlKey = false;
        private bool _panMode = false;
        private GraphicPolyLine temp = new GraphicPolyLine();

        private Recipe_GraphicObject _original = null;

        //public LVDTOffSet From_LVDTOffSet = new LVDTOffSet();

        #endregion Members

        public EditDispensePath _graphicEditor;
        private bool _isSave = false;
        private ActionInfo _selectedAction = null;
        private int SelectedPoint = -1;

        private void SetStartParamEditorsEnabled(bool enabled)
        {
            tbOpenTime.Enabled = enabled;
            tbCloseTime.Enabled = enabled;
            tbNumOfPulse.Enabled = enabled;
            btnPositionApply.Enabled = enabled;
        }

        public FormEditDispensePath(string recipe_name, string file_name)
        {
            InitializeComponent();

            // [2025-9-29]
            // 숫자 입력 제한(선택) — Sunny.UI 지원
            tbOpenTime.Type = Sunny.UI.UITextBox.UIEditType.Double;
            tbCloseTime.Type = Sunny.UI.UITextBox.UIEditType.Double;
            tbNumOfPulse.Type = Sunny.UI.UITextBox.UIEditType.Integer;

            // 초기엔 비활성화
            SetStartParamEditorsEnabled(false);

            _graphicEditor = new EditDispensePath(recipe_name); 
            _graphicEditor.Dock = DockStyle.Fill;
            _graphicEditor.Visible = true;

            plPathDraw.Controls.Add(_graphicEditor);

            // 이벤트 구독: Grid에 행 추가
            _graphicEditor.PointAdded += (s, e) =>
            {
                // 컬럼 순서: No / X / Y / DrawToolType (디자이너의 컬럼 순서 기준)
                // 표시 포맷은 필요에 맞게 조정
                gridGraphicNodes.Rows.Add(
                    e.Index,
                    e.Position.X.ToString("0.###"),
                    e.Position.Y.ToString("0.###"),
                    e.Tool.ToString() // "AddPoint"
                );
            };

            gridGraphicNodes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            gridGraphicNodes.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 15F, FontStyle.Bold);

            // [2025-9-29]
            gridGraphicNodes.CellClick += gridGraphicNodes_CellClick;

        }

        // gridGraphicNodes DataGridView의 폰트 크기 조정
        private void InitializeGridStyle()
        {
            // 헤더 폰트 사이즈 변경
            //gridGraphicNodes.ColumnHeadersDefaultCellStyle.Font =
            //    new Font("맑은 고딕", 10F, FontStyle.Bold);  // 기존보다 작게 (9pt)
        }

        private void FormSetting_GraphicEdit_Load(object sender, EventArgs e)
        {
            try
            {
                //MainRecipe가 이터널이라 보호수준으로 FileName을 가져올수 없음

                //IAuroraEnvironment Aurora = AuroraApp.Current.AuroraEnv();
                //var recipe = Aurora.RecipeManager["MainRecipe"] as MainRecipe;

                //_graphicEditor.EventGraphicClicked += OnGraphicClicked;
                //_graphicEditor.EventMouseClicked += OnMouseClicked;
                //KeyPreview = true;
                this.Focus();

                tbPixelSizeX.Text = _graphicEditor.Recipe_DispPoints.PixelSizeX.ToString("F3");
                tbPixelSizeY.Text = _graphicEditor.Recipe_DispPoints.PixelSizeY.ToString("F3");

                InitUI();

                InitializeGridStyle();

                this.AutoScaleMode = AutoScaleMode.None;
            }
            catch (Exception ex)
            {

            }
            
        }


        private void InitUI()
        {
            try
            {
                // Grid에 현재 값 표시
                _selectedAction = new ActionInfo();
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }

        private void RefreshGraphicGrid()
        {
            gridGraphicNodes.SuspendLayout();
            try
            {
                gridGraphicNodes.Rows.Clear();

                int no = 1;

                // 1) AddPoint로 추가된 노드들
                var nodes = _graphicEditor?.Recipe_DispPoints?.Nodes;
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        gridGraphicNodes.Rows.Add(
                            no++,
                            n.Position.X.ToString("0.###"),
                            n.Position.Y.ToString("0.###"),
                            EditDispensePath.DrawToolType.AddPoint.ToString()  // "AddPoint"
                        );
                    }
                }

                // 2) PolyLine의 각 점들
                var polylines = _graphicEditor?.Recipe_DispPoints?.Polylines;
                if (polylines != null)
                {
                    foreach (var pl in polylines)
                    {
                        for (int j = 0; j < pl.Points.Count; j++)
                        {
                            var p = pl.Points[j];
                            int rowIndex = gridGraphicNodes.Rows.Add(
                                no++,
                                p.X.ToString("0.###"),
                                p.Y.ToString("0.###"),
                                EditDispensePath.DrawToolType.PolyLine.ToString()
                            );

                            var row = gridGraphicNodes.Rows[rowIndex];
                            // 행 메타정보 태깅
                            row.Tag = new GridPointMeta
                            {
                                IsPolylineStart = (j == 0),
                                Tool = EditDispensePath.DrawToolType.PolyLine,
                                PolylineOrder = pl.Order,
                                PointIndex = j
                            };

                            // 시작점이면 연한 푸른색으로 강조
                            if (j == 0)
                            {
                                row.DefaultCellStyle.BackColor = Color.FromArgb(232, 244, 255); // 아주 연한 푸른색
                                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(204, 228, 247); // 선택 시도 연한 푸른색
                            }
                        }
                    }
                }
                //    gridGraphicNodes.ClearSelection(); // 보기 깔끔하게(?)
            }
            finally
            {
                gridGraphicNodes.ResumeLayout();
            }
        }

        private void gridGraphicNodes_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //if (e.RowIndex < 0) return; // 헤더 제외

            //var row = gridGraphicNodes.Rows[e.RowIndex];
            //if (row?.Tag is GridPointMeta meta
            //    && meta.Tool == EditDispensePath.DrawToolType.PolyLine
            //    && meta.IsPolylineStart)
            //{
            //    // 👉 여기서부터 실제 동작을 수행 (내용은 당신이 알려줄 예정)
            //    OnPolylineStartRowDoubleClick(meta);
            //}
            //// 그 외는 무시
            ///

            if (e.RowIndex < 0) return;

            SetStartParamEditorsEnabled(false);     // 입력 비활성화

            var row = gridGraphicNodes.Rows[e.RowIndex];
            if (!(row?.Tag is GridPointMeta meta)) return;

            // 오직 시작점 + PolyLine 만
            if (meta.Tool != EditDispensePath.DrawToolType.PolyLine || !meta.IsPolylineStart)
                return;

            // 대상 Polyline 찾기
            var pl = _graphicEditor?.Recipe_DispPoints?.Polylines?
                .FirstOrDefault(x => x.Order == meta.PolylineOrder);
            if (pl == null) return;

            _editingStartMeta = meta;              // 편집 대상 기억
            SetStartParamEditorsEnabled(true);     // 입력 활성화

            // 기존 값 로드 (없으면 0/빈값)
            tbOpenTime.Text = pl.OpenTimeMs.ToString("0.###");
            tbCloseTime.Text = pl.CloseTimeMs.ToString("0.###");
            tbNumOfPulse.Text = pl.NumOfPulse.ToString();
        }

        // TODO: 더블클릭 시 실제 동작은 여기에서 구현(당신이 내용 제공)
        private void OnPolylineStartRowDoubleClick(GridPointMeta meta)
        {
            // 예시(임시): MessageBox로 확인
            MessageBox.Show($"Polyline #{meta.PolylineOrder} start point double-clicked (Index {meta.PointIndex})");
        }


        public void LoadParam()
        {
            _graphicEditor.Recipe_DispPoints = _graphicEditor.Recipe_DispPoints.Load(txtRecipeName.Text);
            //txtBackgroudImgPath.Text = _graphicEditor.Recipe_DispPoints.BackgroundImagePath;
            tbPixelSizeX.DoubleValue = _graphicEditor.Recipe_DispPoints.PixelSizeX;
            tbPixelSizeY.DoubleValue = _graphicEditor.Recipe_DispPoints.PixelSizeY;

            _graphicEditor.UpdateBackgroundImage();
            _graphicEditor.Invalidate();

            RefreshGraphicGrid();
        }
        private void MouseWheelEventSource(object sender, MouseEventArgs e)
        {
        }        

        //private void OnGraphicClicked(object sender, GraphicEventArgs e)
        //{
        //    //if (this.InvokeRequired)
        //    //{
        //    //    try
        //    //    {
        //    //        this.Invoke(new MethodInvoker(() =>
        //    //        {
        //    //            OnGraphicClicked(sender, e);
        //    //        }));
        //    //    }
        //    //    catch (Exception ex)
        //    //    {
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    try
        //    //    {
        //    //        if (e.Graphic != null)
        //    //        {
        //    //            _selectedAction = e.Graphic;
        //    //            tbOrder.Text = _selectedAction.Order.ToString();
        //    //            UpdateMotorAction();
        //    //        }
        //    //    }
        //    //    catch (Exception ex)
        //    //    {
        //    //    }
        //    //}
        //}

        //private void OnMouseClicked(object sender, EvenMousePosArgs e)
        //{
        //    if (this.InvokeRequired)
        //    {
        //        try
        //        {
        //            this.Invoke(new MethodInvoker(() =>
        //            {
        //                OnMouseClicked(sender, e);
        //            }));
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //    }
        //    else
        //    {
        //        try
        //        {                 
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //    }
        //}

        private void OnPositionClicked(object sender, PositionEventArgs e)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        OnPositionClicked(sender, e);
                    }));
                }
                catch (Exception ex)
                {
                    //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
                }
            }
            else
            {
                try
                {
                    if (e.Node != null)
                    {
                    }
                }
                catch (Exception ex)
                {
                    //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
                }
            }
        }

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            if (CUtil.ShowMessageBox("Load New Image", "All data will be cleared. Do you really want to load a new image?"))
            {
                OpenFileDialog loadFileDialog = new OpenFileDialog();
                loadFileDialog.Filter = "Image File (*.bmp)|*.bmp|All File (*.*)|*.*";
                loadFileDialog.Title = "Select Path to load a new image";

                if (loadFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClearAll();
                    _graphicEditor.Recipe_DispPoints.BackgroundImagePath = loadFileDialog.FileName;
                    _graphicEditor.UpdateBackgroundImage();
                    _graphicEditor.Invalidate();
                }
            }

        }

        //송현수
        private void RotateAndTranslatePoints(Point[] points, float rotationAngle, int deltaX, int deltaY)
        {
            // 회전 변환 행렬 계산
            float angleRad = rotationAngle * (float)Math.PI / 180.0f;
            float cosTheta = (float)Math.Cos(angleRad);
            float sinTheta = (float)Math.Sin(angleRad);

            // 각 점을 회전하고 이동시킴
            for (int i = 0; i < points.Length; i++)
            {
                int rotatedX = (int)(points[i].X * cosTheta - points[i].Y * sinTheta);
                int rotatedY = (int)(points[i].X * sinTheta + points[i].Y * cosTheta);

                points[i] = new Point(rotatedX + deltaX, rotatedY + deltaY);
            }
        }

        private List<GraphicPolyLine> _clipboard = new List<GraphicPolyLine>();

        private void FormChild_GraphicEdit_KeyDown(object sender, KeyEventArgs e)
        {
            //int scrollStep = 100;
            //int controlMoveStep = 10;

            if (int.TryParse(tbMoveStep.Text, out int moveStep) == false)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Z:
                    {
                        if (_graphicEditor.CommandList.Count > 50)
                        {
                            _graphicEditor.CommandList.RemoveAt(0);
                        }

                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control
                            && (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                        {
                            //if (_graphicEditor.CommandList.Count > 0)
                            //{
                            //    _graphicEditor.CtrlZ_Index--;
                            //    //_graphicEditor.CtrlZ_Index--;

                            //    int idx = _graphicEditor.CommandList.Count - _graphicEditor.CtrlZ_Index;

                            //    if (idx >= 0 && idx < _graphicEditor.CommandList.Count)
                            //    {
                            //        _graphicEditor.SetList(_graphicEditor.CommandList[idx]);
                            //        Recipe.GraphicObjects = _graphicEditor.CommandList[idx];
                            //    }
                            //}
                            operateBack(false);
                        }
                        else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                        {
                            //if (_graphicEditor.CommandList.Count > 0)
                            //{
                            //    int idx = _graphicEditor.CommandList.Count - _graphicEditor.CtrlZ_Index;

                            //    if (idx >= 0 && idx < _graphicEditor.CommandList.Count)
                            //    {
                            //        _graphicEditor.SetList(_graphicEditor.CommandList[idx]);
                            //        _graphicEditor.CtrlZ_Index++;
                            //        Recipe.GraphicObjects = _graphicEditor.CommandList[idx];
                            //    }
                            //}
                            operateBack(true);
                        }
                    }
                    break;

                case Keys.Delete:
                    if (_selectedAction != null)
                    {
                        if (CUtil.ShowMessageBox("Check", "Do you want to Delete the object?"))
                        {
                            _selectedAction = null;
                        }
                    }
                    break;

                case Keys.W:
                    if (_selectedAction != null)
                    {
                        //_selectedAction.Move(0, -moveStep);
                        //_graphicEditor.CommandList.Add((Recipe_GraphicObject)_graphicEditor._recipe.Clone());
                    }
                    break;

                case Keys.S:
                    if (_selectedAction != null)
                    {
                        //_selectedAction.Move(0, moveStep);
                        //_graphicEditor.CommandList.Add((Recipe_GraphicObject)_graphicEditor._recipe.Clone());
                    }
                    break;

                case Keys.A:
                    if (_selectedAction != null)
                    {
                        //_selectedAction.Move(-moveStep, 0);
                        //_graphicEditor.CommandList.Add((Recipe_GraphicObject)_graphicEditor._recipe.Clone());
                    }
                    break;

                case Keys.D:
                    if (_selectedAction != null)
                    {
                        //_selectedAction.Move(moveStep, 0);
                        //_graphicEditor.CommandList.Add((Recipe_GraphicObject)_graphicEditor._recipe.Clone());
                    }
                    break;

                case Keys.C:
                    //_clipboard.Clear();
                    //foreach (var item in Recipe.GraphicObjects.GraphicList)
                    //{
                    //    if (item.Selected) _clipboard.Add(item.Clone());
                    //}
                    break;

                case Keys.V:
                    //int nBCount = Recipe.GraphicObjects.GraphicList.Count;
                    //foreach (var item in _clipboard)
                    //{
                    //    Recipe.GraphicObjects.GraphicList.Add(item.Clone());
                    //}
                    //int nACount = Recipe.GraphicObjects.GraphicList.Count;
                    //if (nBCount == nACount) return;
                    //for (int i = 0; i < _clipboard.Count; i++)
                    //{
                    //    RectangleF MoveRect = Recipe.GraphicObjects.GraphicList[nACount - 1 - i].GetBoundary();
                    //    int MoveX = (int)MoveRect.X - _graphicEditor.MousePos.X;
                    //    int MoveY = (int)MoveRect.Y - _graphicEditor.MousePos.Y;
                    //    Recipe.GraphicObjects.GraphicList[nACount - 1 - i].Move(-MoveX, -MoveY);
                    //}
                    //_clipboard.Clear();
                    break;
            }

            _graphicEditor.Invalidate();
        }

        private void uiSymbolButton5_Click(object sender, EventArgs e)
        {
            _graphicEditor.ActiveTool = EditDispensePath.DrawToolType.PolyLine;
        }

        private void timerInvalidate_Tick(object sender, EventArgs e)
        {
            // 항상 번호 보이도록 수정
            _graphicEditor.ViewOrder = true;
            //    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            //    {
            //        _graphicEditor.ViewOrder = true;
            //    }
            //    else
            //    {
            //        _graphicEditor.ViewOrder = false;
            //    }
        }

        private void btnPositionApply_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    _graphicEditor.Invalidate();
            //}
            //catch (Exception ex)
            //{

            //}

            if (_editingStartMeta == null)
            {
                // 편집중인 시작점이 없는 경우: 무시 또는 메시지
                // Sunny.UI 사용 시 UIMessageBox 사용 가능
                // UIMessageBox.ShowError("시작점을 더블클릭해서 선택해 주세요.");
                return;
            }

            var pl = _graphicEditor?.Recipe_DispPoints?.Polylines?
                .FirstOrDefault(x => x.Order == _editingStartMeta.PolylineOrder);
            if (pl == null) return;

            // 파싱/검증
            if (!double.TryParse(tbOpenTime.Text, out var openMs)) openMs = 0;
            if (!double.TryParse(tbCloseTime.Text, out var closeMs)) closeMs = 0;
            if (!int.TryParse(tbNumOfPulse.Text, out var pulse)) pulse = 0;

            // (선택) 간단 검증
            if (openMs < 0) openMs = 0;
            if (closeMs < 0) closeMs = 0;
            if (pulse < 0) pulse = 0;

            // 저장
            pl.OpenTimeMs = openMs;
            pl.CloseTimeMs = closeMs;
            pl.NumOfPulse = pulse;

            // (선택) 저장 후 비활성/해제
            //SetStartParamEditorsEnabled(false);
            _editingStartMeta = null;

            // 필요 시 화면/그리드 갱신
            // _graphicEditor.RedrawComposite();   // 파라미터 값이 그림에 반영된다면
            // RefreshGraphicGrid();               // 그리드에 표시할 값이 있다면
        }


        private void timerStatus_Tick(object sender, EventArgs e)
        {
            try
            {
                //if (Global.Device.Plc.IsOpen)
                //{
                //    float value1 = CDIO_ADDR.IN_W2100_LVDT_POINT01.Current / 1000.0F;
                //    float value2 = CDIO_ADDR.IN_W2102_LVDT_POINT02.Current / 1000.0F;
                //    float value3 = CDIO_ADDR.IN_W2104_LVDT_POINT03.Current / 1000.0F;
                //    float value4 = CDIO_ADDR.IN_W2106_LVDT_POINT04.Current / 1000.0F;
                //    float value5 = CDIO_ADDR.IN_W2108_LVDT_POINT05.Current / 1000.0F;

                //    lblSpec1.Text = value1.ToString("F4");
                //    lblSpec2.Text = value2.ToString("F4");
                //    lblSpec3.Text = value3.ToString("F4");
                //    lblSpec4.Text = value4.ToString("F4");
                //    lblSpec5.Text = value5.ToString("F4");
                //}
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }

        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            _graphicEditor.ZoomIn();
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            _graphicEditor.ZoomOut();
        }

        private void btnZoomFit_Click(object sender, EventArgs e)
        {
            _graphicEditor.ZoomFit();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            //WindowState = FormWindowState.Minimized;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            //this.Close();
        }

        private void btnSaveParam_Click(object sender, EventArgs e)
        {
            _graphicEditor.Recipe_DispPoints.PixelSizeX = tbPixelSizeX.DoubleValue;
            _graphicEditor.Recipe_DispPoints.PixelSizeY = tbPixelSizeY.DoubleValue;

            _graphicEditor.Recipe_DispPoints.Save(txtRecipeName.Text);
            CUtil.ShowMessageBox("Notify", "Save Completed");
        }

        private void btnLoadParam_Click(object sender, EventArgs e)
        {
            LoadParam();

            _graphicEditor.ZoomFit();
        }

        private void ClearAll()
        {
            if (_graphicEditor?.Recipe_DispPoints != null)
            {
                _graphicEditor.Recipe_DispPoints.Nodes.Clear();
                //_graphicEditor.Recipe_DispPoints.Lines.Clear();
                _graphicEditor.Recipe_DispPoints.Polylines.Clear();
                _graphicEditor.RedrawComposite();
            }

            // ← DataGridView도 함께 초기화
            gridGraphicNodes.SuspendLayout();
            gridGraphicNodes.Rows.Clear();
            gridGraphicNodes.ClearSelection();   // 선택 표시 제거(옵션)
            gridGraphicNodes.ResumeLayout();
            // gridGraphicNodes.Refresh();       // 필요 시 강제 리프레시(옵션)
        }

        private void btnCreateParam_Click(object sender, EventArgs e)
        {
            //Recipe.GraphicObjects.actionInfos = new List<ActionInfo>();

            if (CUtil.ShowMessageBox("Clear", "Do you want to clear?"))
            {
                ClearAll();
            }
        }

        private void btnHorzFlip_Click(object sender, EventArgs e)
        {
            //if (_selectedAction != null)
            //{
            //    _selectedAction.HorzFlip();
            //}
        }

        private void btnVertFlip_Click(object sender, EventArgs e)
        {
            //if (_selectedAction != null)
            //{
            //    _selectedAction.VertFlip();
            //}
        }

        private void FormChild_GraphicEditv2_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            if (_isSave == false)
            {

            }
        }

        private void operateBack(bool bBackOrForward)
        {
            if (_graphicEditor.CommandList.Count > 50)
            {
                _graphicEditor.CommandList.RemoveAt(0);
            }
            if (_graphicEditor.CommandList.Count > 0)
            {
                if (bBackOrForward && _graphicEditor.CtrlZ_Index < _graphicEditor.CommandList.Count)
                {
                    _graphicEditor.CtrlZ_Index++;
                }
                else if (!bBackOrForward && _graphicEditor.CtrlZ_Index > 0)
                {
                    _graphicEditor.CtrlZ_Index--;
                }
                else
                    return;

                int idx = _graphicEditor.CommandList.Count - _graphicEditor.CtrlZ_Index;

                if (idx >= 0 && idx < _graphicEditor.CommandList.Count)
                {
                    _graphicEditor.SetList(_graphicEditor.CommandList[idx]);
                }
            }
        }

        private void uiSymbolButton3_Click(object sender, EventArgs e)
        {
            operateBack(true);
        }

        private void uiSymbolButton4_Click(object sender, EventArgs e)
        {
            operateBack(false);
        }

        private void grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_selectedAction == null) return;

                SelectedPoint = e.RowIndex;             
            }
            catch
            {
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                //Recipe.GraphicObjects.actionInfos.Add(new ActionInfo());
                //UpdateMotorAction();
                //_graphicEditor.Invalidate();

                int lastIndex = gridGraphicNodes.Rows.Count - 1;
                if (lastIndex >= 0)
                {
                    gridGraphicNodes.CurrentCell = gridGraphicNodes.Rows[lastIndex].Cells[0];
                    SelectedPoint = lastIndex;
                }

                _graphicEditor.Invalidate();
            }
            catch (Exception ex)
            {

            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                _graphicEditor.Invalidate();
            }
            catch (Exception ex)
            {

            }
            
        }

        private void btnLVDTOffSet_Click(object sender, EventArgs e)
        {
            if (_selectedAction == null)
                return;
            //From_LVDTOffSet = new LVDTOffSet();
            //From_LVDTOffSet.szFormula = UnescapeExpression(_selectedAction.szLVDTOffSet);
            //From_LVDTOffSet.LVDTOffSetFormClosing += OnLVDTOffSetClosed;
            //From_LVDTOffSet.ShowDialog();
        }

        public void OnLVDTOffSetClosed(object sender, EventArgs e)
        {
            //if (From_LVDTOffSet.isChanged)
            //{
            //    _selectedAction.szLVDTOffSet = EscapeExpression(From_LVDTOffSet.szFormula);
            //}
        }

        private string EscapeExpression(string expression)
        {
            return expression.Replace("+", "\\u002B")
                             .Replace("-", "\\u002D")
                             .Replace("*", "\\u002A")
                             .Replace("/", "\\u002F")
                             .Replace("(", "\\u0028")
                             .Replace(")", "\\u0029");
        }

        private void uiSymbolButton2_Click(object sender, EventArgs e)
        {
            _graphicEditor.ActiveTool = EditDispensePath.DrawToolType.AddPoint;
        }

        private void uiSymbolButton1_Click(object sender, EventArgs e)
        {
            _graphicEditor.ActiveTool = EditDispensePath.DrawToolType.Pointer;
        }


        //=================================================================================================================
        // 아래는 테스트(Dispense Path에 Align 보정을 적용 후 로직 검증)을 위한 코드임
        //=================================================================================================================

        // [2025-8-28][sglee] Align 보정 관련 데이터
        // Reference Pattern Origin Point [pixel]
        static double ref_x0 = 0;
        static double ref_y0 = 0;
        // Actual Pattern Origin Point [pixel]
        //static double x0 = 0;
        //static double y0 = 0;
        // Align Compensation [pixel]
        static double dx = 0;
        static double dy = 0;
        static double theta = 0;
        static List<Point3D> alignCompA = new List<Point3D>(); // A-Head의 한 Pallet의 전체 Align Compensation(dx,dy,theta)
        static List<Point3D> alignCompB = new List<Point3D>(); // B-Head의 한 Pallet의 전체 Align Compensation(dx,dy,theta)
        // Actual Path Point [pixel]
        //static double x = 0;
        //static double y = 0;
        // Compensated Path Point [pixel]
        //static double cvt_x = 0;
        //static double cvt_y = 0;
        // Path Distance From Center [pixel]
        //static double dist_x = 0;
        //static double dist_y = 0;
        static List<Point> distPathInA = new List<Point>(); // A-Head의 한 Pallet의 전체 PointF(dist_x,dist_y)
        static List<Point> distPathInB = new List<Point>(); // B-Head의 한 Pallet의 전체 PointF(dist_x,dist_y)
        // Center Point [pixel]
        static double center_x = 1215.5; // 테스트용 이미지의 해상도가 (2431 x 2040)인 경우
        static double center_y = 1020;
        // Unit Center Position [mm]
        static double unit_x = 0;
        static double unit_y = 0;
        // Camera to Dispense Offset [mm]
        static double cam2disp_x = 30; // 테스트 값임
        static double cam2disp_y = 0;
        // Dispense Center Position [mm]
        //static double head_x = 0;
        //static double head_y = 0;
        // Pixel Resolution [mm/pixel]
        static double pixelresol_x = 0.01;
        static double pixelresol_y = 0.01;


        internal static void SetAlignRefOrg(int head_index, double refX0, double refY0)
        {
            ref_x0 = refX0;
            ref_y0 = refY0;
        }

        internal static void AddAlignComp(int head_index, double dX, double dY, double Theta)
        {
            if (head_index == 0)
                alignCompA.Add(new Point3D(dX, dY, Theta));
            else
                alignCompB.Add(new Point3D(dX, dY, Theta));
        }

        internal static void ClearAlignComp(int head_index)
        {
            if (head_index == 0)
                alignCompA.Clear();
            else
                alignCompB.Clear();
        }

        // head_index = { 0: A-Head, 1: B-Head }
        // unit_index = 0 ~ (base: 1)
        internal static (double cvt_x, double cvt_y) CompensatePathPoint(int head_index, int unit_index, double x, double y)
        {
            //Point3D alignComp = new Point3D(0, 0, 0);
            //if (head_index == 0 && alignCompA.Count > unit_index)
            //    alignComp = alignCompA[unit_index];
            //else if (head_index == 1 && alignCompB.Count > unit_index)
            //    alignComp = alignCompB[unit_index];
            //dx = alignComp.X;
            //dy = alignComp.Y;
            //theta = alignComp.Z;

            // [2025-9-4] 수식 오류 수정
            double cvt_x = Math.Cos(theta) * (x - ref_x0) - Math.Sin(theta) * (y - ref_y0) + ref_x0 + dx;
            double cvt_y = Math.Sin(theta) * (x - ref_x0) + Math.Cos(theta) * (y - ref_y0) + ref_y0 + dy;
            return (cvt_x, cvt_y);
        }

        internal static void SetCamCenter(double cam_centerX, double cam_centerY)
        {
            center_x = cam_centerX;
            center_y = cam_centerY;
        }

        internal static (double dist_x, double dist_y) GetPathDistance(double cvt_x, double cvt_y)
        {
            double dist_x = cvt_x - center_x;
            double dist_y = cvt_y - center_y;
            return (dist_x, dist_y);
        }

        internal static void SetCam2DispOffset(double cam2dispX, double cam2dispY)
        {
            cam2disp_x = cam2dispX;
            cam2disp_y = cam2dispY;
        }

        internal static (double head_x, double head_y) GetHeadPosition(double unit_x, double unit_y)
        {
            double head_x = unit_x + cam2disp_x;
            double head_y = unit_y + cam2disp_y;
            return (head_x, head_y);
        }

        internal static void SetPixelResol(double pixelresolX, double pixelresolY)
        {
            pixelresol_x = pixelresolX;
            pixelresol_y = pixelresolY;
        }

        // head_index = { 0: A-Head, 1: B-Head }
        internal static (double align_x, double align_y) GetAlignOffset(int head_index, double dist_x, double dist_y)
        {
            double align_x = head_index == 0 ? -dist_x * pixelresol_x : dist_x * pixelresol_x;
            double align_y = dist_y * pixelresol_y;
            return (align_x, align_y);
        }

        internal static (double x_pos, double y_pos) GetDispensePos(double head_x, double head_y, double align_x, double align_y)
        {
            double x_pos = head_x + align_x;
            double y_pos = head_y + align_y;
            return (x_pos, y_pos);
        }

        private void btnLoadTestImage_Click(object sender, EventArgs e)
        {
            if (CUtil.ShowMessageBox("Load Test Image", "All data will be remained. Do you want to load a test image?"))
            {
                OpenFileDialog loadFileDialog = new OpenFileDialog();
                loadFileDialog.Filter = "Image File (*.bmp)|*.bmp|All File (*.*)|*.*";
                loadFileDialog.Title = "Select Path to load a new image";

                if (loadFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //ClearAll();
                    _graphicEditor.Recipe_DispPoints.BackgroundImagePath = loadFileDialog.FileName;
                    _graphicEditor.UpdateBackgroundImage();
                    _graphicEditor.Invalidate();
                }
            }

        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            dx = double.Parse(tbDx.Text);
            dy = double.Parse(tbDy.Text);
            theta = double.Parse(tbTheta.Text) * Math.PI / 180.0;

            // [2025-9-4] 회전 원점 가져오기
            // 3) Recipe로부터 Dispense Path의 회전 원점을 가져온다 (이미지 위치 [pixel])
            double refX0 = 0;
            double refY0 = 0;
            if (_graphicEditor.Recipe_DispPoints.Nodes.Count > 0)
            {
                refX0 = _graphicEditor.Recipe_DispPoints.Nodes[0].Position.X;
                refY0 = _graphicEditor.Recipe_DispPoints.Nodes[0].Position.Y;
            }
            SetAlignRefOrg(0, refX0, refY0);

            // 4) Recipe로부터 Dispense Path의 Polyline을 가져온다 (이미지 위치 [pixel])
            List<DispensePolyline> PolylinesInAlign = new List<DispensePolyline>();
            foreach (DispensePolyline polyline in _graphicEditor.Recipe_DispPoints.Polylines)
            {
                DispensePolyline polyline_align = new DispensePolyline();
                // 5) 이제 Polyline을 순회하면서 각 Point값을 가져온다 (이미지 위치 [pixel])
                foreach (PointF pointf in polyline.Points)
                {
                    // 6) Align 보정값을 적용한다 (이미지 위치 [pixel])
                    (double cvt_x, double cvt_y) = CompensatePathPoint(0, 0, pointf.X, pointf.Y);

                    //////// 7) 기구 위치로 변환(매핑)하기 위해 카메라 중심으로부터의 거리를 계산한다 (이미지 위치 [pixel])
                    //////(double dist_x, double dist_y) = GetPathDistance(cvt_x, cvt_y);
                    //////// 8) 기구적 거리 옵셋으로 변환한다 (기구적 위치 [mm])
                    //////(double align_x, double align_y) = GetAlignOffset(0, dist_x, dist_y);
                    //////// 9) head_pos(디스펜서 기준)에 기구적 거리 옵셋을 더하여 최종 위치를 구한다 (기구적 위치 [mm])
                    //////(double x_pos, double y_pos) = GetDispensePos(head_x, head_y, align_x, align_y);
                    
                    polyline_align.Points.Add(new PointF((float)cvt_x, (float)cvt_y));
                }
                PolylinesInAlign.Add(polyline_align);
            }

            ClearAll();

            _graphicEditor.Recipe_DispPoints.Nodes.Add( new DispensePointNode(new PointF((float)(ref_x0 + dx), (float)(ref_y0 + dy))));
            foreach (DispensePolyline polyline_align in PolylinesInAlign)
            {
                _graphicEditor.Recipe_DispPoints.Polylines.Add(polyline_align);
            }
            _graphicEditor.UpdateBackgroundImage();
            _graphicEditor.Invalidate();

            RefreshGraphicGrid();
        }
    }
}