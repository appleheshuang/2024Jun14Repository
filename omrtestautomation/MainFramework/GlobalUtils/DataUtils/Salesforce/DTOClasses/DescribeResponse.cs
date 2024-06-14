using System.Collections.Generic;
using System.Collections;

namespace RealignmentEngine.Salesforce.DTOClasses
{
    public class DescribeResponse
    {
        public List<Field> Fields { get; set; }

        public class Field
        {
            public string Name { get; set; }
        }
    }
}