using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.Implementation;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Xml.Serialization;
using System.Xml.Linq;
using QWhale.Editor.TextSource;
using static System.Windows.Forms.AxHost;
using System.Drawing.Printing;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using QWhale.Common;
//using Accord.Math.Geometry;

namespace CogDrawTool
{
    public partial class DrawToolFrm : Form
    {
        private DataTable defectTable;
        private String defectCategory;
        private String _path = "C:/Users/ryuya/Downloads";
        private List<ICogGraphicParentChild> shapeContainer = new List<ICogGraphicParentChild>();
        //Drawing
        private bool drawingMode;
        private bool mouseDown;
        private double previousX = -1;
        private double previousY = -1;
        private CogCompositeShape drawing;
        //Multi-Press Tools
        private int multiToolSteps;
        private List<Tuple<double, double>> multiToolPositions = new List<Tuple<double, double>>();
        private bool changing = false;
        //Shape properties
        private int lineWidth;
        private CogColorConstants lineColor;
        private CogColorConstants dotColor;

        //CompositeShape id - 5, 6, 7, 8, 9, 10

        public delegate void DisplayImageDelegate(Bitmap frameBuffer);

        //Test variables
        CogCompositeShape temp = new CogCompositeShape();
        int steps = 0;
        
        //Not used
        private List<CogRectangleAffine> listDefectList;
        private List<CogGraphicLabel> labelList;
        private List<CogPointMarker> pointList;
        private List<CogLineSegment> lineList;
        private List<CogCompositeShape> tempDefectList = new List<CogCompositeShape>();
        CogCompositeShape shapes = new CogCompositeShape();

        public DrawToolFrm()
        {
            InitializeComponent();
        }

        

        private void btnTest_Click(object sender, EventArgs e)
        {
            CogRectangle shape = new CogRectangle();
            shape.SetCenterWidthHeight(500, 500, 1, 1);
            shape.LineWidthInScreenPixels = 10;
            cogDisplay1.InteractiveGraphics.Add(shape, "Temp", false);

            CogCircle shape2 = new CogCircle();
            shape2.SetCenterRadius(510, 500, 1);
            shape2.LineWidthInScreenPixels = 5;
            cogDisplay1.InteractiveGraphics.Add(shape2, "temp", false);

            CogPointMarker shape3 = new CogPointMarker();
            shape3.SetCenterRotationSize(520, 500, 0, 5);
            cogDisplay1.InteractiveGraphics.Add(shape3, "temp", false);

            CogRectangleAffine shape4 = new CogRectangleAffine();
            shape4.SetCenterLengthsRotationSkew(530, 500, 1, 1, 0, 0);
            shape4.LineWidthInScreenPixels = 10;
            cogDisplay1.InteractiveGraphics.Add(shape4, "temp", false);

            CogLine shape5 = new CogLine();
            shape5.SetFromStartXYEndXY(540, 500, 540, 501);
            cogDisplay1.InteractiveGraphics.Add(shape5, "temp", false);

            CogLineSegment shape6 = new CogLineSegment();
            shape6.SetStartLengthRotation(550, 500, 1, 0);
            cogDisplay1.InteractiveGraphics.Add(shape6, "temp", false);

            CogLineSegment shape7 = new CogLineSegment();
            shape7.SetStartLengthRotation(560, 500, 0.5, 0);
            shape7.LineWidthInScreenPixels = 10;
            shape7.Interactive = true;
            cogDisplay1.InteractiveGraphics.Add(shape7, "temp", false);

            

        }

        private void btnTest2_Click(object sender, EventArgs e)
        {
            Console.WriteLine("BUTTON 2");
            if (shapeContainer[0].GetType() == typeof(CogCompositeShape))
            {
                CogCompositeShape compositeShape = (CogCompositeShape)shapeContainer[0];
                
                CogLineSegment line = (CogLineSegment)compositeShape.Shapes[1];
            }
        }

        private void btnTest3_Click(object sender, EventArgs e)
        {
            Console.WriteLine("BUTTON 3");

            
        }

        private void DrawToolFrm_Load(object sender, EventArgs e)
        {
            cogDisplay1.RenderEngine = CogRenderEngineConstants.CogRenderEngine;
            cogDisplay1.BackColor = Color.Black;
            cogDisplayStatusBarV21.Display = cogDisplay1;
            cogDisplayStatusBarV21.AllowUserSpaceChange = false;

            cogDisplayToolbarV21.Display = cogDisplay1;
            //cogIntCont = cogDisplay1.InteractiveGraphics;

            //Initial defect category
            defectCategory = "Scratch";

            //Display image
            Bitmap bmp = new Bitmap("C:/Users/ryuya/Downloads/Screenshot_20230103_020226.png");
            DisplayImage(bmp);

            //LineWidth
            UpDownLineWidth.Maximum = 10;
            UpDownLineWidth.Minimum = 1;
            lineWidth = (int)UpDownLineWidth.Value;
            //Drawing mode
            drawingMode = false;
            mouseDown = false;
            btnDraw.BackColor = Color.Green;
            //Multi-Press tools settings
            RBtnNone.Checked = true;
            RBtnLineColor.Checked = true;
            //Line and dot Color
            lineColor = CogColorConstants.Blue;
            dotColor = CogColorConstants.Red;

            //Create new DataTable
            CreateDefectListTable();
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (OpenImgDialog.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }
            else
            {
                Bitmap bmp = new Bitmap(OpenImgDialog.FileName);
                DisplayImage(bmp);
            }
        }

        public void DisplayImage(Bitmap frameBuffer)
        {
            if (this.InvokeRequired)
            {
               
                this.BeginInvoke(new DisplayImageDelegate(DisplayImage), new object[] { frameBuffer });
                return;
            }

            try
            {
  
                CogImage8Grey cogDisplayImage = new CogImage8Grey(frameBuffer);

                cogDisplay1.StaticGraphics.Clear();
                cogDisplay1.InteractiveGraphics.Clear();
                cogDisplay1.BackColor = Color.Purple;
                cogDisplay1.Image = cogDisplayImage;
                cogDisplay1.Fit(true);
            }
            catch
            {
            }
        }

        private void CreateDefectListTable()
        {
            // Create a new DataTable.
            defectTable = new DataTable("DefectList");
            DataColumn dtColumn;
            
            // Create id column
            dtColumn = new DataColumn();
            dtColumn.DataType = typeof(Int32);
            dtColumn.ColumnName = "No";
            dtColumn.Caption = "Defect ID";
            dtColumn.ReadOnly = false;
            dtColumn.Unique = true;
            // Add column to the DataColumnCollection.
            defectTable.Columns.Add(dtColumn);

            // Create Name column.
            dtColumn = new DataColumn();
            dtColumn.DataType = typeof(String);
            dtColumn.ColumnName = "Name";
            dtColumn.Caption = "Defect Name";
            dtColumn.AutoIncrement = false;
            dtColumn.ReadOnly = false;
            dtColumn.Unique = false;
            /// Add column to the DataColumnCollection.
            defectTable.Columns.Add(dtColumn);

            // Create Address column.
            dtColumn = new DataColumn();
            dtColumn.DataType = typeof(String);
            dtColumn.ColumnName = "Detail";
            dtColumn.Caption = "Detail";
            dtColumn.ReadOnly = false;
            dtColumn.Unique = false;
            // Add column to the DataColumnCollection.
            defectTable.Columns.Add(dtColumn);

            dGVDefectList.DataSource = defectTable;
            dGVDefectList.Columns[2].Width = 200;
        }

