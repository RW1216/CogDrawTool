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
//using System.Windows.Media;
using System.Xml.Serialization;
using System.Xml.Linq;
using QWhale.Editor.TextSource;

namespace CogDrawTool
{
    public partial class DrawToolFrm : Form
    {
        private DataTable defectTable;
        private String defectCategory;
        private String _path = "C:/Users/ryuya/Downloads";
        private List<ICogGraphicParentChild> shapeContainer = new List<ICogGraphicParentChild>();

        public delegate void DisplayImageDelegate(Bitmap frameBuffer);

        //Test variables
        CogCompositeShape temp = new CogCompositeShape();
        
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
            ((CogLineSegment)cogDisplay1.InteractiveGraphics[0]).StartX = 100;
            ((CogLineSegment)cogDisplay1.InteractiveGraphics[0]).StartY = 100;
            ((CogLineSegment)cogDisplay1.InteractiveGraphics[0]).Interactive = true;
        }

        private void btnTest2_Click(object sender, EventArgs e)
        {
            Console.WriteLine("cogDisplay1.InteractiveGraphics Count: " + shapeContainer.Count);
        }

        private void btnTest3_Click(object sender, EventArgs e)
        {
            Console.WriteLine("BUTTOM 3");
            CogCompositeShape arrow = new CogCompositeShape();
            arrow.GraphicDOFEnable = CogCompositeShapeDOFConstants.All;
            arrow.CompositionMode = CogCompositeShapeCompositionModeConstants.Uniform;
            arrow.Interactive = true;
            arrow.Color = CogColorConstants.Red;
            arrow.Visible = true;
            
            CogLineSegment cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(500, 500, 50, 0);
            cogLineSegment.SelectedSpaceName = "$";
            
            CogLineSegment cogLineSegment2 = new CogLineSegment();
            cogLineSegment2.SetStartLengthRotation(500, 500, 100, 100);
            cogLineSegment2.SelectedSpaceName = "$";

            arrow.Shapes.Add(cogLineSegment);
            arrow.Shapes.Add(cogLineSegment2);
            //Must call this
            arrow.ParentFromChildTransform = arrow.GetParentFromChildTransform();

            cogDisplay1.InteractiveGraphics.Add(arrow, "arrow", false);
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
            Arrow arrow = new Arrow();
            arrow.SetArrowLength(500, 500, 100, 0.45);
            CogCompositeShape arrowComponents = arrow.GetArrow();

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
            string detail = string.Format("{0}", "Type in your detail");
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
                    //ID 0 = arrow
                    if (((CogCompositeShape)graphics[i]).ID == 0)
                    {
                        //TODO: Cant get the new position of arrow
                        detail = string.Format("({0}, {1})",
                            ((CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[0]).StartX,
                            ((CogLineSegment)((CogCompositeShape)graphics[i]).Shapes[0]).StartY);
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
                    CogCompositeShape graphic = (CogCompositeShape)shapes[i];
                    cogDisplay1.InteractiveGraphics.Add(graphic, "Arrow", false);
                    shapeContainer.Add(graphic);
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

        //To update moving arrows
        private void cogDisplay1_MouseDown(object sender, MouseEventArgs e)
        {
    
        }

        private void cogDisplay1_MouseMove(object sender, MouseEventArgs e)
        {
            
        }  
    }
}
