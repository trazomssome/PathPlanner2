using Newtonsoft.Json;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;
using Point = System.Drawing.Point;

namespace DispensePath
{
    public partial class FormEditFiguer : UserControl
    {
        #region Members

        private bool _controlKey = false;
        private bool _panMode = false;
        private GraphicPolyLine temp = new GraphicPolyLine();
        // 일단은 도형은 4개만 사용 한다.
        public GraphicPolyLine[] figuerList = new GraphicPolyLine[4];

        private GraphicPolyLine[] _original = null;

        //public LVDTOffSet From_LVDTOffSet = new LVDTOffSet();

        #endregion Members

        private EditFiguer _EditFiguer;
        private bool _isSave = false;
        private DispensePointNode _selectedNode = null;

        public Setting_Recipe Recipe = null;

        // User Control에서 이걸로 보내준다.
        private GraphicPolyLine _selectedGraphic = null;
        private string recipeName = "";
        private int curFiguerNum = 0;

        public FormEditFiguer(string recipe_name, string file_name)
        {
            InitializeComponent();

            Recipe = new Setting_Recipe(recipe_name, file_name);
            Recipe = Recipe.Load(recipe_name, file_name);
            //for(int i = 0; i < figuerList.Length; i++)
            //{
            //    figuerList[i] = new GraphicPolyLine();
            //}

            figuerList = LoadFiguerData(recipe_name);
            SetOrgData();


            _EditFiguer = new EditFiguer(recipe_name, file_name); 
            _EditFiguer.Dock = DockStyle.Fill;
            _EditFiguer.Visible = true;

            plPathDraw.Controls.Add(_EditFiguer);
        }


        private void Form_EditFiguer_Load(object sender, EventArgs e)
        {
            _EditFiguer.EventFiguerClicked += OnFiguerClicked;
            _EditFiguer.EventPositionClicked += OnPositionClicked;
            _EditFiguer.EventMouseClicked += OnMouseClicked;

            //KeyPreview = true;
            this.Focus();
            _EditFiguer.Init(figuerList[0], recipeName);

            InitUI();
            cbFigure.SelectedIndex = curFiguerNum;
            this.AutoScaleMode = AutoScaleMode.None;
        }

        private void SetOrgData()
        {
            try
            {
                if (figuerList == null) return;
                _original = new GraphicPolyLine[4];
                for (int i = 0; i < 4; i++)
                {
                    _original[i] = new GraphicPolyLine();
                    for (int j = 0; j < figuerList[i].Nodes.Count; j++)
                    {
                        _original[i].Nodes.Add(figuerList[i].Nodes[j]);
                    }
                }
            }
            catch(Exception ex)
            {

            }
        }


