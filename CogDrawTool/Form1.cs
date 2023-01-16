using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
//using System.Windows.Media;

namespace CogDrawTool
{
    public partial class DrawToolFrm : Form
    {
        private DataTable defectTable;
        private String defectCategory;
        private String _path = "C:/Users/ryuya/Downloads";

        public delegate void DisplayImageDelegate(Bitmap frameBuffer);

        //Test variables
        private List<CogCompositeShape> compositeShapeList = new List<CogCompositeShape>();

        //Not used
        private List<CogRectangleAffine> listDefectList;
        private List<CogGraphicLabel> labelList;
        private List<CogPointMarker> pointList;
        private List<CogLineSegment> lineList;
        private List<CogCompositeShape> tempDefectList = new List<CogCompositeShape>();

        public DrawToolFrm()
        {
            InitializeComponent();
        }

        private void onPaint(object sender, PaintEventArgs e)
        {
            //Point[] point = new Point[8];
            GraphicsPath path = new GraphicsPath();
            path.AddLine(new PointF(100, 100), new PointF(200, 200));
            path.AddArc(new Rectangle(new System.Drawing.Point(1, 1), new System.Drawing.Size(20, 20)),
                90, 90);

            if (path != null)
            {
                e.Graphics.FillPath(new System.Drawing.Drawing2D.LinearGradientBrush(
                    new PointF(300, 300), new PointF(350, 350), Color.Black, Color.Black), path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            /*var myPen = new Pen(Color.Aqua);
            var area = new Rectangle(new System.Drawing.Point(0, 0), 
                new System.Drawing.Size(this.Size.Width - 1, this.Size.Height - 1));
            e.Graphics.DrawRectangle(myPen, area);*/
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            /*CogCompositeShape compositeShape = new CogCompositeShape();
            //Line1
            CogLineSegment line1 = new CogLineSegment();
            line1.SetStartLengthRotation(500, 500, 100, 0);
            compositeShape.Shapes.Add(line1);
            //Line2
            CogLineSegment line2 = new CogLineSegment();
            line2.SetStartLengthRotation(400, 400, 50, 0);
            compositeShape.Shapes.Add(line2);
            //Point1
            CogPointMarker point1 = new CogPointMarker();
            point1.SetCenterRotationSize(500, 500, 0, 5);
            compositeShape.Shapes.Add(point1);
            //Shape property
            compositeShape.Interactive = true;
            //cogDisplay1.InteractiveGraphics.Add(line1, "a", false);

            Color black = Color.FromArgb(255, 0, 0, 0);
            Pen pen = new Pen(black);
            pen.Width = 5;*/
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
            cogDefectBoundingRect.TipText = string.Format("Defect No: {0}", cogDisplay1.InteractiveGraphics.Count);
            cogDefectBoundingRect.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
            cogDisplay1.InteractiveGraphics.Add(cogDefectBoundingRect, "DefectRect", false);
            
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
            cogGraphicLabel.TipText = string.Format("Defect No: {0}", cogDisplay1.InteractiveGraphics.Count);
            cogGraphicLabel.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
            cogDisplay1.InteractiveGraphics.Add(cogGraphicLabel, "Annotation", false);

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
            cogPointMarker.TipText = string.Format("Defect No: {0}", cogDisplay1.InteractiveGraphics.Count);
            cogPointMarker.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
            cogDisplay1.InteractiveGraphics.Add(cogPointMarker, "Point", false);
            
            //Populate defect table
            string detail = string.Format("({0}, {1})",
                cogPointMarker.X,
                cogPointMarker.Y);
            defectTable.Rows.Add(defectTable.Rows.Count, defectCategory, detail);
        }

        private void BtnLine_Click(object sender, EventArgs e)
        {
            CogLineSegment cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(500, 500, 50, 0);
            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.BothPoints;
            cogLineSegment.Interactive = true;
            cogLineSegment.TipText = string.Format("Defect No: {0}", cogDisplay1.InteractiveGraphics.Count);
            cogLineSegment.LineWidthInScreenPixels = (int)UpDownLineWidth.Value;
            cogDisplay1.InteractiveGraphics.Add(cogLineSegment, "Line", false);
            //Populate defect table
            string detail = string.Format("({0}, {1})",
                cogLineSegment.StartX,
                cogLineSegment.StartY);
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
            String tipText;
            int index;
            CogInteractiveGraphicsContainer graphics = cogDisplay1.InteractiveGraphics;
           
            for (int i = 0; i < graphics.Count; i++)
            {
                //Get the original index of cogDisplay1.InteractiveGraphics[i].
                // cogDisplay1 item position is dynamic, the original index
                //must be conserved so datatable can be updated. 
                tipText = graphics[i].TipText.ToString();
                index = int.Parse(tipText[tipText.Length - 1].ToString());
                
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
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            //Convert InteractiveGraphics into CogCompositeShape
            CogCompositeShape resultGraphic = new CogCompositeShape();
            //Allows each shape to store their properties (Like interactive, DOF...etc)
            resultGraphic.CompositionMode = CogCompositeShapeCompositionModeConstants.Freeform;

            //Add each interactive graphics into CompositeShape
            for (int i = 0; i < cogDisplay1.InteractiveGraphics.Count; i++)
            {
                resultGraphic.Shapes.Add((ICogGraphicParentChild)cogDisplay1.InteractiveGraphics[i]);
            }

            if (resultGraphic.Shapes.Count > 0)
            {
                //Export vpp
                CogSerializer.SaveObjectToFile(resultGraphic,
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
           
            //Clear display and datatable
            cogDisplay1.InteractiveGraphics.Clear();
            CreateDefectListTable(); 

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
                }
                else if (shapes[i].GetType() == typeof(CogGraphicLabel))
                {
                    CogGraphicLabel graphic = (CogGraphicLabel)shapes[i];
                    cogDisplay1.InteractiveGraphics.Add(graphic, "Annotation", false);
                }
                else if (shapes[i].GetType() == typeof(CogPointMarker))
                {
                    CogPointMarker graphic = (CogPointMarker)shapes[i];
                    cogDisplay1.InteractiveGraphics.Add(graphic, "Point", false);
                }
                else if (shapes[i].GetType() == typeof(CogLineSegment))
                {
                    CogLineSegment graphic = (CogLineSegment)shapes[i];
                    cogDisplay1.InteractiveGraphics.Add(graphic, "Line", false);
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
    }
}