        private void BtnRect_Click(object sender, EventArgs e)
        {
            CogRectangleAffine cogDefectBoundingRect = new CogRectangleAffine();
            cogDefectBoundingRect.SetCenterLengthsRotationSkew(800, 380, 600, 300, 0, 0);
            cogDefectBoundingRect.Interactive = true;
            cogDefectBoundingRect.GraphicDOFEnable = CogRectangleAffineDOFConstants.Position | CogRectangleAffineDOFConstants.Size;
            cogDefectBoundingRect.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            cogDefectBoundingRect.LineWidthInScreenPixels = lineWidth;
            cogDisplay1.InteractiveGraphics.Add(cogDefectBoundingRect, "DefectRect", false);
            shapeContainer.Add(cogDefectBoundingRect);
            //Populate defect table
            string detail = string.Format("({0}, {1})",
                cogDefectBoundingRect.CenterX,
                cogDefectBoundingRect.CenterY);
            defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);
        }
        
        private void BtnAnnotation_Click(object sender, EventArgs e)
        {
            //TODO: Deal with cancel button clicked
            string text = Microsoft.VisualBasic.Interaction.InputBox("Enter the text", "Title", "Text");

            CogGraphicLabel cogGraphicLabel = new CogGraphicLabel();
            cogGraphicLabel.SetXYText(100, 100, text);
            cogGraphicLabel.Interactive = true;
            cogGraphicLabel.GraphicDOFEnable = CogGraphicLabelDOFConstants.Position;
            cogGraphicLabel.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            cogGraphicLabel.LineWidthInScreenPixels = lineWidth;
            cogDisplay1.InteractiveGraphics.Add(cogGraphicLabel, "Annotation", false);
            shapeContainer.Add(cogGraphicLabel);
            //Populate defect table
            string detail = string.Format("({0}, {1})",
                cogGraphicLabel.X, 
                cogGraphicLabel.Y);
            defectTable.Rows.Add(defectTable.Rows.Count, text, detail);
        }

