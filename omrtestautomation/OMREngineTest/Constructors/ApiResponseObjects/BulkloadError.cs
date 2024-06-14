using System;
using System.Collections.Generic;
using System.Text;

namespace Optimizer.Constructors.ApiResponseObjects
{
    public class BulkloadError
    {
        public string file;
        public string table;
        public string error;
        public int lineNumber;
    }
}
