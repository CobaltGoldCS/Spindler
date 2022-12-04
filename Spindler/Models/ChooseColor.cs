using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Models
{
    public class ChooseColor
    {
        public Color color { get; set; }
        public ChooseColor(Color color) 
        {
            this.color = color;
        }
    }
}
