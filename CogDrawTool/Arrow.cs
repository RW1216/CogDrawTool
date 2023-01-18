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
    internal class Arrow 
    {
        //ArrowComponent.Shape[0] = middle line
        //ArrowComponent.Shape[1] = left line
        //ArrowComponent.Shape[2] = right line
        CogCompositeShape ArrowComponent;

        private const double ANGLE = 0.45;

        public Arrow()
        {
            ArrowComponent = new CogCompositeShape();
        }

        public void SetArrowLength(double startX, double startY, double length, double rotation)
        {
            //Long line
            CogLineSegment cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length, rotation);
            cogLineSegment.SelectedSpaceName = "$";
            ArrowComponent.Shapes.Add(cogLineSegment);
            //left line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length / 5, rotation - ANGLE);
            cogLineSegment.SelectedSpaceName = "$";
            ArrowComponent.Shapes.Add(cogLineSegment);
            //right line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length / 5, rotation + ANGLE);
            cogLineSegment.SelectedSpaceName = "$";
            ArrowComponent.Shapes.Add(cogLineSegment);
        }

        public CogCompositeShape GetArrow()
        {
            return ArrowComponent;
        }

        public void UpdateLineSegments(double startX, double startY, double length, double rotation, int linePosition)
        {
            /*//if middle line moved
            if (linePosition == 0)
            {
                //Update left line
                _lineSegments[1].SetStartLengthRotation(startX, startY, length / 5, rotation - ANGLE);
                //Update right line
                _lineSegments[2].SetStartLengthRotation(startX, startY, length / 5, rotation + ANGLE);
            }
            //if left line moved
            else if (linePosition == 1)
            {
                //Update middle line
                _lineSegments[0].SetStartLengthRotation(startX, startY, length * 5, rotation + ANGLE);
                //Update right line
                _lineSegments[2].SetStartLengthRotation(startX, startY, length, rotation + ANGLE*2);
            }
            else if (linePosition == 2)
            {
                //Update left line
                _lineSegments[1].SetStartLengthRotation(startX, startY, length, rotation - ANGLE*2);
                //Update middle line
                _lineSegments[0].SetStartLengthRotation(startX, startY, length * 5, rotation - ANGLE);
            }*/
        }

        public double GetStartX()
        {
            return ((CogLineSegment)ArrowComponent.Shapes[0]).StartX;
        }

        public double GetStartY()
        {
            return ((CogLineSegment)ArrowComponent.Shapes[0]).StartY;
        }
    }
}
