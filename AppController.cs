using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;

namespace CrystalX
{
    class AppController
    {
        public string ImageRepositoryPath;

        public AppController() 
        {
        }

        public ExperimentCollection loadExperiments()
        {
            var q = (from v in MainWindow.db.Experiments select v).OrderByDescending(g => g.DateCreated);

            ExperimentCollection expList = new ExperimentCollection();

            foreach (Experiment exp in q)
            {
                var query = (from t in MainWindow.db.AutoScanRecords
                         join x in MainWindow.db.Experiments
                         on t.ExperimentID equals x.ExperimentID
                         where t.ExperimentID == exp.ExperimentID && t.Visible_YN == 1
                         select new { t.id, t.ScanID, t.light, t.Subfolder, x.ExperimentID, 
                             x.ExperimentName, x.DirectoryPath, t.ProcessedCategory, t.FeaturesExtracted_YN, t.DateCreated
                            }
                         );

                SubFolderCollection folderList = new SubFolderCollection();

                foreach (var sf in query)
                {
                    //string subFolderDir = getLookUpPath((int)sf.ExperimentID, sf.ScanID, sf.light);

                    //if (System.IO.Directory.Exists(subFolderDir))
                    //{
                        bool isFeaturesExtracted = (sf.FeaturesExtracted_YN == 1) ? true : false;
                        int processedCategory = sf.ProcessedCategory;

                        SubFolderEntity sb = new SubFolderEntity((int)sf.id, (int)sf.ExperimentID, sf.ExperimentName, sf.DirectoryPath, sf.ScanID, sf.Subfolder,
                            sf.light, sf.DateCreated, isFeaturesExtracted, processedCategory);

                        folderList.Add(sb);
                    //}

                    //else //i.e. directory does not exist, hence update its visibility in the database (make it invisible)
                    //{
                    //    setSubFolderInvisible((int)sf.ExperimentID, sf.ScanID, sf.light);
                    //}
                }
                
                ExperimentEntity e = new ExperimentEntity((int)exp.ExperimentID, exp.ExperimentName, exp.DirectoryPath, folderList);
                expList.Add(e);
            }

            return expList;
        }

