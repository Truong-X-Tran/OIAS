using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrystalX
{
    public class ExperimentEntity
    {
        public int ExperimentId { get; set; }
        public string ExperimentName { get; set; }
        public string DirectoryPath { get; set; }
        public SubFolderCollection SubFolderList { get; set; }
        
        //Properties for View Model only
        public bool IsChecked { get; set; } //this property is used to bind the user selection of this experiment for processing

        public ExperimentEntity(int _experimentId, string _experimentName, string _directoryPath, SubFolderCollection _subFolderList)
        {
            ExperimentId = _experimentId;
            ExperimentName = _experimentName;
            if (_directoryPath != null)
            {
                DirectoryPath = _directoryPath;
            }
            else
            {
                DirectoryPath = string.Empty;
            }
            SubFolderList = _subFolderList;
        }
    }

    public class ExperimentCollection : List<ExperimentEntity>
    {
    }
}