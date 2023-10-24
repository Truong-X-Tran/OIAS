using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CrystalX
{
    public class ProteinImageEntity
    {
        public int ImageId { get; set; }

        public int ExperimentId { get; set; }

        public int AutoScanId { get; set; }

        public int Light { get; set; }

        public string ImageFileName { get; set; }

        public string ImageFilePath { get; set; }

        public int PredictedClass3Id { get; set; }

        public int PredictedClass10Id { get; set; }
        
        public int ActualClassId { get; set; }

        public int NewCrystals { get; set; }

        public int SizeGrowth { get; set; }

        public ProteinImageEntity(int _experimentId, int _autoScanId, int _light, string _filePath)
        {
            this.ExperimentId = _experimentId;
            this.AutoScanId = _autoScanId;
            this.Light = _light;
            this.ImageFilePath = _filePath;
        }

        public ProteinImageEntity(int imageId, string imageFileName, string imageFilePath, int predictedClass3, int predictedClass10, int actualClassId)
        {
            this.ImageId = imageId;
            this.ImageFileName = imageFileName; 
            this.ImageFilePath = imageFilePath;
            this.PredictedClass3Id = PredictedClass3Id;
            this.ActualClassId = actualClassId;
        }

        public ProteinImageEntity()
        {
            // TODO: Complete member initialization
        }
    }

    public class ProteinImageEntityCollection : ObservableCollection<ProteinImageEntity>
    {
    }

    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return new BitmapImage(new Uri((string) value));
            }
            catch
            {
                return new BitmapImage(new Uri(@"/ProtoScope;component/question_mark.jpg", UriKind.Relative));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}