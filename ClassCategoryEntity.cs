using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrystalX
{
    public class ClassCategoryEntity
    {
        public int classId { get; set; }
        public string className { get; set; }
        public string classDescription { get; set; }
    }

    public class ClassCategoryCollection : List<ClassCategoryEntity>
    {
    }
}
