using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows;
using System.Xml.Serialization;
using Cognex.VisionPro;

namespace CogDrawTool
{
    internal class Shape
    {
    }

    /*public abstract class LeShape : ICogShape
    {
        private bool showBorder = true;
        public bool ShowBorder
        {
            get { return showBorder; }
            set
            {
                showBorder = value;
                LeCanvas.self.Canvas.Invalidate();
            }
        }
        private LeColor borderColor = new LeColor(Color.Black);
        public LeColor LeBorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
                LeCanvas.self.Canvas.Invalidate();
            }
        }

        [XmlIgnore]
        public Color BorderColor
        {
            get { return LeBorderColor.ToColor(); }
            set { LeBorderColor = new LeColor(value); }

        }
        private int borderWidth = 1;
        public int BorderWidth
        {
            get { return borderWidth; }
            set
            {
                borderWidth = value;
                LeCanvas.self.Canvas.Invalidate();
            }
        }

        private Rectangle bounds;
        [XmlIgnore]
        public Rectangle Boundary
        {
            set
            {
                bounds = value;
                Rect = new LeRect(value);
            }
            get { return bounds; }
        }

        public LeShape()
        {
            path = new GraphicsPath();
            objectsInPath = new ArrayList();
        }
    }*/
}
