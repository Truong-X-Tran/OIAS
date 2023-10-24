using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace CrystalX
{
    class HistogramFeaturesExtractor
    {
        int[] histogram;
        double std;
        double mean;
        int totalcount;
        Bitmap bmpGreen;

        //Initialize the green color channel image using the original image
        //Histogram features are extracted for the green color channel
        public HistogramFeaturesExtractor(Bitmap bmp)
        {
            bmpGreen = greenColorChannel(bmp);
        }

        //Obtain the Green channel only
        public Bitmap greenColorChannel(Bitmap img)
        {
            Bitmap bmpout = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            BitmapData bmpdata = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmpoutdata = bmpout.LockBits(new Rectangle(0, 0, bmpout.Width, bmpout.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* inptr = (byte*)bmpdata.Scan0;
                byte* outptr = (byte*)bmpoutdata.Scan0;

                for (int i = 0; i < bmpdata.Height; i++)
                {
                    for (int j = 0; j < bmpdata.Width; j++)
                    {
                        outptr[0] = inptr[1];
                        outptr[1] = inptr[1];
                        outptr[2] = inptr[1];
                        outptr[3] = inptr[3];

                        inptr += 4; outptr += 4;
                    }
                }
            }

            img.UnlockBits(bmpdata);
            bmpout.UnlockBits(bmpoutdata);
            return bmpout;
        }

        // Extract all Histogram Features ( Mean, Std. Deviaion, Kurtosis, Skewness, Autocorelation, Power, Entropy )
        public double[] getHistogramFeatures()
        {
            double[] histFeature = new double[17];
            calculateHistogram();

            histFeature[0] = getMean();
            histFeature[1] = standardDeviation();
            histFeature[2] = skewness();
            histFeature[3] = kurtosis();
            histFeature[4] = entropy();

            int[,] glcm1 = getGLCM(1, 0);
            int[,] glcm2 = getGLCM(0, 1);
            int[,] glcm3 = getGLCM(1, 1);

            histFeature[5] = autocorrGLCM(glcm1, 1, 0);
            histFeature[6] = autocorrGLCM(glcm2, 1, 0);
            histFeature[7] = autocorrGLCM(glcm3, 1, 0);
            histFeature[8] = autocorrImage( 1, 0);

            histFeature[9] = autocorrGLCM(glcm1, 0, 1);
            histFeature[10] = autocorrGLCM(glcm2, 0, 1);
            histFeature[11] = autocorrGLCM(glcm3, 0, 1);
            histFeature[12] = autocorrImage(  0, 1);

            histFeature[13] = autocorrGLCM(glcm1, 1, 1);
            histFeature[14] = autocorrGLCM(glcm2, 1, 1);
            histFeature[15] = autocorrGLCM(glcm3, 1, 1);
            
            histFeature[16] = autocorrImage(1, 1);

            return histFeature;
        }

        //Find the autocorrelation for the 2D histogram, xd and yd are the parameters to determine the direction of the image shifting
        private double autocorrGLCM(int[,] histogram2d, int xd, int yd)
        {
            double corrsum = 0, orgsum = 0;
            int max;
            for (int i = 0 + xd; i < 256; i++)
            {
                for (int j = 0 + yd; j < 256; j++)
                {
                    corrsum += histogram2d[i, j] * histogram2d[i - xd, j - yd];
                    max = histogram2d[i, j] > histogram2d[i - xd, j - yd] ? histogram2d[i, j] : histogram2d[i - xd, j - yd];
                    orgsum += max * max;
                }
            }

            double autocorr = corrsum / orgsum;
            return autocorr;
        }

        //Find the autocorrelation for the image, xd and yd are the parameters to determine the direction of the image shifting
        private double autocorrImage(int xd, int yd)
        {
            BitmapData bmpdata = bmpGreen.LockBits(new Rectangle(0, 0, bmpGreen.Width, bmpGreen.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            double corrsum = 0, orgsum = 0;
            int max;
            unsafe
            {
                byte* inptr;
                byte* inptr2;
                for (int i = 0 + yd; i < bmpGreen.Height; i++)
                {
                    for (int j = 0 + xd; j < bmpGreen.Width; j++)
                    {
                        inptr = (byte*)bmpdata.Scan0 + j * 4 + i * bmpdata.Stride;
                        inptr2 = (byte*)bmpdata.Scan0 + (j - xd) * 4 + (i - yd) * bmpdata.Stride;
                        max = inptr[1] > inptr[1] ? inptr[1] : inptr2[1];
                        corrsum += inptr[1] * inptr2[1];
                        orgsum += max * max;
                    }
                }
            }

            bmpGreen.UnlockBits(bmpdata);
            double autocorr = corrsum / orgsum;

            return autocorr;
        }

        //Find the entropy of the histogram
        public double entropy()
        {
            double sum = 0;
            for (int i = 0; i <= 255; i++)
            {
                double normi = (double)this.histogram[i] / (double)this.totalcount;
                if (normi == 0)
                    continue;
                else
                    sum += normi * Math.Log(normi);
            }

            double entropy = -sum;
            return entropy;
        }

        //Get 2d histogram with input paramters xd and yd
        public int[,] getGLCM(int xd, int yd)
        {
            int[,] histogram2d = new int[256, 256];
            BitmapData bmpdata = bmpGreen.LockBits(new Rectangle(0, 0, bmpGreen.Width, bmpGreen.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* inptr;
                byte* inptr2;
                for (int i = 0; i < bmpdata.Height - yd; i++) {
                    for (int j = 0; j < bmpdata.Width - xd; j++) {
                        inptr = (byte*)bmpdata.Scan0 + j * 4 + i * bmpdata.Stride;
                        inptr2 = (byte*)bmpdata.Scan0 + (j + xd) * 4 + (i + yd) * bmpdata.Stride;
                        histogram2d[inptr[1], inptr2[1]]++;
                    }
                }
            }

            bmpGreen.UnlockBits(bmpdata);
            return histogram2d;
        }

        // Find the Histogram of the grayscale image
        public void calculateHistogram()
        {
            histogram = new int[256];
            BitmapData bmpdata = bmpGreen.LockBits(new Rectangle(0, 0, bmpGreen.Width, bmpGreen.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* inptr = (byte*)bmpdata.Scan0;

                for (int i = 0; i < bmpdata.Height; i++){
                    for (int j = 0; j < bmpdata.Width; j++){
                        histogram[(inptr)[2]]++;
                        inptr += 4;
                    }
                }
            }

            bmpGreen.UnlockBits(bmpdata);
        }

        // Find the mean intensity of the grayscale image using the histogram calculated in calculateHistogram() function.
        public double getMean()
        {
            double totalval = 0;

            for (int i = 0; i <= 255; i++){
                totalval += i * this.histogram[i];
                this.totalcount += this.histogram[i];
            }

            this.mean = totalval / this.totalcount;
            return this.mean;
        }

        // Find the standard deviation of the grayscale image using the mean caluclated in getMean() function
        public double standardDeviation()
        {
            double totalstdval = 0;

            for (int i = 0; i <= 255; i++)
                totalstdval += (i - this.mean) * (i - this.mean) * this.histogram[i];
            
            std = Math.Sqrt(totalstdval / this.totalcount);

            return std;
        }

        // Find the skewness of the grayscale image using the mean and std. deviation calculated in getMean() and standardDeviation() function
        public double skewness()
        {
            double skew;
            double totalskewval = 0;

            //Calculate 3rd central moment
            for (int i = 0; i <= 255; i++)
                totalskewval += Math.Pow((i - this.mean), 3) * this.histogram[i];

            skew = (totalskewval / this.totalcount) / Math.Pow(this.std, 1.5);  // M3/(M2)^3/2

            return skew;
        }

        // Find the kurtosis of the grayscale image using the mean and std. deviation calculated in getMean() and standardDeviation() function
        public double kurtosis()
        {
            double kurtosis;
            double totalkurtosisval = 0;

            //Calculate 4th central moment
            for (int i = 0; i <= 255; i++)
                totalkurtosisval += Math.Pow((i - this.mean), 4) * this.histogram[i];

            kurtosis = (totalkurtosisval / this.totalcount) / Math.Pow(this.std, 2);  // M4/(M2)^4/2

            return kurtosis;
        }
    }
}