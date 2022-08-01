using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Models
{
    public class Path
    {
        public enum Type
        {
            Css,
            XPath
        }
        public Type type;
        public string path;
        public Path(string path)
        {
            type = path.StartsWith("/") ? Type.XPath : Type.Css;
            this.path = path;
        }
    }
}
