using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Services
{
    public interface IWebService
    {
        public Task<Utilities.Result<string, string>> GetHtmlFromUrl(string url);
    }
}
