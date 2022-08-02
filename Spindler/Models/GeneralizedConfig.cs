using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Models
{
    public class GeneralizedConfig : Config
    {
        /// <summary>
        /// The path to check in order to see if this configuration is valid
        /// </summary>
        public string MatchPath { get; set; }
    }
}