        private void setSubFolderInvisible(int _exptId, int _scanId, int _light)
        {
            var expt = (from q in MainWindow.db.AutoScanRecords
                        where q.ExperimentID == _exptId && q.ScanID == _scanId && q.light == _light
                        select q).First();

            expt.Visible_YN = 0;

            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public SubFolderCollection loadAutoScanFolders(ExperimentEntity item)
        {
            return item.SubFolderList;
        }

        public string getLookUpPath(int _experimentId, int _subFolderId, int _light)
        {
            try
            {
                // TTran
                /*
                var t = (from x in MainWindow.db.DirectorySettings
                         select x.directoryPath).First();
                */

                //Check the AutoScanRecord table to determine the rootfolder and subfolder
                var q = (from x in MainWindow.db.AutoScanRecords
                         join e in MainWindow.db.Experiments
                         on x.ExperimentID equals e.ExperimentID
                         where x.ExperimentID == _experimentId && x.ScanID == _subFolderId && x.light == _light
                         select new { e.ExperimentName, e.DirectoryPath, x.Subfolder }
                        ).First();

                if (q.DirectoryPath != null)
                {
                    string lookupDir = q.DirectoryPath + "\\" + q.ExperimentName + "\\" + q.Subfolder;
                    return lookupDir;
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        public ProteinImageEntityCollection getProcessedSubFolderImagesAll(int _experimentId, int _subFolderId, int _light)
        {
            string imagesDir = getLookUpPath(_experimentId, _subFolderId, _light);

            var t = (from v in MainWindow.db.Images
                where v.experimentId == _experimentId && v.scanId == _subFolderId && v.light == _light
                select new { v.id, v.filename, v.predClass3 }
            ).OrderBy(g => g.filename);

            ProteinImageEntityCollection imgList = new ProteinImageEntityCollection();

            foreach (var img in t)
            {
                ProteinImageEntity pimg = new ProteinImageEntity();
                pimg.ImageId = img.id;
                pimg.ImageFileName = img.filename;
                pimg.ImageFilePath = imagesDir + "\\" + img.filename;
                pimg.PredictedClass3Id = (int)img.predClass3;
                imgList.Add(pimg);
            }
            return imgList;
        }

        public string getPriorScanFolderPath(int _experimentId, int _subFolderId, int _light)
        {
            return getLookUpPath(_experimentId, _subFolderId-1, _light);
        }

        public ProteinImageEntityCollection getUnprocessedSubFolderImagesAll(int _experimentId, int _subFolderId, int _light)
        {
            try
            {
                string imagesDir = getLookUpPath(_experimentId, _subFolderId, _light);

                string[] imageFiles = System.IO.Directory.GetFiles(imagesDir, "*.jpg");

                ProteinImageEntityCollection imgList = new ProteinImageEntityCollection();

                foreach (string imgPath in imageFiles)
                {
                    ProteinImageEntity pimg = new ProteinImageEntity();
                    pimg.ExperimentId = _experimentId;
                    pimg.AutoScanId = _subFolderId;
                    pimg.Light = _light;
                    pimg.ImageFileName = System.IO.Path.GetFileName(imgPath);
                    pimg.ImageFilePath = imgPath;
                    pimg.PredictedClass3Id = -1;
                    imgList.Add(pimg);
                }
                return imgList;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public ProteinImageEntityCollection loadAutoScanFolderImagesAll(int _experimentId, int _subFolderId, int _light)
        {
            return getUnprocessedSubFolderImagesAll(_experimentId, _subFolderId, _light);
        }

        public ProteinImageEntityCollection loadAutoScanFolderImagesCrystals(int _experimentId, int _subFolderId, int _light)
        {
            var t = (from v in MainWindow.db.Images
                     join x in MainWindow.db.ImageCategories
                     on v.predClass3 equals x.classId
                     where v.experimentId == _experimentId && v.scanId == _subFolderId && v.light == _light && x.classId==2
                     select new { v.id, v.filename, v.predClass3, x.classDescription }
                    ).OrderBy(g => g.filename);

            ProteinImageEntityCollection imgList = new ProteinImageEntityCollection();
            string lookupDir = getLookUpPath(_experimentId, _subFolderId, _light);

            foreach (var img in t)
            {
                ProteinImageEntity pimg = new ProteinImageEntity();
                pimg.ImageId = img.id;
                pimg.ImageFileName = img.filename;
                pimg.ImageFilePath = lookupDir + "\\" + img.filename;
                pimg.PredictedClass3Id = (int)img.predClass3;
                imgList.Add(pimg);
            }
            return imgList;
        }

        public ProteinImageEntityCollection loadAutoScanFolderImagesNoncrystals(int _experimentId, int _subFolderId, int _light)
        {
            var t = (from v in MainWindow.db.Images
                     join x in MainWindow.db.ImageCategories
                     on v.predClass3 equals x.classId
                     where v.experimentId == _experimentId && v.scanId == _subFolderId && v.light == _light && x.classId == 0
                     select new { v.id, v.filename, v.predClass3, x.classDescription }
                    ).OrderBy(g => g.filename);

            ProteinImageEntityCollection imgList = new ProteinImageEntityCollection();
            string lookupDir = getLookUpPath(_experimentId, _subFolderId, _light);

            foreach (var img in t)
            {
                ProteinImageEntity pimg = new ProteinImageEntity();
                pimg.ImageId = img.id;
                pimg.ImageFileName = img.filename;
                pimg.ImageFilePath = lookupDir + "\\" + img.filename;
                pimg.PredictedClass3Id = (int)img.predClass3;
                imgList.Add(pimg);
            }
            return imgList;
        }

        public ProteinImageEntityCollection loadAutoScanFolderImagesLikelyLeads(int _experimentId, int _subFolderId, int _light)
        {
            var t = (from v in MainWindow.db.Images
                     join x in MainWindow.db.ImageCategories
                     on v.predClass3 equals x.classId
                     where v.experimentId == _experimentId && v.scanId == _subFolderId && v.light == _light && x.classId == 1
                     select new { v.id, v.filename, v.predClass3, x.classDescription }
                    ).OrderBy(g => g.filename);

            ProteinImageEntityCollection imgList = new ProteinImageEntityCollection();
            string lookupDir = getLookUpPath(_experimentId, _subFolderId, _light);

            foreach (var img in t)
            {
                ProteinImageEntity pimg = new ProteinImageEntity();
                pimg.ImageId = img.id;
                pimg.ImageFileName = img.filename;
                pimg.ImageFilePath = lookupDir + "\\" + img.filename;
                pimg.PredictedClass3Id = (int)img.predClass3;
                imgList.Add(pimg);
            }
            return imgList;
        }

        public ClassCategoryCollection loadClassCategories(int _numCategories)
        {
            var q = (from v in MainWindow.db.ImageCategories
                     where v.numCategories == _numCategories
                     select v
                     ).OrderBy(g => g.classId);

            ClassCategoryCollection classList = new ClassCategoryCollection();

            foreach (var cls in q)
            {
                ClassCategoryEntity c = new ClassCategoryEntity();
                c.classId = (int) cls.classId;
                c.className = cls.classDescription;
                c.classDescription = cls.classDescription;
            }
            return classList;
        }

        public void setExperimentProcessed(int _exptId, int _scanId, int _light, int processedCategory)
        {
            var expt = (from q in MainWindow.db.AutoScanRecords 
                        where q.ExperimentID == _exptId && q.ScanID == _scanId && q.light == _light
                        select q).First();

            expt.ProcessedCategory = processedCategory;
            expt.FeaturesExtracted_YN = 1;

            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public int getMaxAutoScanId()
        {
            try
            {
                int maxScanId = (from q in MainWindow.db.AutoScanRecords
                                 select q.ScanID).Max();
                return maxScanId;
            }
            catch (Exception)
            {
                return 0; //If there aren't any rows, catch exception, return 0
            }
        }

        public void updateImagesClassifyScores(int _experimentId, int _autoScanId, int _lightId, string _fileName, int predClass3, int predClass10)
        {
            var res =
                    (from q in MainWindow.db.Images
                     where q.experimentId == _experimentId && q.scanId == _autoScanId && q.light == _lightId && q.filename == _fileName
                     select q);

            foreach (Image img in res)
            {
                img.predClass3 = predClass3;
                img.predClass10 = predClass10;
                img.finalClass = predClass10;
            }

            // Submit the changes to the database. 
            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Provide for exceptions.
            }
        }

        public void updateImagesTemporalScores(int _experimentId, int _autoScanId, int _lightId, string _fileName, int newCrystals, int sizeGrowth)
        {
            var res =
                    (from q in MainWindow.db.Images
                     where q.experimentId == _experimentId && q.scanId == _autoScanId && q.light == _lightId && q.filename == _fileName
                     select q);

            foreach (Image img in res)
            {
                img.newCrystals = newCrystals;
                img.sizeGrowth = sizeGrowth;
            }

            // Submit the changes to the database. 
            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Provide for exceptions.
            }
        }

        public void updateImagesBothScores(int _experimentId, int _autoScanId, int _lightId, string _fileName, int predClass3, int predClass10, int newCrystals, int sizeGrowth)
        {
            var res =
                    (from q in MainWindow.db.Images
                     where q.experimentId == _experimentId && q.scanId == _autoScanId && q.light == _lightId && q.filename == _fileName
                     select q);

            foreach (Image img in res)
            {
                img.predClass3 = predClass3;
                img.predClass10 = predClass10;
                img.finalClass = predClass10;
                img.newCrystals = newCrystals;
                img.sizeGrowth = sizeGrowth;
            }

            // Submit the changes to the database. 
            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Provide for exceptions.
            }
        }

        private bool existsImageRecord(int _experimentId, int _autoScanId, int _lightId, string _fileName)
        {
            var res =
                    (from q in MainWindow.db.Images
                     where q.experimentId == _experimentId && q.scanId == _autoScanId && q.light == _lightId && q.filename == _fileName
                     select q);
            if (res.Any())
                return true;
            else
                return false;
        }

        private void deleteImageRecord(int _experimentId, int _autoScanId, int _lightId, string _fileName)
        {
            var res =
                    (from q in MainWindow.db.Images
                     where q.experimentId == _experimentId && q.scanId == _autoScanId && q.light == _lightId && q.filename == _fileName
                     select q);

            foreach (var img in res)
            {
                MainWindow.db.Images.DeleteOnSubmit(img);
            }

            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Provide for exceptions.
            }
        }

        public void insertImageRecords(ProteinImageEntityCollection _imgList)
        {
            foreach (ProteinImageEntity x in _imgList)
            {
                Image _image = new Image();
                _image.experimentId = x.ExperimentId;
                _image.scanId = x.AutoScanId;
                _image.light = x.Light;
                _image.filename = x.ImageFileName;
                _image.predClass3 = x.PredictedClass3Id;
                _image.predClass10 = x.PredictedClass10Id;

                if (existsImageRecord(x.ExperimentId, x.AutoScanId, x.Light, _image.filename))
                    deleteImageRecord(x.ExperimentId, x.AutoScanId, x.Light, _image.filename);

                MainWindow.db.Images.InsertOnSubmit(_image);
            }
            
            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void insertAutoScanRecord(int ExptId, int ScanId, int Light, string subFolder, int imgCount, bool featuresExtracted, bool imgClassified)
        {
            AutoScanRecord rec = new AutoScanRecord();
            rec.ExperimentID = ExptId;
            rec.ScanID = ScanId;
            rec.light = Light;
            rec.DateCreated = DateTime.Now;
            rec.Subfolder = subFolder;
            rec.Visible_YN = 1;
            rec.FeaturesExtracted_YN = featuresExtracted ? 1 : 0;
            rec.ProcessedCategory = imgClassified ? 1 : 0;
            rec.ExpertScored_YN = 0; //Setting as expert score not available

            MainWindow.db.AutoScanRecords.InsertOnSubmit(rec);

            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void insertImage(int ExptId, int ScanId, int Light, string filename)
        {
            CrystalX.Image rec = new CrystalX.Image();

            rec.experimentId = ExptId;
            rec.scanId = ScanId;
            rec.light = Light;
            rec.filename = filename;

            MainWindow.db.Images.InsertOnSubmit(rec);

            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void updateImageCategories(ProteinImageEntityCollection imgList)
        {
            foreach (ProteinImageEntity img in imgList)
            {
                var res =
                    (from q in MainWindow.db.Images
                     where q.id == img.ImageId
                     select q).First();

                res.predClass3 = img.PredictedClass3Id;
            }
            MainWindow.db.SubmitChanges();
        }

        public void insertAutoScanRecord(int _exptId, int _scanId, int _light, string _subFolder)
        {
            AutoScanRecord rec = new AutoScanRecord();
            rec.ExperimentID = _exptId;
            rec.ScanID = _scanId;
            rec.light = _light;
            rec.DateCreated = DateTime.Now;
            rec.Subfolder = _subFolder;
            rec.Visible_YN = 1;
            rec.FeaturesExtracted_YN = 0;
            rec.ProcessedCategory = 0;
            rec.ExpertScored_YN = 0; //Setting as expert score not available

            MainWindow.db.AutoScanRecords.InsertOnSubmit(rec);

            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void setAutoScanRecordProcessed(int _experimentId, int _autoScanId, int _light)
        {
                var res =
                    (from q in MainWindow.db.AutoScanRecords
                    where q.ExperimentID == _experimentId && q.ScanID == _autoScanId && q.light == _light
                    select q).First();

                res.FeaturesExtracted_YN = 1;
                res.ProcessedCategory = 1;

                try
                {
                    MainWindow.db.SubmitChanges();
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
        }

        //Purpose: Set the selected subfolders to be non-visible.
        //The records will be set non-visible in the database. The records aren't deleted from the database.
        public void deleteExperiments(SubFolderCollection subFolderList)
        {
            foreach (var x in subFolderList)
            {
                var res =
                        (from q in MainWindow.db.AutoScanRecords
                         where q.ExperimentID == x.ExperimentId && q.ScanID == x.SubFolderId && q.light == x.LightId
                         select q).First();

                res.Visible_YN = 0;
            }

            try
            {
                MainWindow.db.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // Provide for exceptions.
            }
        }
    }
}