using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace CrystalX
{
    public class SubFolderEntity
    {
        public int Id { get; set; }
        public int ExperimentId { get; set; }
        public string ExperimentName { get; set; }
        public string DirectoryPath { get; set; }
        public int SubFolderId { get; set; }
        public string SubFolder { get; set; }
        public int LightId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ImagesCount { get; set; }
        public int ProcessedCategory { get; set; }

        //Properties for view models
        public bool IsChecked { get; set; }  //This property is used to bind if the user selects this subfolder
        public SolidColorBrush ProcessedColor { get; set; } //This property is used to denote different colors for processed and unprocessed subfolders

        public SubFolderEntity(int _id, int _experimentId, string _experimentName, string _directoryPath, int _subFolderId, string _subFolder, 
            int _lightId, DateTime _createdDate, bool _isFeaturesExtracted, int _processedCategory)
        {
            Id = _id;
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
            SubFolderId = _subFolderId;
            SubFolder = _subFolder;
            LightId = _lightId;
            CreatedDate = _createdDate;
            ProcessedCategory = _processedCategory;

            switch (ProcessedCategory)
            {
                case 0: // Indicates None of classification and temporal analysis are done
                    ProcessedColor = new SolidColorBrush(Colors.Red);
                    break;

                case 1: // Only classification is done
                    ProcessedColor = new SolidColorBrush(Colors.Blue);
                    break;

                case 2: //Only temporal analysis is done
                    ProcessedColor = new SolidColorBrush(Colors.Black);
                    break;
                case 3: //Both temporal and classification analysis is done
                    ProcessedColor = new SolidColorBrush(Colors.Green);
                    break;

                default:
                    ProcessedColor = new SolidColorBrush(Colors.Black);
                    break;
            }
            IsChecked = false;  //Initially, uncheck the selection of the folder
        }
    }

    public class SubFolderCollection : List<SubFolderEntity>
    {
    }
}