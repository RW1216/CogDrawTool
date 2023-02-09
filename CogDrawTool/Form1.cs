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
using System.Windows.Media.Animation;
using AForge.Math.Geometry;
using AForge;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace CogDrawTool
{
    public partial class DrawToolFrm : Form
    {
        private DataTable defectTable;
        private String defectCategory;
        private String _path = "C:/Users/ryuya/Downloads";
        private List<ICogGraphicParentChild> shapeContainer = new List<ICogGraphicParentChild>();
        private CogCompositeShape previousGraphic = new CogCompositeShape();
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
        //Stores save path
        CommonOpenFileDialog SaveFile = new CommonOpenFileDialog();

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
            shape7.LineWidthInScreenPixels = 5;
            shape7.Interactive = true;
            cogDisplay1.InteractiveGraphics.Add(shape7, "temp", false);

            CogEllipse shape8 = new CogEllipse();
            shape8.SetCenterXYRadiusXYRotation(580, 500, 1, 0.5, 0);
            shape8.LineWidthInScreenPixels = lineWidth;
            cogDisplay1.InteractiveGraphics.Add(shape8, "temp", false);
            
            
            
        }

        private void btnTest2_Click(object sender, EventArgs e)
        {
            Console.WriteLine("BUTTON 2");
            if (shapeContainer[0].GetType() == typeof(CogCompositeShape))
            {
                CogCompositeShape compositeShape = (CogCompositeShape)shapeContainer[0];
                
                CogLineSegment line = (CogLineSegment)compositeShape.Shapes[1];
                Console.WriteLine(line.Length);
            }
        }

        private void btnTest3_Click(object sender, EventArgs e)
        {
            Console.WriteLine("BUTTON 3");
            CogLineSegment shape7 = new CogLineSegment();
            shape7.SetStartLengthRotation(560, 500, 0.5, 0);
            shape7.LineWidthInScreenPixels = 10;
            shape7.Interactive = true;
            cogDisplay1.InteractiveGraphics.Add(shape7, "temp", false);
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

            //Set the default path
            string workingDirectory = Environment.CurrentDirectory;
            string path = Directory.GetParent(workingDirectory).Parent.FullName;
            path = Path.Combine(path, "Saved Files");
            SaveFile.InitialDirectory = path;
            SaveFile.IsFolderPicker = true;
            //Set the initial file name
            TBFileName.Text = "Temporary Name";

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
            cogDefectBoundingRect.Color = lineColor;
            
            string detail = string.Format("({0}, {1})",
                cogDefectBoundingRect.CenterX,
                cogDefectBoundingRect.CenterY);
            AddInteractiveDisplay(cogDefectBoundingRect, detail);
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
            cogGraphicLabel.Color = lineColor;
            
            string detail = string.Format("({0}, {1})",
                cogGraphicLabel.X, 
                cogGraphicLabel.Y);
            AddInteractiveDisplay(cogGraphicLabel, detail);
        }

        private void BtnPoint_Click(object sender, EventArgs e)
        {
            CogPointMarker cogPointMarker = new CogPointMarker();
            cogPointMarker.SetCenterRotationSize(500, 500, 0, 5);
            cogPointMarker.GraphicDOFEnable = CogPointMarkerDOFConstants.Position;
            cogPointMarker.Interactive = true;
            cogPointMarker.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            cogPointMarker.LineWidthInScreenPixels = lineWidth;
            cogPointMarker.Color = lineColor;
            
            string detail = string.Format("({0}, {1})",
                cogPointMarker.X,
                cogPointMarker.Y);
            AddInteractiveDisplay(cogPointMarker, detail);
        }

        private void BtnLine_Click(object sender, EventArgs e)
        {
            CogLineSegment cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(200, 200, 500, 0);
            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.All;
            cogLineSegment.Interactive = true;
            cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
            cogLineSegment.LineWidthInScreenPixels = lineWidth;
            cogLineSegment.Color = lineColor;

            string detail = string.Format("Length: {0}", cogLineSegment.Length);
            AddInteractiveDisplay(cogLineSegment, detail);
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
            arrowComponents.LineWidthInScreenPixels = lineWidth;
            arrowComponents.Color = lineColor;
            arrowComponents.ID = 0;
            arrowComponents.ParentFromChildTransform = arrowComponents.GetParentFromChildTransform();

            //NOTICE: CogCompositeShape do not have x and y variable.
            //   The shapes x and y value does not update
            //   even after the CogCompositeShape position change. 
            //   Perhaps could ask the user for details, or use other way
            //   that can get the x and y position.
            string detail = string.Format("{0}", "Type in your details");
            AddInteractiveDisplay(arrowComponents, detail);
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
            distanceComponents.LineWidthInScreenPixels = lineWidth;
            distanceComponents.Color = lineColor;
            distanceComponents.ID = 2;
            distanceComponents.ParentFromChildTransform = distanceComponents.GetParentFromChildTransform();
            //distanceComponents.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
            
            //NOTICE: CogCompositeShape do not have x and y variable.
            //   The shapes x and y value does not update
            //   even after the CogCompositeShape position changes. 
            //   Perhaps could ask the user for details, or use other way
            //   that can get the x and y position.
            string detail = string.Format("{0}", "Type in your details");
            AddInteractiveDisplay(distanceComponents, detail);
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
                    detail = string.Format("Length: {0}", ((CogLineSegment)graphics[i]).Length);
                    //Update if detail does not match 
                    if (defectTable.Rows[index]["Detail"].ToString() != detail)
                    {
                        defectTable.Rows[index]["Detail"] = detail;
                    }
                }
                else if (graphics[i].GetType() == typeof(CogCompositeShape))
                {
                    //ID0 = arrows
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
                    //ID1 = drawings
                    else if (((CogCompositeShape)graphics[i]).ID == 1)
                    {
                        detail = string.Format("{0}", "Enter your details");
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    //ID2 = distance
                    else if (((CogCompositeShape)graphics[i]).ID == 2)
                    {
                        detail = string.Format("{0}", "Enter your details");
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    else if (((CogCompositeShape)graphics[i]).ID == 5)
                    {
                        double totalLength = 0;
                        //Iterate and add on all line distance
                        for (int j = 1; j < ((CogCompositeShape)graphics[i]).Shapes.Count; j += 2)
                        {
                            totalLength += ((CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[j]).Length;
                        }
                        detail = string.Format("Total Length: {0}", totalLength);
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    else if (((CogCompositeShape)graphics[i]).ID == 6)
                    {
                        //Compute the angle
                        CogLineSegment line1 = (CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[1];
                        CogLineSegment line2 = (CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[3];

                        float angle = AForge.Math.Geometry.GeometryTools.GetAngleBetweenVectors(
                            new AForge.Point((float)line1.EndX, (float)line1.EndY),
                            new AForge.Point((float)line1.StartX, (float)line1.StartY),
                            new AForge.Point((float)line2.EndX, (float)line2.EndY));

                        detail = string.Format("Angle: {0}", angle);
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    else if (((CogCompositeShape)graphics[i]).ID == 7)
                    {
                        double radius = ((CogCircle)((CogCompositeShape)graphics[i]).Shapes[1]).Radius;
                        detail = string.Format("Radius: {0}", radius);
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    else if (((CogCompositeShape)graphics[i]).ID == 8)
                    {
                        //Compute the angle
                        float angle = 0;
                        CogLineSegment line1 = (CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[0];
                        CogLineSegment line2 = (CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[1];

                        LineSegment ls1 = new LineSegment(new AForge.Point((float)line1.StartX, (float)line1.StartY),
                            new AForge.Point((float)line1.EndX, (float)line1.EndY));
                        LineSegment ls2 = new LineSegment(new AForge.Point((float)line2.StartX, (float)line2.StartY),
                            new AForge.Point((float)line2.EndX, (float)line2.EndY));

                        //Find the intersect
                        AForge.Point? intersect = ls1.GetIntersectionWith(ls2);
                        if (intersect != null)
                        {
                            //Get the angle
                            angle = AForge.Math.Geometry.GeometryTools.GetAngleBetweenVectors(
                                new AForge.Point(intersect.Value.X, intersect.Value.Y),
                                new AForge.Point((float)line1.StartX, (float)line1.StartY),
                                new AForge.Point((float)line2.StartX, (float)line2.StartY));
                            detail = string.Format("Angle: {0}", angle);
                        }
                        else
                        {
                            //Get the angle
                            detail = "Invalid";
                        }
                        
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    else if (((CogCompositeShape)graphics[i]).ID == 9)
                    {
                        double length = ((CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[1]).Length;
                        detail = string.Format("Length: {0}", length);
                        //Update if detail does not match
                        if (defectTable.Rows[index]["Detail"].ToString() != detail)
                        {
                            defectTable.Rows[index]["Detail"] = detail;
                        }
                    }
                    else if (((CogCompositeShape)graphics[i]).ID == 10)
                    {
                        double length = ((CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[4]).Length;
                        detail = string.Format("Length: {0}", length);
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
            //Allows each shape to maintain their properties (Like interactive, DOF...etc)
            container.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;
            
            //Add each shapes into CompositeShape
            for (int i = 0; i < shapeContainer.Count; i++)
            {
                container.Shapes.Add(shapeContainer[i]);
            }
            
            if (container.Shapes.Count > 0)
            {
                //Export vpp
                CogSerializer.SaveObjectToFile(container, Path.Combine(SaveFile.InitialDirectory, TBFileName.Text + ".vpp"));
                container.Dispose();

                //Export bmp
                Bitmap displayBitmap = (Bitmap)cogDisplay1.CreateContentBitmap(
                    CogDisplayContentBitmapConstants.Display, null, 0);
                displayBitmap.Save(Path.Combine(SaveFile.InitialDirectory, TBFileName.Text + ".bmp"));

                //Export CSV
                using (var streamWriter = new StreamWriter(Path.Combine(SaveFile.InitialDirectory, TBFileName.Text + ".csv"), false))
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
            //Clear the display, shapeContainer and datatable.
            cogDisplay1.InteractiveGraphics.Clear();
            CreateDefectListTable();
            shapeContainer.Clear();

            //Load vpp file
            string vppPath = Path.Combine(SaveFile.InitialDirectory, TBFileName.Text + ".vpp");
            string csvPath = Path.Combine(SaveFile.InitialDirectory, TBFileName.Text + ".csv");
            if (File.Exists(vppPath) && File.Exists(csvPath))
            {
                CogCompositeShape loadedCompositeShape = (CogCompositeShape)CogSerializer.LoadObjectFromFile(vppPath);
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
                            for (int j = 0; j < compositeShape.Shapes.Count; j += 2)
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
                        else if (compositeShape.ID == 8)
                        {
                            compositeShape.Shapes[0].Changed += TwoLineAngle_Changed;
                            compositeShape.Shapes[1].Changed += TwoLineAngle_Changed;
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
                var reader = new StreamReader(Path.Combine(SaveFile.InitialDirectory, TBFileName.Text + ".csv"));
                var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                using (var dr = new CsvDataReader(csv))
                {
                    defectTable.Load(dr);
                }
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

                //Save the currently selected graphic
                for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
                {
                    if (cogDisplay1.InteractiveGraphics[i].Selected)
                    {
                        if (cogDisplay1.InteractiveGraphics[i].GetType() == typeof(CogCompositeShape))
                        {
                            previousGraphic = new CogCompositeShape((CogCompositeShape)cogDisplay1.InteractiveGraphics[i]);
                        }
                    }
                }

                //Save positions for Multi-Press tools
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
                        line.Color = lineColor;
                        string detail = string.Format("Length: {0}", line.Length);
                        AddInteractiveDisplay(line, detail);

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
                            dot.Color = dotColor;
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
                                cogLineSegment.Color = lineColor;
                                cogLineSegment.SetStartEnd(multiToolPositions[i].Item1,
                                    multiToolPositions[i].Item2, multiToolPositions[i + 1].Item1,
                                    multiToolPositions[i + 1].Item2);
                                compositeShape.Shapes.Add(cogLineSegment);
                            }
                        }

                        //Compute the angle
                        CogLineSegment line1 = (CogLineSegment)compositeShape.Shapes[1];
                        CogLineSegment line2 = (CogLineSegment)compositeShape.Shapes[3];

                        float angle = AForge.Math.Geometry.GeometryTools.GetAngleBetweenVectors(
                            new AForge.Point((float)line1.EndX, (float)line1.EndY),
                            new AForge.Point((float)line1.StartX, (float)line1.StartY),
                            new AForge.Point((float)line2.EndX, (float)line2.EndY));

                        string detail = string.Format("Angle: {0}", angle);
                        AddInteractiveDisplay(compositeShape, detail);

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
                        point.Color = dotColor;
                        point.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        point.SetCenterRotationSize(multiToolPositions[0].Item1,
                            multiToolPositions[0].Item2, 0, lineWidth + 3);
                        //Circle
                        CogCircle circle = new CogCircle();
                        circle.SelectedSpaceName = "$";
                        circle.LineWidthInScreenPixels = lineWidth;
                        circle.Color = lineColor;
                        circle.SetCenterRadius(multiToolPositions[0].Item1, multiToolPositions[0].Item2, radius);
                        circle.GraphicDOFEnable = CogCircleDOFConstants.All;
                        circle.Interactive = true;
                        circle.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        circle.Changed += Circle_Changed;
                        //Add into composite shape
                        compositeShape.Shapes.Add(point);
                        compositeShape.Shapes.Add(circle);

                        string detail = string.Format("Radius: {0}", circle.Radius);
                        AddInteractiveDisplay(compositeShape, detail);

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

                        CogLineSegment cogLineSegment;
                        //Line
                        for (int i = 0; i < 4; i += 2)
                        {
                            cogLineSegment = new CogLineSegment();
                            cogLineSegment.SelectedSpaceName = "$";
                            cogLineSegment.Interactive = true;
                            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.BothPoints;
                            cogLineSegment.LineWidthInScreenPixels = lineWidth;
                            cogLineSegment.Color = lineColor;   
                            cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                            cogLineSegment.Changed += TwoLineAngle_Changed;
                            cogLineSegment.SetStartEnd(multiToolPositions[i].Item1,
                                multiToolPositions[i].Item2, multiToolPositions[i + 1].Item1,
                                multiToolPositions[i + 1].Item2);
                            compositeShape.Shapes.Add(cogLineSegment);
                        }

                        CogLineSegment line1 = (CogLineSegment)compositeShape.Shapes[0];
                        CogLineSegment line2 = (CogLineSegment)compositeShape.Shapes[1];
                        LineSegment ls1 = new LineSegment(new AForge.Point((float)line1.StartX, (float)line1.StartY),
                            new AForge.Point((float)line1.EndX, (float)line1.EndY));
                        LineSegment ls2 = new LineSegment(new AForge.Point((float)line2.StartX, (float)line2.StartY),
                            new AForge.Point((float)line2.EndX, (float)line2.EndY));
                        
                        //Find the intersect
                        AForge.Point? intersect = ls1.GetIntersectionWith(ls2);
                        if (intersect != null)
                        {
                            //Get the angle
                            float angle = AForge.Math.Geometry.GeometryTools.GetAngleBetweenVectors(
                                new AForge.Point(intersect.Value.X, intersect.Value.Y),
                                new AForge.Point((float)line1.StartX, (float)line1.StartY),
                                new AForge.Point((float)line2.StartX, (float)line2.StartY));

                            //Create a line that indicates the angle
                            LineSegment shorterLine = new LineSegment(new AForge.Point((float)line1.StartX, (float)line1.StartY),
                                new AForge.Point((float)line1.EndX, (float)line1.EndY));
                            LineSegment longerLine = new LineSegment(new AForge.Point((float)line2.StartX, (float)line2.StartY),
                                new AForge.Point((float)line2.EndX, (float)line2.EndY));
                            if (ls1.Length > ls2.Length)
                            {
                                longerLine = new LineSegment(new AForge.Point((float)line1.StartX, (float)line1.StartY),
                                new AForge.Point((float)line1.EndX, (float)line1.EndY));
                                shorterLine = new LineSegment(new AForge.Point((float)line2.StartX, (float)line2.StartY),
                                    new AForge.Point((float)line2.EndX, (float)line2.EndY));
                            }

                            float ratio = (float)1 / (float)6;
                            //Get the position of the start line
                            float xDiff = (shorterLine.Start.X - intersect.Value.X) * ratio;
                            float yDiff = (shorterLine.Start.Y - intersect.Value.Y) * ratio;
                            AForge.Point startPoint = new AForge.Point(intersect.Value.X + xDiff,
                                intersect.Value.Y + yDiff);
                            //Get the position of longer line from start point at specific distance (of shorter line)
                            float shortLineLength = (float)Math.Sqrt(Math.Pow(shorterLine.Start.X - intersect.Value.X, 2)
                                + Math.Pow(shorterLine.Start.Y - intersect.Value.Y, 2));
                            float longLineLength = (float)Math.Sqrt(Math.Pow(longerLine.Start.X - intersect.Value.X, 2)
                                + Math.Pow(longerLine.Start.Y - intersect.Value.Y, 2));
                            float distanceRatio = shortLineLength / longLineLength;
                            xDiff = (longerLine.Start.X - intersect.Value.X) * distanceRatio;
                            yDiff = (longerLine.Start.Y - intersect.Value.Y) * distanceRatio;
                            xDiff = xDiff * ratio;
                            yDiff = yDiff * ratio;
                            //Get the position of the end line
                            AForge.Point endPoint = new AForge.Point(intersect.Value.X + xDiff,
                                intersect.Value.Y + yDiff);

                            cogLineSegment = new CogLineSegment();
                            cogLineSegment.SelectedSpaceName = "$";
                            cogLineSegment.Interactive = true;
                            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
                            cogLineSegment.LineWidthInScreenPixels = lineWidth;
                            cogLineSegment.Color = dotColor;
                            cogLineSegment.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                            cogLineSegment.SetStartEnd(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
                            compositeShape.Shapes.Add(cogLineSegment);

                            string detail = string.Format("Angle: {0}", angle);
                            AddInteractiveDisplay(compositeShape, detail);
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Invalid Position");
                        }
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
                        cogLineSegment.Color = lineColor;
                        //cogLineSegment.Changed += PerpLengthLine_Changed;
                        cogLineSegment.SetStartEnd(multiToolPositions[0].Item1,
                            multiToolPositions[0].Item2, multiToolPositions[1].Item1,
                            multiToolPositions[1].Item2);
                        compositeShape.Shapes.Add(cogLineSegment);

                        //Find the perpendicular gradient of the line above
                        CogLineSegment line1 = (CogLineSegment)compositeShape.Shapes[0];
                        
                        //To prevent the division by 0.
                        double m1 = (line1.EndY - line1.StartY) / (line1.EndX - line1.StartX);
                        double m2 = -1 / m1;
                        if ((line1.EndX - line1.StartX) == 0)
                        {
                            m1 = 9999999999999999999;
                            m2 = 0;
                        }
                        if (m1 == 0) { m2 = 9999999999999999999; }
                        double c1 = line1.StartY - (m1 * line1.StartX);
                        double c2 = mappedY - (m2 * mappedX);
                        //Get the intersection of 2 lines
                        double intersectX = (c2 - c1) / (m1 - m2);
                        double intersectY = (m1 * intersectX) + c1;
                        if (m2 == 0)
                        {
                            intersectY = mappedY;
                        }
                        //Perpendicular Line that stops on the intersection 
                        CogLineSegment perpLine = new CogLineSegment();
                        perpLine.SelectedSpaceName = "$";
                        perpLine.Interactive = true;
                        perpLine.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
                        perpLine.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        perpLine.LineWidthInScreenPixels = lineWidth;
                        perpLine.Color = dotColor;
                        //perpLine.Changed += PerpLengthLine_Changed;
                        perpLine.SetStartEnd(mappedX, mappedY, intersectX, intersectY);
                        compositeShape.Shapes.Add(perpLine);

                        string detail = string.Format("Length: {0}", perpLine.Length);
                        AddInteractiveDisplay(compositeShape, detail);

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
                        dot1.Color = dotColor;
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
                        circle1.Color = lineColor;
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
                        dot2.Color = dotColor;
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
                        circle2.Color = lineColor;
                        circle2.Interactive = true;
                        compositeShape.Shapes.Add(circle2);
                        //Line
                        CogLineSegment line = new CogLineSegment();
                        line.SelectedSpaceName = "$";
                        line.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
                        line.TipText = string.Format("Defect No: {0}", shapeContainer.Count);
                        line.LineWidthInScreenPixels = lineWidth;
                        line.Color = lineColor;
                        line.SetStartEnd(multiToolPositions[0].Item1, multiToolPositions[0].Item2,
                            multiToolPositions[2].Item1, multiToolPositions[2].Item2);
                        compositeShape.Shapes.Add(line);

                        string detail = string.Format("Length: {0}", line.Length);
                        AddInteractiveDisplay(compositeShape, detail);

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
            line.Color = lineColor;
            line.SetStartEnd(startX, startY, endX, endY);
            cogDisplay1.InteractiveGraphics.Add(line, "temp", false);
        }

        private void AddTemporaryPoint(double x, double y)
        {
            //For temporary display
            CogLineSegment line = new CogLineSegment();
            line.SetStartLengthRotation(x, y, 0.0001, 0);
            line.LineWidthInScreenPixels = lineWidth + 3;
            line.Color = dotColor;
            cogDisplay1.InteractiveGraphics.Add(line, "temp", false);
        }

        private void AddTemporaryCircle(double centerX, double centerY, double radius)
        {
            //For temporary display
            CogCircle circle = new CogCircle();
            circle.SetCenterRadius(centerX, centerY, radius);
            circle.LineWidthInScreenPixels = lineWidth;
            circle.Color = lineColor;
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
                    
                    //Add temporary display
                    AddTemporaryLine(previousX, previousY, mappedX, mappedY);
                    //To add into CogCompositeShape
                    CogLineSegment shape = new CogLineSegment();
                    shape.SetStartEnd(previousX, previousY, mappedX, mappedY);
                    shape.SelectedSpaceName = "$";

                    drawing.Shapes.Add(shape);

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
                    //Remove temporary drawings
                    cogDisplay1.InteractiveGraphics.Remove("temp");
                    //Add graphics
                    string detail = string.Format("{0}", "Enter your details");
                    AddInteractiveDisplay(drawing, detail);
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
                drawing.Color = lineColor;
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
                    dot.Color = dotColor;
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
                        cogLineSegment.Color = lineColor;
                        cogLineSegment.SetStartEnd(multiToolPositions[i].Item1,
                            multiToolPositions[i].Item2, multiToolPositions[i + 1].Item1,
                            multiToolPositions[i + 1].Item2);
                        compositeShape.Shapes.Add(cogLineSegment);
                        //Update the total length
                        totalLength += cogLineSegment.Length;
                    }
                }

                //Add interactive graphics
                string detail = string.Format("Total Length: {0}", totalLength);
                AddInteractiveDisplay(compositeShape, detail);

                RBtnNone.Checked = true;
            }
        }

        private void Dot_Changed(object sender, CogChangedEventArgs e)
        {
            if (!changing)
            {
                //To prevent another changed methods to be invoked while
                //the position is getting changed.
                changing = true;
                
                CogCompositeShape compositeShape = new CogCompositeShape();
                int index = -1;
                CogCompositeShape temp;
                //Find the index of changed dot from composite shape in cogInteractiveDisplay
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

        private void TwoLineAngle_Changed(object sender, CogChangedEventArgs e)
        {
            CogCompositeShape compositeShape = new CogCompositeShape();
            CogCompositeShape temp;
            //Find the index of changed dot from composite shape in cogInteractiveDisplay
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

            if (1 < compositeShape.Shapes.Count)
            {
                CogLineSegment line1 = (CogLineSegment)compositeShape.Shapes[0];
                CogLineSegment line2 = (CogLineSegment)compositeShape.Shapes[1];
                CogLineSegment line3 = (CogLineSegment)compositeShape.Shapes[2];
                LineSegment ls1 = new LineSegment(new AForge.Point((float)line1.StartX, (float)line1.StartY),
                    new AForge.Point((float)line1.EndX, (float)line1.EndY));
                LineSegment ls2 = new LineSegment(new AForge.Point((float)line2.StartX, (float)line2.StartY),
                    new AForge.Point((float)line2.EndX, (float)line2.EndY));

                //Find the intersect
                AForge.Point? intersect = ls1.GetIntersectionWith(ls2);
                if (intersect != null)
                {
                    //Create a line that indicates the angle
                    LineSegment shorterLine = new LineSegment(new AForge.Point((float)line1.StartX, (float)line1.StartY),
                        new AForge.Point((float)line1.EndX, (float)line1.EndY));
                    LineSegment longerLine = new LineSegment(new AForge.Point((float)line2.StartX, (float)line2.StartY),
                        new AForge.Point((float)line2.EndX, (float)line2.EndY));
                    if (ls1.Length > ls2.Length)
                    {
                        longerLine = new LineSegment(new AForge.Point((float)line1.StartX, (float)line1.StartY),
                        new AForge.Point((float)line1.EndX, (float)line1.EndY));
                        shorterLine = new LineSegment(new AForge.Point((float)line2.StartX, (float)line2.StartY),
                            new AForge.Point((float)line2.EndX, (float)line2.EndY));
                    }

                    float ratio = (float)1 / (float)6;
                    //Get the position of the start line
                    float xDiff = (shorterLine.Start.X - intersect.Value.X) * ratio;
                    float yDiff = (shorterLine.Start.Y - intersect.Value.Y) * ratio;
                    AForge.Point startPoint = new AForge.Point(intersect.Value.X + xDiff,
                        intersect.Value.Y + yDiff);
                    //Get the position of longer line from start point at specific distance (of shorter line)
                    float shortLineLength = (float)Math.Sqrt(Math.Pow(shorterLine.Start.X - intersect.Value.X, 2)
                        + Math.Pow(shorterLine.Start.Y - intersect.Value.Y, 2));
                    float longLineLength = (float)Math.Sqrt(Math.Pow(longerLine.Start.X - intersect.Value.X, 2)
                        + Math.Pow(longerLine.Start.Y - intersect.Value.Y, 2));
                    float distanceRatio = shortLineLength / longLineLength;
                    xDiff = (longerLine.Start.X - intersect.Value.X) * distanceRatio;
                    yDiff = (longerLine.Start.Y - intersect.Value.Y) * distanceRatio;
                    xDiff = xDiff * ratio;
                    yDiff = yDiff * ratio;
                    //Get the position of the end line
                    AForge.Point endPoint = new AForge.Point(intersect.Value.X + xDiff,
                        intersect.Value.Y + yDiff);
                    line3.SetStartEnd(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
                }
                else
                {
                    Console.WriteLine("INVALID");
                    line1.StartX = ((CogLineSegment)previousGraphic.Shapes[0]).StartX;
                    line1.StartY = ((CogLineSegment)previousGraphic.Shapes[0]).StartY;
                    line1.EndX = ((CogLineSegment)previousGraphic.Shapes[0]).EndX;
                    line1.EndY = ((CogLineSegment)previousGraphic.Shapes[0]).EndY;
                    line2.StartX = ((CogLineSegment)previousGraphic.Shapes[1]).StartX;
                    line2.StartY = ((CogLineSegment)previousGraphic.Shapes[1]).StartY;
                    line2.EndX = ((CogLineSegment)previousGraphic.Shapes[1]).EndX;
                    line2.EndY = ((CogLineSegment)previousGraphic.Shapes[1]).EndY;
                }
            }
        }
        private void Circle_Changed(object sender, CogChangedEventArgs e)
        {
            if (!changing)
            {
                //Might not be neede for circle changed. (But required for multi line changed)
                changing = true;

                CogCompositeShape compositeShape = new CogCompositeShape();
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

            //Update the line and circle position
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

                if (1 < compositeShape.Shapes.Count)
                {
                    if (0 == index)
                    {
                        
                    }
                }
                
                changing = false;
            }
        }

        private void AddInteractiveDisplay(ICogGraphicParentChild graphic, string detail)
        {
            shapeContainer.Add(graphic);
            cogDisplay1.InteractiveGraphics.Add((ICogGraphicInteractive)graphic, "shape", false);
            //Populate defect table
            defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);
        }

        private int GetShapeIndex_ShapeContainer(ICogGraphicParentChild graphic)
        {
            for (int i = 0; i < shapeContainer.Count; i++)
            {
                if (graphic.Equals(shapeContainer[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetShapeIndex_CogInteractiveDisplay(ICogGraphicParentChild graphic)
        {
            for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
            {
                if (graphic.Equals(cogDisplay1.InteractiveGraphics[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        private void UpDownLineWidth_ValueChanged(object sender, EventArgs e)
        {
            lineWidth = (int)(sender as NumericUpDown).Value;
        }

        private void btnTrash_Click(object sender, EventArgs e)
        {
            int displayIndex = -1;
            int index = -1;
            bool isSelected = false;
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
                            isSelected = true;
                        }
                    }
                }
            }

            if (isSelected)
            {
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
        }

        private void PBColor_Click(object sender, EventArgs e)
        {
            PictureBox p = (PictureBox)sender;
            /*int r = p.BackColor.R;
            int g = p.BackColor.G;
            int b = p.BackColor.B;
            //OLE is in BGR format
            int OLE = ((b * 256 * 256) + (g * 256) + r);*/
            int OLE = ColorTranslator.ToOle(p.BackColor);

            if (RBtnLineColor.Checked)
            {
                lineColor = (CogColorConstants)(OLE);
                RBtnLineColor.BackColor = p.BackColor;
            }
            else if (RBtnDotColor.Checked)
            {
                dotColor = (CogColorConstants)(OLE);
                RBtnDotColor.BackColor = p.BackColor;
            }
        }

        private void btnSelectPath_Click(object sender, EventArgs e)
        {
            if (SaveFile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SaveFile.InitialDirectory = SaveFile.FileName;
            }
        }

        private void btnColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                int OLE = ColorTranslator.ToOle(colorDialog1.Color);
                
                if (RBtnLineColor.Checked)
                {
                    lineColor = (CogColorConstants)(OLE);
                    RBtnLineColor.BackColor = colorDialog1.Color;
                }
                else if (RBtnDotColor.Checked)
                {
                    dotColor = (CogColorConstants)(OLE);
                    RBtnDotColor.BackColor = colorDialog1.Color;
                }
            }
        }
    }
}