        private void BtnPoint_Click(object sender, EventArgs e)
        {
            CogPointMarker cogPointMarker = new CogPointMarker();
            cogPointMarker.SetCenterRotationSize(500, 500, 0, 5);
            cogPointMarker.GraphicDOFEnable = CogPointMarkerDOFConstants.Position;
            cogPointMarker.Interactive = true;
            cogPointMarker.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            cogPointMarker.LineWidthInScreenPixels = lineWidth;
            cogDisplay1.InteractiveGraphics.Add(cogPointMarker, "Point", false);
            shapeContainer.Add(cogPointMarker);
            //Populate defect table
            string detail = string.Format("({0}, {1})",
                cogPointMarker.X,
                cogPointMarker.Y);
            defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);
        }

        private void BtnLine_Click(object sender, EventArgs e)
        {
            CogLineSegment cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(200, 200, 500, 0);
            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.All;
            cogLineSegment.Interactive = true;
            cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            cogLineSegment.LineWidthInScreenPixels = lineWidth;
            cogDisplay1.InteractiveGraphics.Add(cogLineSegment, "Line", false);
            shapeContainer.Add(cogLineSegment);
            //Populate defect table
            string detail = string.Format("({0}, {1})",
                cogLineSegment.StartX,
                cogLineSegment.StartY);
            defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);
        }

        private void btnArrow_Click(object sender, EventArgs e)
        {
            ShapeDesigner arrow = new ShapeDesigner();
            arrow.SetArrowLength(500, 500, 100, 0.45);
            CogCompositeShape arrowComponents = arrow.GetShape();

            //Set the CogCompositeShape properties
            arrowComponents.Interactive = true;
            arrowComponents.GraphicDOFEnable = CogCompositeShapeDOFConstants.All;
            arrowComponents.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            arrowComponents.ID = 0;
            arrowComponents.ParentFromChildTransform = arrowComponents.GetParentFromChildTransform();

            shapeContainer.Add(arrowComponents);
            cogDisplay1.InteractiveGraphics.Add(arrowComponents, "Arrow", false);
            //Populate defect table
            //NOTICE: CogCompositeShape do not have x and y variable.
            //   The shapes x and y value does not update
            //   even after the CogCompositeShape position change. 
            //   Perhaps could ask the user for details, or use other way
            //   that can get the x and y position.
            string detail = string.Format("{0}", "Type in your details");
            defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);
        }

        private void btnDistance_Click(object sender, EventArgs e)
        {
            ShapeDesigner distance = new ShapeDesigner();
            distance.SetDistanceLength(300, 300, 500, 300);
            CogCompositeShape distanceComponents = distance.GetShape();

            //Set the CogCompositeShape properties
            distanceComponents.Interactive = true;
            distanceComponents.GraphicDOFEnable = CogCompositeShapeDOFConstants.All;
            distanceComponents.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            distanceComponents.ID = 2;
            distanceComponents.ParentFromChildTransform = distanceComponents.GetParentFromChildTransform();
            //distanceComponents.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
            
            shapeContainer.Add(distanceComponents);
            cogDisplay1.InteractiveGraphics.Add(distanceComponents, "Distance", false);
            //Populate defect table
            //NOTICE: CogCompositeShape do not have x and y variable.
            //   The shapes x and y value does not update
            //   even after the CogCompositeShape position change. 
            //   Perhaps could ask the user for details, or use other way
            //   that can get the x and y position.
            string detail = string.Format("{0}", "Type in your details");
            defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);
        }

        private void RBDefect_CheckedChanged(object sender, EventArgs e)
        {
            //Using tag to update defect category
            defectCategory = (sender as RadioButton).Tag.ToString();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            //TODO: Could use a dragging stopped method 
            string detail;
            int index = 0;
            CogInteractiveGraphicsContainer graphics = cogDisplay1.InteractiveGraphics;
            
            for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
            {
                for (int j = 0; j < shapeContainer.Count; j++)
                {
                    //Check if it's the same object
                    if (graphics[i].Equals(shapeContainer[j]))
                    {
                        index = j;
                        break;
                    }
                }              
                if (graphics[i].GetType() == typeof(CogRectangleAffine))
                {
                    detail = string.Format("({0}, {1})", 
                        ((CogRectangleAffine)graphics[i]).CenterX,
                        ((CogRectangleAffine)graphics[i]).CenterY);
                    //Update if detail does not match 
                    if (defectTable.Rows[index]["Detail"].ToString() != detail)
                    {
                        defectTable.Rows[index]["Detail"] = detail;
                    }
                }
                else if (graphics[i].GetType() == typeof(CogGraphicLabel))
                {
                    detail = string.Format("({0}, {1})",
                        ((CogGraphicLabel)graphics[i]).X,
                        ((CogGraphicLabel)graphics[i]).Y);
                    //Update if detail does not match 
                    if (defectTable.Rows[index]["Detail"].ToString() != detail)
                    {
                        defectTable.Rows[index]["Detail"] = detail;
                    }
                }
                else if (graphics[i].GetType() == typeof(CogPointMarker))
                {
                    detail = string.Format("({0}, {1})",
                        ((CogPointMarker)graphics[i]).X,
                        ((CogPointMarker)graphics[i]).Y);
                    //Update if detail does not match 
                    if (defectTable.Rows[index]["Detail"].ToString() != detail)
                    {
                        defectTable.Rows[index]["Detail"] = detail;
                    }
                }
                else if (graphics[i].GetType() == typeof(CogLineSegment))
                {
                    detail = string.Format("({0}, {1})",
                        ((CogLineSegment)graphics[i]).StartX,
                        ((CogLineSegment)graphics[i]).StartX);
                    //Update if detail does not match 
                    if (defectTable.Rows[index]["Detail"].ToString() != detail)
                    {
                        defectTable.Rows[index]["Detail"] = detail;
                    }
                }
                else if (graphics[i].GetType() == typeof(CogCompositeShape))
                {
                    //ID 0 = arrows
                    if (((CogCompositeShape)graphics[i]).ID == 0)
                    {
                        //TODO: Can't get the new arrow position.
                        detail = string.Format("({0}, {1})",
                            ((CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[0]).StartX,
                            ((CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[0]).StartY);
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    //ID 1 = drawings
                    else if (((CogCompositeShape)graphics[i]).ID == 1)
                    {
                        detail = string.Format("{0}", "Enter your details");
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    //ID 2 = distance
                    else if (((CogCompositeShape)graphics[i]).ID == 2)
                    {
                        detail = string.Format("{0}", "Enter your details");
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                }
            } 
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            //Convert InteractiveGraphics to CogCompositeShape
            CogCompositeShape container = new CogCompositeShape();
            //Allows each shape to store their properties (Like interactive, DOF...etc)
            container.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
            
            //Add each shape into CompositeShape
            for (int i = 0; i < shapeContainer.Count; i++)
            {
                container.Shapes.Add(shapeContainer[i]);
            }
          
            if (container.Shapes.Count > 0)
            {
                //Export vpp
                CogSerializer.SaveObjectToFile(container,
                    Path.Combine(_path, "Serialize.vpp"));

                //Export CSV
                using (var streamWriter = new StreamWriter(Path.Combine(_path, "DefectList.csv"), false))
                {
                    //headers
                    for (int i = 0; i < defectTable.Columns.Count; i++)
                    {
                        streamWriter.Write(defectTable.Columns[i]);
                        if (i < defectTable.Columns.Count - 1)
                        {
                            streamWriter.Write(",");
                        }
                    }
                    streamWriter.Write(streamWriter.NewLine);
                    foreach (DataRow dr in defectTable.Rows)
                    {
                        for (int i = 0; i < defectTable.Columns.Count; i++)
                        {
                            //If not DBNull
                            if (!Convert.IsDBNull(dr[i]))
                            {
                                string value = dr[i].ToString();
                                if (value.Contains(','))
                                {
                                    value = String.Format("\"{0}\"", value);
                                    streamWriter.Write(value);
                                }
                                else
                                {
                                    streamWriter.Write(value);
                                }
                            }
                            if (i < defectTable.Columns.Count - 1)
                            {
                                streamWriter.Write(",");
                            }
                        }
                        streamWriter.Write(streamWriter.NewLine);
                    }
                    streamWriter.Close();
                    streamWriter.Dispose();
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            //TODO: Have to load image again to show shapes, but why.
            Bitmap bmp = new Bitmap(Path.Combine(_path, "Screenshot_20230103_020226.png"));
            DisplayImage(bmp);
           
            //Clear the display, shapeContainer and datatable.
            cogDisplay1.InteractiveGraphics.Clear();
            CreateDefectListTable();
            shapeContainer.Clear();

            //Load vpp file
            string path = Path.Combine(_path, "Serialize.vpp");
            CogCompositeShape loadedCompositeShape = (CogCompositeShape)CogSerializer.LoadObjectFromFile(path);
            CogGraphicChildren shapes = loadedCompositeShape.Shapes;

            for (int i = 0; i < shapes.Count; i++)
            {
                if (shapes[i].GetType() == typeof(CogRectangleAffine))
                {
                    cogDisplay1.InteractiveGraphics.Add((CogRectangleAffine)shapes[i], "DefectRect", false);
                    shapeContainer.Add((CogRectangleAffine)shapes[i]);
                }
                else if (shapes[i].GetType() == typeof(CogGraphicLabel))
                {
                    cogDisplay1.InteractiveGraphics.Add((CogGraphicLabel)shapes[i], "Annotation", false);
                    shapeContainer.Add((CogGraphicLabel)shapes[i]);
                }
                else if (shapes[i].GetType() == typeof(CogPointMarker))
                {
                    cogDisplay1.InteractiveGraphics.Add((CogPointMarker)shapes[i], "Point", false);
                    shapeContainer.Add((CogPointMarker)shapes[i]);
                }
                else if (shapes[i].GetType() == typeof(CogLineSegment))
                {
                    cogDisplay1.InteractiveGraphics.Add((CogLineSegment)shapes[i], "Line", false);
                    shapeContainer.Add((CogLineSegment)shapes[i]);
                }
                else if (shapes[i].GetType() == typeof(CogCompositeShape))
                {
                    CogCompositeShape compositeShape = (CogCompositeShape)shapes[i];
                    
                    if (compositeShape.ID == 5)
                    {
                        for (int j = 0; j < compositeShape.Shapes.Count; j+=2)
                        {
                            compositeShape.Shapes[j].Changed += Dot_Changed;
                        }
                    }
                    else if (compositeShape.ID == 6)
                    {
                        for (int j = 0; j < compositeShape.Shapes.Count; j += 2)
                        {
                            compositeShape.Shapes[j].Changed += Dot_Changed;
                        }
                    }
                    else if (compositeShape.ID == 7)
                    {
                        compositeShape.Shapes[1].Changed += Circle_Changed;
                    }
                    else if (compositeShape.ID == 10)
                    {
                        compositeShape.Shapes[0].Changed += TwoCircleLength_Changed;
                        compositeShape.Shapes[2].Changed += TwoCircleLength_Changed;
                    }
                    //Add them after the changes are made
                    cogDisplay1.InteractiveGraphics.Add(compositeShape, "compositeShape", false);
                    shapeContainer.Add(compositeShape);
                }
            }

            //Load csv file
            var reader = new StreamReader(Path.Combine(_path, "DefectList.csv"));
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            using (var dr = new CsvDataReader(csv))
            {
                defectTable.Load(dr);
            }
        }

        private void cogDisplay1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //For drawings
                mouseDown = true;
                double mappedX;
                double mappedY;
                cogDisplay1.GetTransform("#", "*").MapPoint(e.X, e.Y,
                    out mappedX, out mappedY);

                previousX = mappedX;
                previousY = mappedY;

                //For Multi-Press tools
                if (!RBtnNone.Checked)
                {
                    if (0 < multiToolSteps)
                    {
                        multiToolPositions.Add(new Tuple<double, double>(mappedX, mappedY));
                    }
                }

                //###2Points Length###
                if (RBtn2Points.Checked)
                {
                    if (2 == multiToolSteps)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (1 == multiToolSteps)
                    {
                        CogLineSegment line = new CogLineSegment();
                        line.SetStartEnd(multiToolPositions[0].Item1, 
                            multiToolPositions[0].Item2, mappedX, mappedY);
                        line.GraphicDOFEnable = CogLineSegmentDOFConstants.All;
                        line.Interactive = true;
                        line.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        line.LineWidthInScreenPixels = lineWidth;
                        cogDisplay1.InteractiveGraphics.Add(line, "Line", false);
                        shapeContainer.Add(line);
                        //Populate defect table
                        string detail = string.Format("Length: {0}", line.Length);
                        defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);

                        RBtnNone.Checked = true;
                    }                   
                    multiToolSteps -= 1;
                }
                //###Multi Points Length###
                else if (RBtnMultiPointsLength.Checked)
                {
                    if (multiToolSteps == 9999)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (multiToolSteps < 9999)
                    {
                        int index = multiToolPositions.Count - 2;
                        AddTemporaryLine(multiToolPositions[index].Item1,
                            multiToolPositions[index].Item2, mappedX, mappedY);
                    }
                    multiToolSteps -= 1;
                }
                //###Triangle Angle###
                else if (RBtnTrigAngle.Checked)
                {
                    if (3 == multiToolSteps)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (2 == multiToolSteps)
                    {
                        int index = multiToolPositions.Count - 2;
                        AddTemporaryLine(multiToolPositions[index].Item1,
                            multiToolPositions[index].Item2, mappedX, mappedY);
                    }
                    else if (1 == multiToolSteps)
                    {
                        CogCompositeShape compositeShape = new CogCompositeShape();
                        compositeShape.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
                        compositeShape.Interactive = true;
                        compositeShape.GraphicDOFEnable = CogCompositeShapeDOFConstants.Position;
                        compositeShape.ID = 6;   //Number to identify the type

                        for (int i = 0; i < multiToolPositions.Count; i++)
                        {
                            //Dot
                            CogLineSegment dot = new CogLineSegment();
                            dot.SelectedSpaceName = "$";
                            dot.Interactive = true;
                            dot.GraphicDOFEnable = CogLineSegmentDOFConstants.Position;
                            dot.LineWidthInScreenPixels = lineWidth + 3;
                            dot.DragLineWidthInScreenPixels = lineWidth + 3;
                            dot.SelectedLineWidthInScreenPixels = lineWidth + 3;
                            dot.Color = CogColorConstants.Red;
                            dot.Changed += Dot_Changed;
                            dot.SetStartLengthRotation(multiToolPositions[i].Item1,
                                multiToolPositions[i].Item2, 0.0001, 0);
                            compositeShape.Shapes.Add(dot);

                            if (i < multiToolPositions.Count - 1)
                            {
                                //Line
                                CogLineSegment cogLineSegment = new CogLineSegment();
                                cogLineSegment.SelectedSpaceName = "$";
                                cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                                cogLineSegment.Interactive = false;
                                cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
                                cogLineSegment.LineWidthInScreenPixels = lineWidth;
                                cogLineSegment.SetStartEnd(multiToolPositions[i].Item1,
                                    multiToolPositions[i].Item2, multiToolPositions[i + 1].Item1,
                                    multiToolPositions[i + 1].Item2);
                                compositeShape.Shapes.Add(cogLineSegment);
                            }
                        }

                        //Compute the angle
                        double angle = 0;
                        CogLineSegment line1 = (CogLineSegment)compositeShape.Shapes[1];
                        CogLineSegment line2 = (CogLineSegment)compositeShape.Shapes[3];
                        double s1 = (line1.StartY - line1.EndY) / (line1.EndX - line1.StartX);
                        double s2 = (line2.StartY - line2.EndY) / (line2.EndX - line2.StartX);
                        double c1 = line1.StartY - (s1 * line1.StartX);
                        double c2 = line2.StartY - (s2 * line2.StartX);
                        angle = Math.Abs( (1 + (s1 * s2)) / 
                            (Math.Sqrt(1 + Math.Pow(s1, 2)) * Math.Sqrt(1 + Math.Pow(s2, 2))) );
                        angle = Math.Acos(angle) * 180 / Math.PI;

                        shapeContainer.Add(compositeShape);
                        cogDisplay1.InteractiveGraphics.Add(compositeShape, "Angle 1", false);
                        // Populate defect table
                        string detail = string.Format("Angle: {0}", angle);
                        defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);

                        RBtnNone.Checked = true;
                    }
                    //Decrement for Angle steps
                    multiToolSteps -= 1;
                }
                //###Single Circle Radius###
                else if (RBtnSingleRadius.Checked)
                {
                    if (1 < multiToolSteps)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (1 == multiToolSteps)
                    {
                        //Calculate the radius
                        double diffX = multiToolPositions[0].Item1 - mappedX;
                        double diffY = multiToolPositions[0].Item2 - mappedY;
                        double radius = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));

                        CogCompositeShape compositeShape = new CogCompositeShape();
                        compositeShape.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
                        compositeShape.Interactive = true;
                        compositeShape.GraphicDOFEnable = CogCompositeShapeDOFConstants.Position;
                        compositeShape.ID = 7;   //Number to indentify the type

                        //Dot in the middle of the circle
                        CogPointMarker point = new CogPointMarker();
                        point.SelectedSpaceName = "$";
                        point.Color = CogColorConstants.Red;
                        point.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        point.SetCenterRotationSize(multiToolPositions[0].Item1,
                            multiToolPositions[0].Item2, 0, lineWidth + 3);
                        //Circle
                        CogCircle circle = new CogCircle();
                        circle.SelectedSpaceName = "$";
                        circle.LineWidthInScreenPixels = lineWidth;
                        circle.SetCenterRadius(multiToolPositions[0].Item1, multiToolPositions[0].Item2, radius);
                        circle.GraphicDOFEnable = CogCircleDOFConstants.All;
                        circle.Interactive = true;
                        circle.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        circle.Changed += Circle_Changed;
                        //Add into composite shape
                        compositeShape.Shapes.Add(point);
                        compositeShape.Shapes.Add(circle);

                        cogDisplay1.InteractiveGraphics.Add(compositeShape, "Circle", false);
                        shapeContainer.Add(compositeShape);
                        //Populate defect table
                        string detail = string.Format("Radius: {0}", circle.Radius);
                        defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);

                        RBtnNone.Checked = true;
                    }
                    //Decrement 
                    multiToolSteps -= 1;
                }
                //###2Line Angle###
                else if (RBtn2LineAngle.Checked)
                {
                    if (4 == multiToolSteps)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (3 == multiToolSteps)
                    {
                        AddTemporaryLine(multiToolPositions[0].Item1, 
                            multiToolPositions[0].Item2, mappedX, mappedY);
                    }
                    else if (2 == multiToolSteps)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (1 == multiToolSteps)
                    {
                        CogCompositeShape compositeShape = new CogCompositeShape();
                        compositeShape.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
                        compositeShape.Interactive = true;
                        compositeShape.GraphicDOFEnable = CogCompositeShapeDOFConstants.Position;
                        compositeShape.ID = 8;   //Number to identify the type

                        //Line
                        for (int i = 0; i < 4; i += 2)
                        {
                            CogLineSegment cogLineSegment = new CogLineSegment();
                            cogLineSegment.SelectedSpaceName = "$";
                            cogLineSegment.Interactive = true;
                            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.BothPoints;
                            cogLineSegment.LineWidthInScreenPixels = lineWidth;
                            cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                            cogLineSegment.SetStartEnd(multiToolPositions[i].Item1,
                                multiToolPositions[i].Item2, multiToolPositions[i + 1].Item1,
                                multiToolPositions[i + 1].Item2);
                            compositeShape.Shapes.Add(cogLineSegment);
                        }

                        //Compute the angle
                        double angle = 0;
                        CogLineSegment line1 = (CogLineSegment)compositeShape.Shapes[0];
                        CogLineSegment line2 = (CogLineSegment)compositeShape.Shapes[1];
                        double s1 = (line1.StartY - line1.EndY) / (line1.EndX - line1.StartX);
                        double s2 = (line2.StartY - line2.EndY) / (line2.EndX - line2.StartX);
                        double c1 = line1.StartY - (s1 * line1.StartX);
                        double c2 = line2.StartY - (s2 * line2.StartX);
                        angle = Math.Abs((1 + (s1 * s2)) /
                            (Math.Sqrt(1 + Math.Pow(s1, 2)) * Math.Sqrt(1 + Math.Pow(s2, 2))));
                        angle = Math.Acos(angle) * 180 / Math.PI;

                        shapeContainer.Add(compositeShape);
                        cogDisplay1.InteractiveGraphics.Add(compositeShape, "Angle 1", false);
                        // Populate defect table
                        string detail = string.Format("Angle: {0}", angle);
                        defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);

                        RBtnNone.Checked = true;
                    }
                    multiToolSteps -= 1;
                }
                //###Perpendicular Length###
                else if (RBtnPerpLength.Checked)
                {
                    if (3 == multiToolSteps)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (2 == multiToolSteps)
                    {
                        AddTemporaryLine(multiToolPositions[0].Item1,
                            multiToolPositions[0].Item2, mappedX, mappedY);
                    }
                    else if (1 == multiToolSteps)
                    {
                        CogCompositeShape compositeShape = new CogCompositeShape();
                        compositeShape.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
                        compositeShape.Interactive = true;
                        compositeShape.GraphicDOFEnable = CogCompositeShapeDOFConstants.Position | CogCompositeShapeDOFConstants.Rotation;
                        compositeShape.ID = 9;   //Number to indentify the type

                        //Line
                        CogLineSegment cogLineSegment = new CogLineSegment();
                        cogLineSegment.SelectedSpaceName = "$";
                        cogLineSegment.Interactive = true;
                        cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
                        cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        cogLineSegment.LineWidthInScreenPixels = lineWidth;
                        //cogLineSegment.Changed += PerpLengthLine_Changed;
                        cogLineSegment.SetStartEnd(multiToolPositions[0].Item1,
                            multiToolPositions[0].Item2, multiToolPositions[1].Item1,
                            multiToolPositions[1].Item2);
                        compositeShape.Shapes.Add(cogLineSegment);

                        //Find the perpendicular gradient of the line above
                        CogLineSegment line1 = (CogLineSegment)compositeShape.Shapes[0];
                        double m1 = (line1.EndY - line1.StartY) / (line1.EndX - line1.StartX);
                        double m2 = -1 / m1;
                        double c1 = line1.StartY - (m1 * line1.StartX);
                        double c2 = mappedY - (m2 * mappedX);
                        //Get the intersect of 2 lines
                        double intersectX = (c2 - c1) / (m1 - m2);
                        double intersectY = (m1 * intersectX) + c1;

                        //Perpendicular Line that stops on the intersection 
                        CogLineSegment perpLine = new CogLineSegment();
                        perpLine.SelectedSpaceName = "$";
                        perpLine.Interactive = true;
                        perpLine.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
                        perpLine.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        perpLine.LineWidthInScreenPixels = lineWidth;
                        perpLine.Color = CogColorConstants.Red;
                        perpLine.SetStartEnd(mappedX, mappedY, intersectX, intersectY);
                        compositeShape.Shapes.Add(perpLine);

                        shapeContainer.Add(compositeShape);
                        cogDisplay1.InteractiveGraphics.Add(compositeShape, "Angle 1", false);
                        // Populate defect table
                        string detail = string.Format("Length: {0}", perpLine.Length);
                        defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);

                        RBtnNone.Checked = true;
                    }
                    multiToolSteps -= 1;
                }
                //###2 Circle Length###
                else if (RBtn2CircleLength.Checked)
                {
                    if (4 == multiToolSteps)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (3 == multiToolSteps)
                    {
                        //Calculate the radius
                        double diffX = multiToolPositions[0].Item1 - mappedX;
                        double diffY = multiToolPositions[0].Item2 - mappedY;
                        double radius = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));
                        AddTemporaryCircle(multiToolPositions[0].Item1, multiToolPositions[0].Item2, radius);
                    }
                    else if (2 == multiToolSteps)
                    {
                        AddTemporaryPoint(mappedX, mappedY);
                    }
                    else if (1 == multiToolSteps)
                    {
                        CogCompositeShape compositeShape = new CogCompositeShape();
                        compositeShape.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
                        compositeShape.Interactive = true;
                        compositeShape.GraphicDOFEnable = CogCompositeShapeDOFConstants.Position;
                        compositeShape.ID = 10;   //Number to identify the type

                        //Dot1
                        CogLineSegment dot1 = new CogLineSegment();
                        dot1.SelectedSpaceName = "$";
                        dot1.Interactive = true;
                        dot1.GraphicDOFEnable = CogLineSegmentDOFConstants.Position;
                        dot1.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        dot1.LineWidthInScreenPixels = lineWidth + 3;
                        dot1.DragLineWidthInScreenPixels = lineWidth + 3;
                        dot1.SelectedLineWidthInScreenPixels = lineWidth + 3;
                        dot1.Color = CogColorConstants.Red;
                        dot1.Changed += TwoCircleLength_Changed;
                        dot1.SetStartLengthRotation(multiToolPositions[0].Item1,
                        multiToolPositions[0].Item2, 0.0001, 0);
                        compositeShape.Shapes.Add(dot1);
                        //Circle1
                        //Calculate the radius
                        double diffX = multiToolPositions[0].Item1 - multiToolPositions[1].Item1;
                        double diffY = multiToolPositions[0].Item2 - multiToolPositions[1].Item2;
                        double radius = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));
                        CogCircle circle1 = new CogCircle();
                        circle1.SelectedSpaceName = "$";
                        circle1.SetCenterRadius(multiToolPositions[0].Item1, multiToolPositions[0].Item2, radius);
                        circle1.GraphicDOFEnable = CogCircleDOFConstants.Radius;
                        circle1.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        circle1.LineWidthInScreenPixels = lineWidth;
                        circle1.Interactive = true;
                        compositeShape.Shapes.Add(circle1);
                        //Dot2
                        CogLineSegment dot2 = new CogLineSegment();
                        dot2.SelectedSpaceName = "$";
                        dot2.Interactive = true;
                        dot2.GraphicDOFEnable = CogLineSegmentDOFConstants.Position;
                        dot2.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        dot2.LineWidthInScreenPixels = lineWidth + 3;
                        dot2.DragLineWidthInScreenPixels = lineWidth + 3;
                        dot2.SelectedLineWidthInScreenPixels = lineWidth + 3;
                        dot2.Color = CogColorConstants.Red;
                        dot2.Changed += TwoCircleLength_Changed;
                        dot2.SetStartLengthRotation(multiToolPositions[2].Item1,
                        multiToolPositions[2].Item2, 0.0001, 0);
                        compositeShape.Shapes.Add(dot2);
                        //Circle2
                        //Calculate the radius
                        diffX = multiToolPositions[2].Item1 - multiToolPositions[3].Item1;
                        diffY = multiToolPositions[2].Item2 - multiToolPositions[3].Item2;
                        radius = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));
                        CogCircle circle2 = new CogCircle();
                        circle2.SelectedSpaceName = "$";
                        circle2.SetCenterRadius(multiToolPositions[2].Item1, multiToolPositions[2].Item2, radius);
                        circle2.GraphicDOFEnable = CogCircleDOFConstants.Radius;
                        circle2.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        circle2.LineWidthInScreenPixels = lineWidth;
                        circle2.Interactive = true;
                        compositeShape.Shapes.Add(circle2);
                        //Line
                        CogLineSegment line = new CogLineSegment();
                        line.SelectedSpaceName = "$";
                        line.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
                        line.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        line.LineWidthInScreenPixels = lineWidth;
                        line.SetStartEnd(multiToolPositions[0].Item1, multiToolPositions[0].Item2,
                            multiToolPositions[2].Item1, multiToolPositions[2].Item2);
                        compositeShape.Shapes.Add(line);

                        shapeContainer.Add(compositeShape);
                        cogDisplay1.InteractiveGraphics.Add(compositeShape, "2 Circle Length", false);
                        // Populate defect table
                        string detail = string.Format("Length: {0}", line.Length);
                        defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);

                        RBtnNone.Checked = true;
                    }
                    multiToolSteps -= 1;
                }
            }
        }

        private void AddTemporaryLine(double startX, double startY, double endX, double endY)
        {
            //For temporary display
            CogLineSegment line = new CogLineSegment();
            line.LineWidthInScreenPixels = lineWidth;
            //line.Color = CogColorConstants.
            line.SetStartEnd(startX, startY, endX, endY);
            cogDisplay1.InteractiveGraphics.Add(line, "temp", false);
        }

        private void AddTemporaryPoint(double x, double y)
        {
            //For temporary display
            CogLineSegment line = new CogLineSegment();
            line.SetStartLengthRotation(x, y, 0.0001, 0);
            line.LineWidthInScreenPixels = lineWidth + 3;
            cogDisplay1.InteractiveGraphics.Add(line, "temp", false);
        }

        private void AddTemporaryCircle(double centerX, double centerY, double radius)
        {
            //For temporary display
            CogCircle circle = new CogCircle();
            circle.SetCenterRadius(centerX, centerY, radius);
            circle.LineWidthInScreenPixels = lineWidth;
            cogDisplay1.InteractiveGraphics.Add(circle, "temp", false);
        }

        private void cogDisplay1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawingMode)
            {
                if (mouseDown)
                {
                    //Update the position
                    //Get the position
                    double mappedX;
                    double mappedY;
                    cogDisplay1.GetTransform("#", "*").MapPoint(e.X, e.Y,
                        out mappedX, out mappedY);
                    
                    //For temporary display
                    CogLineSegment shape = new CogLineSegment();
                    shape.SetStartEnd(previousX, previousY, mappedX, mappedY);
                    shape.LineWidthInScreenPixels = lineWidth;
                    //To add into CogCompositeShape
                    CogLineSegment shape2 = new CogLineSegment();
                    shape2.SetStartEnd(previousX, previousY, mappedX, mappedY);
                    shape2.SelectedSpaceName = "$";

                    cogDisplay1.InteractiveGraphics.Add(shape, "temp", false);
                    drawing.Shapes.Add(shape2);

                    previousX = mappedX;
                    previousY = mappedY;
                }
            }
        }

        private void cogDisplay1_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void btnDraw_Click(object sender, EventArgs e)
        {
            if (drawingMode)
            {
                drawingMode = false;
                btnDraw.BackColor = Color.Green;
                //Other buttons
                BtnRect.Enabled = true;
                BtnAnnotation.Enabled = true;
                BtnPoint.Enabled = true;
                BtnLine.Enabled = true;
                btnArrow.Enabled = true;

                //Make sure it is drawn
                if (0 < drawing.Shapes.Count)
                {
                    shapeContainer.Add(drawing);
                    cogDisplay1.InteractiveGraphics.Add(drawing, "Drawing", false);
                    //Remove temporary drawings
                    cogDisplay1.InteractiveGraphics.Remove("temp");
                    // Populate defect table
                    string detail = string.Format("{0}", "Enter your details");
                    defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);
                }
            }
            else
            {
                drawingMode = true;
                btnDraw.BackColor = Color.Red;
                //Other buttons
                BtnRect.Enabled = false;
                BtnAnnotation.Enabled = false;
                BtnPoint.Enabled = false;
                BtnLine.Enabled = false;
                btnArrow.Enabled = false;

                //Create new CogCompositeShape for drawing
                drawing = new CogCompositeShape();
                drawing.Interactive = true;
                drawing.GraphicDOFEnable = CogCompositeShapeDOFConstants.All;
                drawing.LineWidthInScreenPixels = lineWidth;
                drawing.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                drawing.ID = 1;   //Signifies a drawing
                drawing.ParentFromChildTransform = drawing.GetParentFromChildTransform();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            cogDisplay1.InteractiveGraphics.Clear();
            CreateDefectListTable();
            shapeContainer.Clear();
        }

        private void MultiToolRBtn_CheckedChanged(object sender, EventArgs e)
        {
            // Remove any temporary drawings after the mode successfully
            //finishes or changes.
            if (0 < multiToolPositions.Count)
            {
                //Remove temporary drawings
                cogDisplay1.InteractiveGraphics.Remove("temp");
            }
            
            multiToolSteps = int.Parse((sender as RadioButton).Tag.ToString());
            multiToolPositions = new List<Tuple<double, double>>();

        }

        private void RBtnMultiPointsLength_Click(object sender, EventArgs e)
        {
            //If previous mode is Multi-Points length 
            if (RBtnMultiPointsLength.Checked && 1 < multiToolPositions.Count)
            {
                double totalLength = 0;
                CogCompositeShape compositeShape = new CogCompositeShape();
                compositeShape.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
                compositeShape.Interactive = true;
                compositeShape.GraphicDOFEnable = CogCompositeShapeDOFConstants.Position;
                compositeShape.ID = 5;   //Number to identify the type

                //Create and add each lines into the composite shape, and connections(dots)
                for (int i = 0; i < multiToolPositions.Count; i++)
                {
                    //Dot
                    CogLineSegment dot = new CogLineSegment();
                    dot.SelectedSpaceName = "$";
                    dot.Interactive = true;
                    dot.GraphicDOFEnable = CogLineSegmentDOFConstants.Position;
                    dot.LineWidthInScreenPixels = lineWidth + 3;
                    dot.DragLineWidthInScreenPixels = lineWidth + 3;
                    dot.SelectedLineWidthInScreenPixels = lineWidth + 3;
                    dot.Color = CogColorConstants.Red;
                    dot.Changed += Dot_Changed;
                    dot.SetStartLengthRotation(multiToolPositions[i].Item1,
                        multiToolPositions[i].Item2, 0.5, 0);
                    compositeShape.Shapes.Add(dot);

                    if (i < multiToolPositions.Count - 1)
                    {
                        //Line
                        CogLineSegment cogLineSegment = new CogLineSegment();
                        cogLineSegment.SelectedSpaceName = "$";
                        cogLineSegment.Interactive = false;
                        cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
                        cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        cogLineSegment.LineWidthInScreenPixels = lineWidth;
                        cogLineSegment.SetStartEnd(multiToolPositions[i].Item1,
                            multiToolPositions[i].Item2, multiToolPositions[i + 1].Item1,
                            multiToolPositions[i + 1].Item2);
                        compositeShape.Shapes.Add(cogLineSegment);
                        //Update the total length
                        totalLength += cogLineSegment.Length;
                    }
                }

                shapeContainer.Add(compositeShape);
                cogDisplay1.InteractiveGraphics.Add(compositeShape, "Multi-Point length", false);
                // Populate defect table
                string detail = string.Format("Total Length: {0}", totalLength);
                defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);

                RBtnNone.Checked = true;
            }
        }

        private void Dot_Changed(object sender, CogChangedEventArgs e)
        {
            if (!changing)
            {
                //To prevent lines to be invoked while
                //the position is getting changed.
                changing = true;
                
                CogCompositeShape compositeShape = new CogCompositeShape();
                int index = -1;
                CogCompositeShape temp;
                for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
                {
                    if (cogDisplay1.InteractiveGraphics[i].GetType() == typeof(CogCompositeShape))
                    {
                        temp = (CogCompositeShape)cogDisplay1.InteractiveGraphics[i];
                        for (int j = 0; j < temp.Shapes.Count; j++)
                        {
                            if (sender.Equals(temp.Shapes[j]))
                            {
                                compositeShape = (CogCompositeShape)cogDisplay1.InteractiveGraphics[i];
                                index = j;
                            }
                        }

                    }
                }

                //Update the line position
                if (1 < compositeShape.Shapes.Count)
                {
                    if (0 == index)
                    {
                        //Update the first line
                        ((CogLineSegment)compositeShape.Shapes[1]).StartX = ((CogLineSegment)compositeShape.Shapes[index]).MidpointX;
                        ((CogLineSegment)compositeShape.Shapes[1]).StartY = ((CogLineSegment)compositeShape.Shapes[index]).MidpointY;
                    }
                    else if (0 < index && index < compositeShape.Shapes.Count - 1)
                    {
                        //Update the previous line
                        ((CogLineSegment)compositeShape.Shapes[index - 1]).EndX = ((CogLineSegment)compositeShape.Shapes[index]).MidpointX;
                        ((CogLineSegment)compositeShape.Shapes[index - 1]).EndY = ((CogLineSegment)compositeShape.Shapes[index]).MidpointY;
                        //Update the next line
                        ((CogLineSegment)compositeShape.Shapes[index + 1]).StartX = ((CogLineSegment)compositeShape.Shapes[index]).MidpointX;
                        ((CogLineSegment)compositeShape.Shapes[index + 1]).StartY = ((CogLineSegment)compositeShape.Shapes[index]).MidpointY;
                    }
                    else
                    {
                        //Update the second last line
                        ((CogLineSegment)compositeShape.Shapes[index - 1]).EndX = ((CogLineSegment)compositeShape.Shapes[index]).MidpointX;
                        ((CogLineSegment)compositeShape.Shapes[index - 1]).EndY = ((CogLineSegment)compositeShape.Shapes[index]).MidpointY;
                    }
                }

                changing = false;
            }
        }

        private void Circle_Changed(object sender, CogChangedEventArgs e)
        {
            if (!changing)
            {
                //Might not be neede for circle changed. (But required for multi line changed)
                changing = true;

                CogCompositeShape compositeShape = new CogCompositeShape();
                int index = -1;
                CogCompositeShape temp;
                for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
                {
                    if (cogDisplay1.InteractiveGraphics[i].GetType() == typeof(CogCompositeShape))
                    {
                        temp = (CogCompositeShape)cogDisplay1.InteractiveGraphics[i];
                        for (int j = 0; j < temp.Shapes.Count; j++)
                        {
                            if (sender.Equals(temp.Shapes[j]))
                            {
                                compositeShape = (CogCompositeShape)cogDisplay1.InteractiveGraphics[i];
                            }
                        }

                    }
                }

                //Update the dot position after the circle has changed
                if (1 < compositeShape.Shapes.Count)
                {
                    ((CogPointMarker)compositeShape.Shapes[0]).X = ((CogCircle)compositeShape.Shapes[1]).CenterX;
                    ((CogPointMarker)compositeShape.Shapes[0]).Y = ((CogCircle)compositeShape.Shapes[1]).CenterY;
                }

                changing = false;
            }
        }

        private void TwoCircleLength_Changed(object sender, CogChangedEventArgs e)
        {
            CogCompositeShape compositeShape = new CogCompositeShape();
            int index = -1;
            CogCompositeShape temp;
            for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
            {
                if (cogDisplay1.InteractiveGraphics[i].GetType() == typeof(CogCompositeShape))
                {
                    temp = (CogCompositeShape)cogDisplay1.InteractiveGraphics[i];
                    for (int j = 0; j < temp.Shapes.Count; j++)
                    {
                        if (sender.Equals(temp.Shapes[j]))
                        {
                            compositeShape = (CogCompositeShape)cogDisplay1.InteractiveGraphics[i];
                            index = j;
                        }
                    }

                }
            }

            //Update the line position and circle position
            if (1 < compositeShape.Shapes.Count)
            {
                if (0 == index)
                {
                    //Update first circle and line's start xy
                    double x = ((CogLineSegment)compositeShape.Shapes[0]).StartX;
                    double y = ((CogLineSegment)compositeShape.Shapes[0]).StartY;
                    ((CogCircle)compositeShape.Shapes[1]).CenterX = x;
                    ((CogCircle)compositeShape.Shapes[1]).CenterY = y;
                    ((CogLineSegment)compositeShape.Shapes[4]).StartX = x;
                    ((CogLineSegment)compositeShape.Shapes[4]).StartY = y;
                }  
                else if (2 == index)
                {
                    //Update second circle and line's end xy
                    double x = ((CogLineSegment)compositeShape.Shapes[2]).StartX;
                    double y = ((CogLineSegment)compositeShape.Shapes[2]).StartY;
                    ((CogCircle)compositeShape.Shapes[3]).CenterX = x;
                    ((CogCircle)compositeShape.Shapes[3]).CenterY = y;
                    ((CogLineSegment)compositeShape.Shapes[4]).EndX = x;
                    ((CogLineSegment)compositeShape.Shapes[4]).EndY = y;
                }
            }
        }

        private void PerpLengthLine_Changed(object sender, CogChangedEventArgs e)
        {
            if (!changing)
            {
                //To prevent lines to be invoked while
                //the position is getting changed.
                changing = true;

                CogCompositeShape compositeShape = new CogCompositeShape();
                int index = -1;
                CogCompositeShape temp;
                for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
                {
                    if (cogDisplay1.InteractiveGraphics[i].GetType() == typeof(CogCompositeShape))
                    {
                        temp = (CogCompositeShape)cogDisplay1.InteractiveGraphics[i];
                        for (int j = 0; j < temp.Shapes.Count; j++)
                        {
                            if (sender.Equals(temp.Shapes[j]))
                            {
                                compositeShape = (CogCompositeShape)cogDisplay1.InteractiveGraphics[i];
                                index = j;
                            }
                        }

                    }
                }

                //Update the line position
                if (1 < compositeShape.Shapes.Count)
                {
                    if (0 == index)
                    {
                        //Update the perpendicular line

                        // Get the distance from start point of the normal line
                        //to the intersection of the perpendicular line (Before it
                        //was changed). 
                        CogLineSegment line1 = (CogLineSegment)compositeShape.Shapes[0];
                        CogLineSegment perpLine = (CogLineSegment)compositeShape.Shapes[1];
                        double distance = Math.Sqrt(Math.Pow(perpLine.EndX - line1.StartX, 2) 
                            + Math.Pow(perpLine.EndY - line1.StartY, 2));
                        double m1 = (line1.EndY - line1.StartY) / (line1.EndX - line1.StartX);
                        double m2 = -1 / m1;


                        /*//Find the perpendicular gradient of the line above
                        double c1 = line1.StartY - (m1 * line1.StartX);
                        double c2 = mappedY - (m2 * mappedX);
                        //Get the intersect of 2 lines
                        double intersectX = (c2 - c1) / (m1 - m2);
                        double intersectY = (m1 * intersectX) + c1;*/
                    }
                    else if (1 == index)
                    {
                        //Update the original line
                    }
                    
                }

                changing = false;
            }
        }

        private void UpDownLineWidth_ValueChanged(object sender, EventArgs e)
        {
            lineWidth = (int)(sender as NumericUpDown).Value;
        }

        private void btnTrash_Click(object sender, EventArgs e)
        {
            int displayIndex = -1;
            int index = -1;
            for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
            {
                //If the current graphic are selected
                if (cogDisplay1.InteractiveGraphics[i].Selected)
                {
                    for (int j = 0; j < shapeContainer.Count; j++)
                    {
                        if (cogDisplay1.InteractiveGraphics[i].Equals(shapeContainer[j]))
                        {
                            displayIndex = i;
                            index = j;
                        }
                    }
                }
            }

            //Remove the graphic from the display
            cogDisplay1.InteractiveGraphics.Remove(displayIndex);
            //Remove the graphic from the container
            shapeContainer.RemoveAt(index);
            //Remove the graphic from the defect table
            defectTable.Rows.RemoveAt(index);

            //Update the id
            for (int i = index; i < defectTable.Rows.Count; i++)
            {
                //Update the defect table row id
                defectTable.Rows[i]["No"] = i;
                string tipText = string.Format("Defect No: {0}", i);
                //Update the interactive graphics tiptext
                for (int j = 0; j < cogDisplay1.InteractiveGraphics.Count; j++)
                {
                    if (cogDisplay1.InteractiveGraphics[j].Equals(shapeContainer[i]))
                    {
                        //If it is CogCompositeShape
                        if (cogDisplay1.InteractiveGraphics[j].GetType() == typeof(CogCompositeShape))
                        {
                            //Update the tooltip for CogCompositeShape for composite mode of uniform
                            cogDisplay1.InteractiveGraphics[j].TipText = tipText;

                            CogCompositeShape shapeComponent = (CogCompositeShape)cogDisplay1.InteractiveGraphics[j];
                            for (int k = 0; k < shapeComponent.Shapes.Count; k++)
                            {
                                if (shapeComponent.Shapes[k].GetType() == typeof(CogLineSegment))
                                {
                                    ((CogLineSegment)shapeComponent.Shapes[k]).TipText = tipText;
                                }
                                else if (shapeComponent.Shapes[k].GetType() == typeof(CogCircle))
                                {
                                    ((CogCircle)shapeComponent.Shapes[k]).TipText = tipText;
                                }
                                else if (shapeComponent.Shapes[k].GetType() == typeof(CogPointMarker))
                                {
                                    ((CogPointMarker)shapeComponent.Shapes[k]).TipText = tipText;
                                }
                            }
                        }
                        else if (cogDisplay1.InteractiveGraphics[j].GetType() == typeof(CogRectangleAffine))
                        {
                            ((CogRectangleAffine)cogDisplay1.InteractiveGraphics[j]).TipText = tipText;
                        }
                        else if (cogDisplay1.InteractiveGraphics[j].GetType() == typeof(CogGraphicLabel))
                        {
                            ((CogGraphicLabel)cogDisplay1.InteractiveGraphics[j]).TipText = tipText;
                        }
                        else if (cogDisplay1.InteractiveGraphics[j].GetType() == typeof(CogPointMarker))
                        {
                            ((CogPointMarker)cogDisplay1.InteractiveGraphics[j]).TipText = tipText;
                        }
                        else if (cogDisplay1.InteractiveGraphics[j].GetType() == typeof(CogLineSegment))
                        {
                            ((CogLineSegment)cogDisplay1.InteractiveGraphics[j]).TipText = tipText;
                        }
                    }
                }
            }
        }

        private void PBColor_Click(object sender, EventArgs e)
        {
            PictureBox p = (PictureBox)sender;
            CogColorConstants color = CogColorConstants.Red;

            if (p.BackColor == Color.Red) { color = CogColorConstants.Red; }
            else if (p.BackColor == Color.Yellow) { color = CogColorConstants.Yellow; }
            else if (p.BackColor == Color.Magenta) { color = CogColorConstants.Magenta; }
            else if (p.BackColor == Color.Blue) { color = CogColorConstants.Blue; }
            else if (p.BackColor == Color.White) { color = CogColorConstants.White; }
            else if (p.BackColor == Color.Black) { color = CogColorConstants.Black; }
            else if (p.BackColor == Color.Cyan) { color = CogColorConstants.Cyan; }
            else if (p.BackColor == Color.Green) { color = CogColorConstants.Green; }
            else if (p.BackColor == Color.Orange) { color = CogColorConstants.Orange; }

            if (RBtnLineColor.Checked) { lineColor = color; RBtnLineColor.BackColor = p.BackColor; }
            else if (RBtnDotColor.Checked) { dotColor = color; RBtnDotColor.BackColor = p.BackColor; }
        }
    }
}
