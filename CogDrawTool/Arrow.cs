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
        //_lineSegments[0] = middle line
        //_lineSegments[1] = left line
        //_lineSegments[2] = right line
        private CogLineSegment[] _lineSegments;
        private int _index;

        CogLineSegment cogLineSegment;

        private const double ANGLE = 0.45;

        //
        // Summary:
        //     Constructs a new instance of this class.
        [CogDocSummary("Constructs a new instance of this class.")]
        public Arrow()
        {
            _lineSegments = new CogLineSegment[3];
        }

        public void SetArrowLength(double startX, double startY, double length, double rotation)
        {
            //Long line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length, rotation);
            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.BothPoints;
            cogLineSegment.Interactive = true;
            _lineSegments[0] = cogLineSegment;
            //left line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length / 5, rotation - ANGLE);
            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
            cogLineSegment.Interactive = true;
            _lineSegments[1] = cogLineSegment;
            //right line
            cogLineSegment = new CogLineSegment();
            cogLineSegment.SetStartLengthRotation(startX, startY, length / 5, rotation + ANGLE);
            cogLineSegment.GraphicDOFEnable = CogLineSegmentDOFConstants.None;
            cogLineSegment.Interactive = true;
            _lineSegments[2] = cogLineSegment;

            //ArrowShape.Shapes.Add()
        }

        CogCompositeShape ArrowShape;

        public new CogCompositeShape GetArrow()
        {
            return ArrowShape;
        }

        //TODO: Is this the correct way of method hiding?
        public new void SetStartLengthRotation(double startX, double startY, double length, double rotation)
        { }

        public void AddIntoInteractiveGraphics(CogDisplay display, string groupName, bool checkForDuplicates, int graphicIndex)
        {
            _index = graphicIndex;

            foreach (CogLineSegment line in _lineSegments)
            {
                line.TipText = string.Format("Defect No: {0}", graphicIndex);
                display.InteractiveGraphics.Add(line, groupName, checkForDuplicates);
            }
        }

        private void UpdateDisplay(CogDisplay display)
        {

        }

        public void UpdateLineSegments(double startX, double startY, double length, double rotation, int linePosition)
        {
            //if middle line moved
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
            }
        }

        public int GetLinePosition(CogLineSegment searchLine)
        {
            for (int i = 0; i < _lineSegments.Length; i++)
            {
                if (_lineSegments[i].Equals(searchLine))
                {
                    //Console.WriteLine("SAME");
                    return i;
                }
                else
                {
                    //Console.WriteLine("DIFFERENT");
                }
            }
            return -1;
        }

        public double GetStartX()
        {
            return _lineSegments[0].StartX;
        }

        public double GetStartY()
        {
            return _lineSegments[0].StartY;
        }

        public int GetIndex()
        {
            return _index;
        }

    }
}
