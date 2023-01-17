using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cognex.VisionPro;

namespace CogDrawTool.Shapes
{
    [Serializable]
    internal class Rectangle : CogRectangleAffine
    {
        public int Index;

        public Rectangle() { }

    }
}
