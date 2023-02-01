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
            if (shapeContainer[0].GetType() == typeof(CogCompositeShape))
            {
                if (((CogCompositeShape)shapeContainer[0]).CompositionMode == CogCompositeShapeCompositionModeConstants.Uniform)
                {
                    ((CogCompositeShape)shapeContainer[0]).CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
                    CogCompositeShape shape = (CogCompositeShape)shapeContainer[0];
                    Console.WriteLine(((CogLineSegment)shape.Shapes[0]).Length);
                }
                else
                {
                    ((CogCompositeShape)shapeContainer[0]).CompositionMode = CogCompositeShapeCompositionModeConstants.Uniform;
                    CogCompositeShape shape = (CogCompositeShape)shapeContainer[0];
                    Console.WriteLine(((CogLineSegment)shape.Shapes[0]).Length);
                }
                
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
            UpDownLineWidth.Value = 1;

            //Drawing mode
            drawingMode = false;
            mouseDown = false;
            btnDraw.BackColor = Color.Green;

            //Multi-Press tools settings
            RBtnNone.Checked = true;

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
            cogDefectBoundingRect.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
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
            cogGraphicLabel.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
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
            cogPointMarker.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
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
            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.BothPoints;
            cogLineSegment.Interactive = true;
            cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            cogLineSegment.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
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

        private void AddDefectList(int count, string type)//, CogRectangleAffine rect)
        {
            defectTable.Rows.Add(defectTable.Rows.Count, type, string.Format("({0}, {1})",
                listDefectList[0].CenterX, listDefectList[0].CenterY));

            /*DataSet dtSet = new DataSet();
            DataRow myDataRow;

            dtSet.Tables.Add(defectTable);
            
            myDataRow = defectTable.NewRow();
            myDataRow["No"] = count;
            myDataRow["Name"] = type;
            myDataRow["Detail"] = string.Format("({0}, {1})", rect.CenterX, rect.CenterY); 
            defectTable.Rows.Add(myDataRow);*/
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
            CogCompositeShape container = (CogCompositeShape)CogSerializer.LoadObjectFromFile(path);
            CogGraphicChildren shapes = container.Shapes;
            
            for (int i = 0; i < shapes.Count; i++)
            {
                if (shapes[i].GetType() == typeof(CogRectangleAffine))
                {
                    CogRectangleAffine graphic = (CogRectangleAffine)shapes[i];
                    cogDisplay1.InteractiveGraphics.Add(graphic, "DefectRect", false);
                    shapeContainer.Add(graphic);
                }
                else if (shapes[i].GetType() == typeof(CogGraphicLabel))
                {
                    CogGraphicLabel graphic = (CogGraphicLabel)shapes[i];
                    cogDisplay1.InteractiveGraphics.Add(graphic, "Annotation", false);
                    shapeContainer.Add(graphic);
                }
                else if (shapes[i].GetType() == typeof(CogPointMarker))
                {
                    CogPointMarker graphic = (CogPointMarker)shapes[i];
                    cogDisplay1.InteractiveGraphics.Add(graphic, "Point", false);
                    shapeContainer.Add(graphic);
                }
                else if (shapes[i].GetType() == typeof(CogLineSegment))
                {
                    CogLineSegment graphic = (CogLineSegment)shapes[i];
                    cogDisplay1.InteractiveGraphics.Add(graphic, "Line", false);
                    shapeContainer.Add(graphic);
                }
                else if (shapes[i].GetType() == typeof(CogCompositeShape))
                {
                    //Get the ID of CogCompositeShape
                    if (((CogCompositeShape)shapes[i]).ID == 0)
                    {
                        CogCompositeShape graphic = (CogCompositeShape)shapes[i];
                        cogDisplay1.InteractiveGraphics.Add(graphic, "Arrow", false);
                        shapeContainer.Add(graphic);
                    }
                    else if (((CogCompositeShape)shapes[i]).ID == 1)
                    {
                        CogCompositeShape graphic = (CogCompositeShape)shapes[i];
                        cogDisplay1.InteractiveGraphics.Add(graphic, "Drawing", false);
                        shapeContainer.Add(graphic);
                    }
                    else if (((CogCompositeShape)shapes[i]).ID == 2)
                    {
                        CogCompositeShape graphic = (CogCompositeShape)shapes[i];
                        cogDisplay1.InteractiveGraphics.Add(graphic, "Distance", false);
                        shapeContainer.Add(graphic);
                    }
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
                if (RBtn2Points.Checked)
                {
                    if (2 == multiToolSteps)
                    {
                        //For temporary display
                        CogLineSegment line = new CogLineSegment();
                        line.SetStartLengthRotation(mappedX, mappedY, 0.001, 0);
                        line.LineWidthInScreenPixels = (int)UpDownLineWidth.Value * 2;
                        cogDisplay1.InteractiveGraphics.Add(line, "temp", false);

                        multiToolPositions.Add(new Tuple<double, double>(mappedX, mappedY));
                    }
                    else if (1 == multiToolSteps)
                    {
                        CogLineSegment line = new CogLineSegment();
                        line.SetStartEnd(multiToolPositions[0].Item1, 
                            multiToolPositions[0].Item2, mappedX, mappedY);
                        line.GraphicDOFEnable = CogLineSegmentDOFConstants.BothPoints;
                        line.Interactive = true;
                        line.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        line.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
                        cogDisplay1.InteractiveGraphics.Add(line, "Line", false);
                        shapeContainer.Add(line);
                        //Populate defect table
                        string detail = string.Format("Length: {0}", line.Length);
                        defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);

                        RBtnNone.Checked = true;
                    }
                    //Decrement for 2 points length
                    multiToolSteps -= 1;
                }
                else if (RBtnMultiPointsLength.Checked)
                {
                    if (multiToolSteps == 9999)
                    {
                        //For temporary display
                        CogLineSegment line = new CogLineSegment();
                        line.SetStartLengthRotation(mappedX, mappedY, 0.001, 0);
                        line.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
                        cogDisplay1.InteractiveGraphics.Add(line, "temp", false);
                    }
                    else if (multiToolSteps < 9999)
                    {
                        CogLineSegment line = new CogLineSegment();
                        int index = multiToolPositions.Count - 1;
                        line.SetStartEnd(multiToolPositions[index].Item1, 
                            multiToolPositions[index].Item2, mappedX, mappedY);
                        cogDisplay1.InteractiveGraphics.Add(line, "temp", false);
                    }
                    
                    multiToolPositions.Add(new Tuple<double, double>(mappedX, mappedY));
                    //Decrement for Multi Point Length steps
                    multiToolSteps -= 1;
                }
                else if (RBtnTrigAngle.Checked)
                {
                    if (3 == multiToolSteps)
                    {
                        //For temporary display
                        CogLineSegment line = new CogLineSegment();
                        line.SetStartLengthRotation(mappedX, mappedY, 0.001, 0);
                        line.LineWidthInScreenPixels = (int)UpDownLineWidth.Value * 2;
                        cogDisplay1.InteractiveGraphics.Add(line, "temp", false);

                        multiToolPositions.Add(new Tuple<double, double>(mappedX, mappedY));
                    }
                    else if (2 == multiToolSteps)
                    {
                        //For temporary display
                        CogLineSegment line = new CogLineSegment();
                        int index = multiToolPositions.Count - 1;
                        line.SetStartEnd(multiToolPositions[index].Item1,
                            multiToolPositions[index].Item2, mappedX, mappedY);
                        cogDisplay1.InteractiveGraphics.Add(line, "temp", false);

                        multiToolPositions.Add(new Tuple<double, double>(mappedX, mappedY));
                    }
                    else if (1 == multiToolSteps)
                    {
                        //Add the positions first so everything can be done inside a loop
                        multiToolPositions.Add(new Tuple<double, double>(mappedX, mappedY));

                        CogCompositeShape compositeShape = new CogCompositeShape();
                        compositeShape.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
                        compositeShape.Interactive = true;
                        compositeShape.GraphicDOFEnable = CogCompositeShapeDOFConstants.Position;
                        compositeShape.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
                        compositeShape.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        compositeShape.ID = 100;   //Just a random number for now

                        for (int i = 0; i < multiToolPositions.Count; i++)
                        {
                            //Dot
                            CogLineSegment dot = new CogLineSegment();
                            dot.SelectedSpaceName = "$";
                            dot.Interactive = true;
                            dot.GraphicDOFEnable = CogLineSegmentDOFConstants.Position;
                            dot.LineWidthInScreenPixels = 5;
                            dot.DragLineWidthInScreenPixels = 5;
                            dot.SelectedLineWidthInScreenPixels = 5;
                            dot.Color = CogColorConstants.Red;
                            dot.Changed += Dot_Changed;
                            dot.SetStartLengthRotation(multiToolPositions[i].Item1,
                                multiToolPositions[i].Item2, 0.001, 0);
                            compositeShape.Shapes.Add(dot);

                            if (i < multiToolPositions.Count - 1)
                            {
                                //Line
                                CogLineSegment cogLineSegment = new CogLineSegment();
                                cogLineSegment.SelectedSpaceName = "$";
                                cogLineSegment.Interactive = false;
                                cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
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

                        //TODO: CAN DELETE
                        /*Console.WriteLine(string.Format("X0: {0}, Y0: {1}, X1: {2}, Y1: {3}", 
                            line1.StartX, line1.StartY, line1.EndX, line1.EndY));
                        Console.WriteLine(string.Format("X0: {0}, Y0: {1}, X1: {2}, Y1: {3}",
                            line2.StartX, line2.StartY, line2.EndX, line2.EndY));
                        Console.WriteLine((s2 - s1) / (1 + s2 * s1));
                        Console.WriteLine("S1: " + s1);
                        Console.WriteLine("C1: " + c1);
                        Console.WriteLine("S2: " + s2);
                        Console.WriteLine("C2: " + c2);
                        Console.WriteLine(angle);*/

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
                else if (RBtnSingleRadius.Checked)
                {
                    if (1 < multiToolSteps)
                    {
                        //For temporary display
                        CogLineSegment line = new CogLineSegment();
                        line.SetStartLengthRotation(mappedX, mappedY, 0.001, 0);
                        line.LineWidthInScreenPixels = (int)UpDownLineWidth.Value * 2;
                        cogDisplay1.InteractiveGraphics.Add(line, "temp", false);

                        multiToolPositions.Add(new Tuple<double, double>(mappedX, mappedY));
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
                        compositeShape.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
                        compositeShape.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        compositeShape.ID = 100;   //Just a random number for now

                        //Dot in the middle of the circle
                        CogPointMarker point = new CogPointMarker();
                        point.SelectedSpaceName = "$";
                        point.SetCenterRotationSize(multiToolPositions[0].Item1,
                            multiToolPositions[0].Item2, 0, (int)UpDownLineWidth.Value * 2);
                        //Circle
                        CogCircle circle = new CogCircle();
                        circle.SelectedSpaceName = "$";
                        circle.SetCenterRadius(multiToolPositions[0].Item1, multiToolPositions[0].Item2, radius);
                        circle.GraphicDOFEnable = CogCircleDOFConstants.All;
                        circle.Interactive = true;
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
                else if (RBtn2LineAngle.Checked)
                {
                    if (4 == multiToolSteps)
                    {

                    }
                }
            }
        }

        private void cogDisplay1_MouseMove(object sender, MouseEventArgs e)
        {
            //Update the position
            //Get the position
            double mappedX;
            double mappedY;
            cogDisplay1.GetTransform("#", "*").MapPoint(e.X, e.Y,
                out mappedX, out mappedY);
            previousX = mappedX;
            previousY = mappedY;

            if (drawingMode)
            {
                if (mouseDown)
                {
                    //For temporary display
                    CogLineSegment shape = new CogLineSegment();
                    shape.SetStartEnd(previousX, previousY, mappedX, mappedY);
                    shape.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
                    //To add into CogCompositeShape
                    CogLineSegment shape2 = new CogLineSegment();
                    shape2.SetStartEnd(previousX, previousY, mappedX, mappedY);
                    shape2.SelectedSpaceName = "$";

                    cogDisplay1.InteractiveGraphics.Add(shape, "temp", false);
                    drawing.Shapes.Add(shape2);
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
                drawing.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
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
                compositeShape.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
                compositeShape.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                compositeShape.ID = 100;   //Just a random number for now

                //Create and add each lines into the composite shape, and connections(dots)
                for (int i = 0; i < multiToolPositions.Count; i++)
                {
                    //Dot
                    CogLineSegment dot = new CogLineSegment();
                    dot.SelectedSpaceName = "$";
                    dot.Interactive = true;
                    dot.GraphicDOFEnable = CogLineSegmentDOFConstants.Position;
                    dot.LineWidthInScreenPixels = 5;
                    dot.DragLineWidthInScreenPixels = 5;
                    dot.SelectedLineWidthInScreenPixels = 5;
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
                //To prevent lines to invoke this method after
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

        //METHODS NOT USED
        private void CogLineSegment_Dragging2(object sender, CogDraggingEventArgs e)
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
            Console.WriteLine(index);
            //Update the line position
            if (1 < compositeShape.Shapes.Count)
            {
                if (0 == index)
                {
                    Console.WriteLine("FIRST");
                    //Update the first line
                    ((CogLineSegment)compositeShape.Shapes[1]).StartX = previousX;
                    ((CogLineSegment)compositeShape.Shapes[1]).StartY = previousY;
                }
                else if (0 < index && index < compositeShape.Shapes.Count - 1)
                {
                    Console.WriteLine("MIDDLE");
                    //Update the previous line
                    ((CogLineSegment)compositeShape.Shapes[index-1]).EndX = previousX;
                    ((CogLineSegment)compositeShape.Shapes[index-1]).EndY = previousY;
                    //Update the next line
                    ((CogLineSegment)compositeShape.Shapes[index+1]).StartX = previousX;
                    ((CogLineSegment)compositeShape.Shapes[index+1]).StartY = previousY;
                }
                else if (index == compositeShape.Shapes.Count - 1)
                {
                    Console.WriteLine("LAST");
                    //Update the second last line
                    ((CogLineSegment)compositeShape.Shapes[index-1]).EndX = previousX;
                    ((CogLineSegment)compositeShape.Shapes[index-1]).EndY = previousY;
                }
            }
        }
        private void CogLineSegment_DraggingOriginal(object sender, CogDraggingEventArgs e)
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

            //Update the line position
            if (1 < compositeShape.Shapes.Count)
            {
                if (0 == index)
                {
                    //Update the first line
                    ((CogLineSegment)compositeShape.Shapes[1]).StartX = ((CogLineSegment)compositeShape.Shapes[index]).EndX;
                    ((CogLineSegment)compositeShape.Shapes[1]).StartY = ((CogLineSegment)compositeShape.Shapes[index]).EndY;
                }
                else if (0 < index && index < compositeShape.Shapes.Count - 1)
                {
                    //Update the previous line
                    ((CogLineSegment)compositeShape.Shapes[index - 1]).EndX = ((CogLineSegment)compositeShape.Shapes[index]).StartX;
                    ((CogLineSegment)compositeShape.Shapes[index - 1]).EndY = ((CogLineSegment)compositeShape.Shapes[index]).StartY;
                    //Update the next line
                    ((CogLineSegment)compositeShape.Shapes[index + 1]).StartX = ((CogLineSegment)compositeShape.Shapes[index]).EndX;
                    ((CogLineSegment)compositeShape.Shapes[index + 1]).StartY = ((CogLineSegment)compositeShape.Shapes[index]).EndY;
                }
                else
                {
                    //Update the second last line
                    ((CogLineSegment)compositeShape.Shapes[index - 1]).EndX = ((CogLineSegment)compositeShape.Shapes[index]).StartX;
                    ((CogLineSegment)compositeShape.Shapes[index - 1]).EndY = ((CogLineSegment)compositeShape.Shapes[index]).StartY;
                }
            }
        }
    }
}
