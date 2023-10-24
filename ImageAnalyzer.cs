using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CrystalX
{
    class ImageAnalyzer
    {
        double[] graphFeatures;
        double[] featuresWithoutGraphFeatures;
        double[] allFeatures;

        public ImageAnalyzer()
        {
        }

        private double[] extractFeaturesUsingCsharp(string imagepath)
        {
            ImageProcessor ip = new ImageProcessor(imagepath);
            double[] features = ip.extractFeatures();
            return features;
        }

        private void extractFeaturesUsingMatlab(string imagepath)
        {
            // Create the MATLAB instance 
            //MLApp.MLApp matlab = new MLApp.MLApp();

            string baseDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            //string MatlabFilesDir = baseDir + "\\Matlab\\FeatureExtraction\\";

            // Add the Matlab files directory to Matlab path
            //matlab.Execute(@"cd " + MatlabFilesDir);

            // Define the output 
            object result = null;

            // Call the MATLAB function 
           // matlab.Feval("extractFeaturesWithoutGraphF", 1, out result, imagepath);
            object[] res = result as object[];
            float[,] temp1  = (float[,])res[0];

            featuresWithoutGraphFeatures = new double[temp1.Length];
            for (int i = 0; i < temp1.Length; i++)
                featuresWithoutGraphFeatures[i] = (double) temp1[0,i];

            result = null;
            //matlab.Feval("GetGraphFeatures", 1, out result, imagepath);
            res = result as object[];
            double[,] temp2 = (double[,]) res[0];

            graphFeatures = new double[temp2.Length];
            for (int i = 0; i < temp2.Length; i++)
                graphFeatures[i] = (double) temp2[0, i];

            int allFeaturesLength = featuresWithoutGraphFeatures.Length + graphFeatures.Length;
            allFeatures = new double[allFeaturesLength];

            for (int i = 0; i < featuresWithoutGraphFeatures.Length; i++)
                allFeatures[i] = featuresWithoutGraphFeatures[i];

            for (int i = 0; i < graphFeatures.Length; i++)
                allFeatures[featuresWithoutGraphFeatures.Length + i] = graphFeatures[i];
        }

        public void classifyImageExtendedFeatures(string imagepath, out int predClass3, out int predClass10)
        {
            extractFeaturesUsingMatlab(imagepath);

            WekaClassifier wc = new WekaClassifier();

            int subClass = -1;
            predClass10 = -1;

            int numCategories = 3;
            string modelPath = wc.getModelPath(ClassLevels.FirstLevel, numCategories, ClassificationMethods.RandomForest, FeatureOption.Extended);
            predClass3 = wc.applyClassifier(featuresWithoutGraphFeatures, modelPath, numCategories);

            switch (predClass3)
            {
                case 0: //i.e., non-crystals
                    numCategories = 3; //there are 3 sub-categories of non-crystals
                    modelPath = wc.getModelPath(ClassLevels.NonCrystals, numCategories, ClassificationMethods.RandomForest, FeatureOption.Extended);
                    subClass = wc.applyClassifier(featuresWithoutGraphFeatures, modelPath, numCategories);
                    predClass10 = 1 + subClass; //Classes are numbered 0, 1 and 2, but in 10-class form, we count from 1
                    break;

                case 1: //i.e., likely-leads
                    numCategories = 2; //there are 2 sub-categories of likely-leads
                    modelPath = wc.getModelPath(ClassLevels.LikelyLeads, numCategories, ClassificationMethods.RandomForest, FeatureOption.Extended);
                    subClass = wc.applyClassifier(featuresWithoutGraphFeatures, modelPath, numCategories);
                    predClass10 = 4 + subClass; //Likely-leads are numbered 4 or 5 in the 10-class category. subClass can have values 0 or 1.
                    break;

                case 2: //Classify into 5 crystal sub-categories
                    numCategories = 5;
                    modelPath = wc.getModelPath(ClassLevels.Crystals, numCategories, ClassificationMethods.RandomForest, FeatureOption.Extended);
                    subClass = wc.applyClassifier(allFeatures, modelPath, numCategories);
                    predClass10 = 6 + subClass; //Crystals are numbered from 6 to 10 in the 10-class category.
                    break;

                default:
                    predClass10 = -1;
                    break;
            }
        }

        public void classifyImageLimitedFeatures(string imagepath, out int predClass3, out int predClass10)
        {
            double[] features = extractFeaturesUsingCsharp(imagepath);

            WekaClassifier wc = new WekaClassifier();

            int subClass = -1;
            predClass10 = -1;

            int numCategories = 3;
            string modelPath = wc.getModelPath(ClassLevels.FirstLevel, numCategories, ClassificationMethods.RandomForest, FeatureOption.Limited);
            predClass3 = wc.applyClassifier(features, modelPath, numCategories);

            switch (predClass3)
            {
                case 0: //i.e., non-crystals
                    numCategories = 3; //there are 3 sub-categories of non-crystals
                    modelPath = wc.getModelPath(ClassLevels.NonCrystals, numCategories, ClassificationMethods.RandomForest, FeatureOption.Limited);
                    subClass = wc.applyClassifier(features, modelPath, numCategories);
                    predClass10 = 1 + subClass; //Classes are numbered 0, 1 and 2, but in 10-class form, we count from 1
                    break;

                case 1: //i.e., likely-leads
                    numCategories = 2; //there are 2 sub-categories of likely-leads
                    modelPath = wc.getModelPath(ClassLevels.LikelyLeads, numCategories, ClassificationMethods.RandomForest, FeatureOption.Limited);
                    subClass = wc.applyClassifier(features, modelPath, numCategories);
                    predClass10 = 4 + subClass; //Likely-leads are numbered 4 or 5 in the 10-class category. subClass can have values 0 or 1.
                    break;

                case 2: //Classify into 5 crystal sub-categories
                    numCategories = 5;
                    modelPath = wc.getModelPath(ClassLevels.Crystals, numCategories, ClassificationMethods.RandomForest, FeatureOption.Limited);
                    subClass = wc.applyClassifier(features, modelPath, numCategories);
                    predClass10 = 6 + subClass; //Crystals are numbered from 6 to 10 in the 10-class category.
                    break;

                default:
                    predClass10 = -1;
                    break;
            }
        }

        //Classify with all features using Matlab functions
        public void temporalAnalysisImgPair(string imagepath1, string imagepath2, out int newCrystals, out int sizeGrowth)
        {
            // Create the MATLAB instance 
            //MLApp.MLApp matlab = new MLApp.MLApp();

            string baseDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            //string MatlabFilesDir = baseDir + "\\Matlab\\Temporal\\";

            // Add the Matlab files directory to Matlab path
            //matlab.Execute(@"cd " + MatlabFilesDir);

            // Define the output 
            object result = null;

            //matlab.Feval("fnTemporalChangePair", 2, out result, imagepath1, imagepath2);

            object[] res = result as object[];

            newCrystals = Convert.ToInt16(res[0]);
            sizeGrowth = Convert.ToInt16(res[1]);
        }
    }
}