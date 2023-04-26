using Spindler.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Services
{
    public interface IWebService
    {
        public Task<IResult<string>> GetHtmlFromUrl(string url);
    }
}