        private void InitUI()
        {
            try
            {               
                 // Grid에 현재 값 표시
                _selectedGraphic = new GraphicPolyLine();
                _selectedGraphic = figuerList[curFiguerNum];
                UpdateSelected();
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }

        private void MouseWheelEventSource(object sender, MouseEventArgs e)
        {
        }        

        private void OnFiguerClicked(object sender, GraphicEventArgs e)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        OnFiguerClicked(sender, e);
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
                    if (e.Graphic != null)
                    {
                        _selectedGraphic = e.Graphic;
                        UpdateSelected();
                    }
                }
                catch (Exception ex)
                {
                    //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
                }
            }
        }

        private void UpdateSelected()
        {
            try
            {
                dgvFiguer.Rows.Clear();
                for (int i = 0; i < _selectedGraphic.Nodes.Count; i++)
                {
                    DispensePointNode node = _selectedGraphic.Nodes[i];
                    string use = node.Use ? "O" : "X";
                    dgvFiguer.Rows.Add(new string[] { (i + 1).ToString(), $"{node.Position.X * Recipe.Align.PixelSize_X} mm", $"{node.Position.Y * Recipe.Align.PixelSize_Y} mm", 
                        node.OffsetZ.ToString(), node.Speed.ToString() + " mm/s", use });
                }
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }

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
                        //_selectedNode.Position = new PointF(e.Node.Position.X * 10, e.Node.Position.Y * 10);

                        //UpdateSelected();
                    }


                }
                catch (Exception ex)
                {
                    //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
                }
            }
        }

        private void OnMouseClicked(object sender, EvenMousePosArgs e)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        OnMouseClicked(sender, e);
                    }));
                }
                catch (Exception ex)
                {
                }
            }
            else
            {
                try
                {
                    if (_selectedNode != null)
                    {
                        if (Recipe == null) return;

                        _selectedNode.Position = new PointF(e.mousePos.X, e.mousePos.Y);

                        UpdateSelected();


                        //Recipe.GraphicObjects.actionInfos[SelectedPoint].Position = e.mousePos;
                        //UpdateMotorAction();
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (CUtil.ShowMessageBox("Save", "Do you want to Save?"))
                {
                    SaveFiguerData(recipeName);

                    _isSave = true;
                }
            }
            catch (Exception ex)
            {
                //CLogger.Add(LOG.EXCEPTION, "[FAILED] {0}==>{1}   Execption ==> {2}", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name, ex.ToString());
                CUtil.ShowMessageBox("EXCEPTION", $"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {ex.Message}");
            }
        }

        private void SaveFiguerData(string model_name)
        {
            //Recipe.Save(Recipe.Name);
            string path = $"D:\\[SGMachineFDP]\\Recipe\\_config_recipe\\Figuer.json";
            string json = JsonConvert.SerializeObject(figuerList, Formatting.Indented);
            // 파일로 저장
            File.WriteAllText(path, json);
            UINotifier.Show($"Save Complete");
        }

        private GraphicPolyLine[] LoadFiguerData(string model_name)
        {
            string path = $"D:\\[SGMachineFDP]\\Recipe\\_config_recipe\\Figuer.json";
            //string path = $"{Application.StartupPath}\\RECIPE\\{model_name}\\Figuer.json";
            recipeName = model_name;
            GraphicPolyLine[] loadedList = null;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // 객체로 역직렬화
                loadedList = JsonConvert.DeserializeObject<GraphicPolyLine[]>(json);
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

        private void FormChild_EditFiguer_KeyDown(object sender, KeyEventArgs e)
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
                        if (_EditFiguer.CommandList.Count > 50)
                        {
                            _EditFiguer.CommandList.RemoveAt(0);
                        }

                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control
                            && (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                        {
                            //if (_EditFiguer.CommandList.Count > 0)
                            //{
                            //    _EditFiguer.CtrlZ_Index--;
                            //    //_EditFiguer.CtrlZ_Index--;

                            //    int idx = _EditFiguer.CommandList.Count - _EditFiguer.CtrlZ_Index;

                            //    if (idx >= 0 && idx < _EditFiguer.CommandList.Count)
                            //    {
                            //        _EditFiguer.SetList(_EditFiguer.CommandList[idx]);
                            //        Recipe.GraphicObjects = _EditFiguer.CommandList[idx];
                            //    }
                            //}
                            operateBack(false);
                        }
                        else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                        {
                            //if (_EditFiguer.CommandList.Count > 0)
                            //{
                            //    int idx = _EditFiguer.CommandList.Count - _EditFiguer.CtrlZ_Index;

                            //    if (idx >= 0 && idx < _EditFiguer.CommandList.Count)
                            //    {
                            //        _EditFiguer.SetList(_EditFiguer.CommandList[idx]);
                            //        _EditFiguer.CtrlZ_Index++;
                            //        Recipe.GraphicObjects = _EditFiguer.CommandList[idx];
                            //    }
                            //}
                            operateBack(true);
                        }
                    }
                    break;

                case Keys.Delete:
                    if (_selectedGraphic != null)
                    {
                        if (CUtil.ShowMessageBox("Check", "Do you want to Delete the object?"))
                        {
                            //Recipe.GraphicObjects.GraphicList.Remove(_selectedGraphic);
                            figuerList[curFiguerNum].Nodes.Clear();
                            _selectedGraphic = null;
                        }
                    }
                    break;

                case Keys.W:
                    if (_selectedGraphic != null)
                    {
                        _selectedGraphic.Move(0, -moveStep);
                        //_EditFiguer.CommandList.Add((Recipe_GraphicObject)_EditFiguer._figure.Clone());
                    }
                    break;

                case Keys.S:
                    if (_selectedGraphic != null)
                    {
                        _selectedGraphic.Move(0, moveStep);
                        //_EditFiguer.CommandList.Add((Recipe_GraphicObject)_EditFiguer._recipe.Clone());
                    }
                    break;

                case Keys.A:
                    if (_selectedGraphic != null)
                    {
                        _selectedGraphic.Move(-moveStep, 0);
                        //_EditFiguer.CommandList.Add((Recipe_GraphicObject)_EditFiguer._recipe.Clone());
                    }
                    break;

                case Keys.D:
                    if (_selectedGraphic != null)
                    {
                        _selectedGraphic.Move(moveStep, 0);
                        //_EditFiguer.CommandList.Add((Recipe_GraphicObject)_EditFiguer._recipe.Clone());
                    }
                    break;

                case Keys.C:
                    _clipboard.Clear();
                    foreach (var item in figuerList[curFiguerNum].Nodes)
                    {
                        //if (item.Selected) _clipboard.Add(item.Clone());
                    }
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
                    //    int MoveX = (int)MoveRect.X - _EditFiguer.MousePos.X;
                    //    int MoveY = (int)MoveRect.Y - _EditFiguer.MousePos.Y;
                    //    Recipe.GraphicObjects.GraphicList[nACount - 1 - i].Move(-MoveX, -MoveY);
                    //}
                    //_clipboard.Clear();
                    break;
            }

            _EditFiguer.Invalidate();
        }

        private void uiSymbolButton5_Click(object sender, EventArgs e)
        {
            _EditFiguer.ActiveTool = EditFiguer.DrawToolType.PolyLine;
            if(figuerList[curFiguerNum] != null && figuerList[curFiguerNum].Nodes != null && figuerList[curFiguerNum].Nodes.Count > 0)
                figuerList[curFiguerNum].Nodes.Clear();
            //Recipe.GraphicObjects.GraphicList.Add(new GraphicPolyLine());
        }

        private void timerInvalidate_Tick(object sender, EventArgs e)
        {
            // 항상 번호 보이도록 수정
            _EditFiguer.ViewOrder = true;
            //    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            //    {
            //        _EditFiguer.ViewOrder = true;
            //    }
            //    else
            //    {
            //        _EditFiguer.ViewOrder = false;
            //    }
        }

        private void btnPositionApply_Click(object sender, EventArgs e)
        {
            try
            {
                if (_selectedNode != null)
                {
                    //_selectedNode.Position = new PointF(float.Parse(tbPositionX.Text) * 10, float.Parse(tbPositionY.Text) * 10);
                    //_selectedNode.Speed = double.Parse(tbSpeed.Text);
                    _selectedNode.Use = btnUse.FillColor == Color.Green ? true : false;
                    //_selectedNode.OffsetZ = double.Parse(tbOffsetZ.Text);
                }
                //if (From_LVDTOffSet.isChanged)
                //{
                //    _selectedGraphic.szLVDTOffSet = From_LVDTOffSet.szFormula;
                //}
                UpdateSelected();
                _EditFiguer.Invalidate();
            }
            catch
            {
            }
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
            _EditFiguer.ZoomIn();
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            _EditFiguer.ZoomOut();
        }

        private void btnZoomFit_Click(object sender, EventArgs e)
        {
            _EditFiguer.ZoomFit();
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
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON File (*.json)|*.json|All File (*.*)|*.*";
            saveFileDialog.Title = "Select Paht to Save File";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string saveFilePath = saveFileDialog.FileName;

                string json = JsonConvert.SerializeObject(figuerList, Formatting.Indented);
                File.WriteAllText(saveFilePath, json);
            }
        }

        private void btnLoadParam_Click(object sender, EventArgs e)
        {
            OpenFileDialog loadFileDialog = new OpenFileDialog();
            loadFileDialog.Filter = "JSON File (*.json)|*.json|All File (*.*)|*.*";
            loadFileDialog.Title = "Select Paht to Load File";

            if (loadFileDialog.ShowDialog() == DialogResult.OK)
            {
                figuerList = LoadFiguerData(loadFileDialog.FileName);
                SetOrgData();

                //string loadFilePath = loadFileDialog.FileName;

                //string json = File.ReadAllText(loadFilePath);

                //figuerList = JsonConvert.DeserializeObject<List<GraphicPolyLine>>(json);
            }
        }

        private void btnCreateParam_Click(object sender, EventArgs e)
        {
            if (curFiguerNum >= 0)
            {
                figuerList[curFiguerNum].Nodes.Clear();
                _EditFiguer._figure = figuerList[curFiguerNum];
                InitUI();
            }
        }

        private void btnHorzFlip_Click(object sender, EventArgs e)
        {
            if (_selectedGraphic != null)
            {
                _selectedGraphic.HorzFlip();
            }
        }

        private void btnVertFlip_Click(object sender, EventArgs e)
        {
            if (_selectedGraphic != null)
            {
                _selectedGraphic.VertFlip();
            }
        }

        private void FormChild_EditFiguerv2_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            if (_isSave == false)
            {
               //Recipe.GraphicObjects = _original.Clone();
            }
        }

        private void operateBack(bool bBackOrForward)
        {
            if (_EditFiguer.CommandList.Count > 50)
            {
                _EditFiguer.CommandList.RemoveAt(0);
            }
            if (_EditFiguer.CommandList.Count > 0)
            {
                if (bBackOrForward && _EditFiguer.CtrlZ_Index < _EditFiguer.CommandList.Count)
                {
                    _EditFiguer.CtrlZ_Index++;
                }
                else if (!bBackOrForward && _EditFiguer.CtrlZ_Index > 0)
                {
                    _EditFiguer.CtrlZ_Index--;
                }
                else
                    return;

                int idx = _EditFiguer.CommandList.Count - _EditFiguer.CtrlZ_Index;

                if (idx >= 0 && idx < _EditFiguer.CommandList.Count)
                {
                    _EditFiguer.SetList(_EditFiguer.CommandList[idx]);
                    //Recipe.GraphicObjects = _EditFiguer.CommandList[idx];
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
                if (_selectedGraphic == null) return;

                DispensePointNode node = _selectedNode = _selectedGraphic.Nodes[e.RowIndex];

                _selectedGraphic.SelectedPoint = e.RowIndex;

                tbPositionX.Text = (node.Position.X * Recipe.Align.PixelSize_X).ToString();
                tbPositionY.Text = (node.Position.Y * Recipe.Align.PixelSize_Y).ToString();

                if (node.Use)
                {
                    btnUse.FillColor = Color.Green;
                }
                else
                {
                    btnUse.FillColor = Color.Transparent;
                }
            }
            catch
            {
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            _selectedGraphic?.Nodes.Add(new DispensePointNode());
            UpdateSelected();
            _EditFiguer.Invalidate();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            _selectedGraphic?.Nodes.RemoveAt(_selectedGraphic.SelectedPoint);
            UpdateSelected();
            _EditFiguer.Invalidate();
        }

        private void btnMoveIndex_Click(object sender, EventArgs e)
        {
            int nIndex;
            if (!int.TryParse(tbIndex.Text, out nIndex) || nIndex <= 0)
            {
                System.Windows.Forms.MessageBox.Show("Input Error");
                return;
            }
            if (nIndex > _selectedGraphic.Nodes.Count)
            {
                System.Windows.Forms.MessageBox.Show("Index out of range");
                return;
            }
            nIndex--;
            if (nIndex >= 0 && nIndex < _selectedGraphic.Nodes.Count)
            {
                DispensePointNode temp = _selectedGraphic.Nodes[_selectedGraphic.SelectedPoint].Clone(false);
                _selectedGraphic.Nodes.RemoveAt(_selectedGraphic.SelectedPoint);
                _selectedGraphic.Nodes.Insert(nIndex, temp);
                _selectedGraphic.SelectedPoint = nIndex;
            }
            UpdateSelected();
            dgvFiguer.CurrentCell = dgvFiguer.Rows[nIndex].Cells[0];
            _EditFiguer.Invalidate();
        }

        private void btnLVDTOffSet_Click(object sender, EventArgs e)
        {
            if (_selectedGraphic == null)
                return;
            //From_LVDTOffSet = new LVDTOffSet();
            //From_LVDTOffSet.szFormula = UnescapeExpression(_selectedGraphic.szLVDTOffSet);
            //From_LVDTOffSet.LVDTOffSetFormClosing += OnLVDTOffSetClosed;
            //From_LVDTOffSet.ShowDialog();
        }

        public void OnLVDTOffSetClosed(object sender, EventArgs e)
        {
            //if (From_LVDTOffSet.isChanged)
            //{
            //    _selectedGraphic.szLVDTOffSet = EscapeExpression(From_LVDTOffSet.szFormula);
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

        private string UnescapeExpression(string escapedExpression)
        {
            return escapedExpression.Replace("\\u002B", "+")
                                   .Replace("\\u002D", "-")
                                   .Replace("\\u002A", "*")
                                   .Replace("\\u002F", "/")
                                   .Replace("\\u0028", "(")
                                   .Replace("\\u0029", ")");
        }

        private void cbFigure_SelectedIndexChanged(object sender, EventArgs e)
        {
            curFiguerNum = cbFigure.SelectedIndex;
            // 선택된 Figuer를 보여준다.
            //_EditFiguer.Init(figuerList[curFiguerNum], recipeName);
            _EditFiguer._figure = figuerList[curFiguerNum];
            InitUI();
        }

        private void btnUse_Click(object sender, EventArgs e)
        {
            if (btnUse.FillColor != Color.Green)
            {
                btnUse.FillColor = Color.Green;
            }
            else
            {
                btnUse.FillColor = Color.Transparent;
            }
        }
    }
}