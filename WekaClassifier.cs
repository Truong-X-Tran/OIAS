using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Data;
using weka.classifiers;
using weka.core;
using System.Reflection;
using System.IO;

namespace CrystalX
{
    enum ClassLevels { FirstLevel, NonCrystals, LikelyLeads, Crystals };
    
    enum ClassificationMethods{ RandomForest, J48, NaiveBayesian};
    
    enum FeatureOption { Limited, Extended };

    class WekaClassifier
    {
        static string baseDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
        string TrainingModelsDir = baseDir + "\\TrainingModels\\";

        public WekaClassifier()
        {
        }

        private FastVector getAttributesFastVector(int dim, int numCategories)
        {
            FastVector fvWekaAttributes = new FastVector(dim + 1);

            for (int i = 0; i < dim; i++)
                fvWekaAttributes.addElement(new weka.core.Attribute("attribute" + i));

            FastVector fvClassVal = new FastVector(numCategories);
            for (int i = 0; i < numCategories; i++)
                fvClassVal.addElement(i.ToString());

            weka.core.Attribute ClassAttribute = new weka.core.Attribute("class", fvClassVal);

            fvWekaAttributes.addElement(ClassAttribute);
            return fvWekaAttributes;
        }

        private Instance getAllFeaturesArray(double [] features)
        {
            int dim = features.Length;
            Instance myInstance = new Instance(dim + 1);
            for (int i = 0; i < dim; i++)
                myInstance.setValue(i, features[i]);

            myInstance.setMissing(dim); //set class attribute to be missing
            return myInstance;
        }

        //Return the training model path based on the classification method, numcategories and classification level
        public string getModelPath(ClassLevels lvl, int numCategories, ClassificationMethods classifyMethod, FeatureOption featureOption)
        {
            string modelFileName = "";

            switch (lvl)
            {
                case ClassLevels.FirstLevel:
                    modelFileName = "firstlevel" + numCategories + "class";
                    break;

                case ClassLevels.NonCrystals:
                    modelFileName = "noncrystals" + numCategories + "class";
                    break;

                case ClassLevels.LikelyLeads:
                    modelFileName = "likelyleads" + numCategories + "class";
                    break;

                case ClassLevels.Crystals:
                    modelFileName = "crystals" + numCategories + "class";
                    break;
            }

            switch (classifyMethod)
            {
                case ClassificationMethods.J48:
                    modelFileName += "_j48";
                    break;

                case ClassificationMethods.RandomForest:
                    modelFileName += "_rf";
                    break;

                case ClassificationMethods.NaiveBayesian:
                    modelFileName += "_nb";
                    break;
            }

            switch (featureOption)
            {
                case FeatureOption.Limited:
                    modelFileName += "_lim.model";
                    break;

                case FeatureOption.Extended:
                    modelFileName += "_ext.model";
                    break;
            }

            string trainingModelPath = TrainingModelsDir + modelFileName;

            return trainingModelPath;
        }

        //Apply classifier for the featurevector fv into numCategories using model file modelPath
        public int applyClassifier(double[] features, string modelPath, int numCategories)
        {
            //Classify using all features
            //First, get the dimension count for all features, create weka attributes and make test instances structure
            int numDimensions = features.Length;
            FastVector fvWekaAttributesAll = getAttributesFastVector(numDimensions, numCategories);
            Instances testSet = new Instances("testSet", fvWekaAttributesAll, 1);
            testSet.setClassIndex(numDimensions);

            //Classify using all features
            Instance myInstance_all = getAllFeaturesArray(features);
            myInstance_all.setDataset(testSet);

            Classifier cls = (Classifier) weka.core.SerializationHelper.read(modelPath);
            int predClass = Convert.ToInt16(cls.classifyInstance(myInstance_all));
            
            return predClass;
        }
    }
}