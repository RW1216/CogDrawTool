using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.Implementation.Internal;
using static System.Windows.Forms.AxHost;

namespace CogDrawTool
{
    internal class ShapeDesigner 
    {
        //ArrowComponent.Shape[0] = middle line
        //ArrowComponent.Shape[1] = left line
        //ArrowComponent.Shape[2] = right line
        CogCompositeShape shapeComponents;

        private const double ARROW_ANGLE = 0.45;

        public ShapeDesigner()
        {
            shapeComponents = new CogCompositeShape();
        }

        public void SetArrowLength(double startX, double startY, double length, double rotation)
        {
            //Long line
            CogLineSegment cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length, rotation);
            cogLineSegment.SelectedSpaceName = "$";
            shapeComponents.Shapes.Add(cogLineSegment);
            //left line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length / 5, rotation - ARROW_ANGLE);
            cogLineSegment.SelectedSpaceName = "$";
            shapeComponents.Shapes.Add(cogLineSegment);
            //right line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length / 5, rotation + ARROW_ANGLE);
            cogLineSegment.SelectedSpaceName = "$";
            shapeComponents.Shapes.Add(cogLineSegment);
        }

        public void SetDistanceLength(double startX, double startY, double endX, double endY)
        {
            //Long Line
            CogLineSegment cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartEnd(startX, startY, endX, endY);
            cogLineSegment.SelectedSpaceName = "$";
            shapeComponents.Shapes.Add(cogLineSegment);
            //left Line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartEnd(startX , startY - 10, startX, startY + 10);
            cogLineSegment.SelectedSpaceName = "$";
            shapeComponents.Shapes.Add(cogLineSegment);
            //right Line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartEnd(endX, endY - 10, endX, endY + 10);
            cogLineSegment.SelectedSpaceName = "$";
            shapeComponents.Shapes.Add(cogLineSegment);
        }

        public CogCompositeShape GetShape()
        {
            return shapeComponents;
        }

        public void ClearShape()
        {
            shapeComponents = new CogCompositeShape();
        }

        public double GetStartX()
        {
            return ((CogLineSegment)shapeComponents.Shapes[0]).StartX;
        }

        public double GetStartY()
        {
            return ((CogLineSegment)shapeComponents.Shapes[0]).StartY;
        }
    }
}
