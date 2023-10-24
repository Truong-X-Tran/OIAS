using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Forms;
//using Emgu.CV;
//using Emgu.CV.Structure;
//using Emgu.CV.ML;
//using Emgu.CV.ML.Structure;

namespace CrystalX
{
    class Otsu
    {
        // function is used to compute the q values in the equation
        private float Px(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += hist[i];

            return (float)sum;
        }

        // function is used to compute the mean values in the equation (mu)
        private float Mx(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += i * hist[i];

            return (float)sum;
        }

        // finds the maximum element in a vector
        private int findMax(float[] vec, int n)
        {
            float maxVec = 0;
            int idx = 0;
            int i;

            for (i = 1; i < n - 1; i++)
            {
                if (vec[i] > maxVec)
                {
                    maxVec = vec[i];
                    idx = i;
                }
            }
            return idx;
        }

        // simply computes the image histogram
        unsafe private void getHistogram(byte* p, int w, int h, int ws, int[] hist)
        {
            hist.Initialize();
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w * 3; j += 3)
                {
                    int index = i * ws + j;
                    hist[p[index]]++;
                }
            }
        }

        // find otsu threshold
        public int getOtsuThreshold(Bitmap bmp)
        {
            byte t = 0;
            float[] vet = new float[256];
            int[] hist = new int[256];
            vet.Initialize();

            float p1, p2, p12;
            int k;

            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* p = (byte*)(void*)bmData.Scan0.ToPointer();

                getHistogram(p, bmp.Width, bmp.Height, bmData.Stride, hist);

                // loop through all possible t values and maximize between class variance
                for (k = 1; k != 255; k++)
                {
                    p1 = Px(0, k, hist);
                    p2 = Px(k + 1, 255, hist);
                    p12 = p1 * p2;
                    if (p12 == 0)
                        p12 = 1;
                    float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
                    vet[k] = (float)diff * diff / p12;
                    //vet[k] = (float)Math.Pow((Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1), 2) / p12;
                }
            }
            bmp.UnlockBits(bmData);

            t = (byte)findMax(vet, 256);

            return t;
        }

        // simple routine to convert to gray scale
        public void Convert2GrayScaleFast(Bitmap bmp)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* p = (byte*)(void*)bmData.Scan0.ToPointer();
                int stopAddress = (int)p + bmData.Stride * bmData.Height;
                while ((int)p != stopAddress)
                {
                    p[0] = (byte)(.299 * p[2] + .587 * p[1] + .114 * p[0]);
                    p[1] = p[0];
                    p[2] = p[0];
                    p += 3;
                }
            }
            bmp.UnlockBits(bmData);
        }

        // simple routine for thresholdin
        public void threshold(Bitmap bmp, int thresh)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* p = (byte*)(void*)bmData.Scan0.ToPointer();
                int h = bmp.Height;
                int w = bmp.Width;
                int ws = bmData.Stride;

                for (int i = 0; i < h; i++)
                {
                    byte* row = &p[i * ws];
                    for (int j = 0; j < w * 3; j += 3)
                    {
                        row[j] = (byte)((row[j] > (byte)thresh) ? 255 : 0);
                        row[j + 1] = (byte)((row[j + 1] > (byte)thresh) ? 255 : 0);
                        row[j + 2] = (byte)((row[j + 2] > (byte)thresh) ? 255 : 0);
                    }
                }
            }
            bmp.UnlockBits(bmData);
        }
    }

    class ImageProcessor
    {
        public string filename { get; set; }
        string imgpath { get; set; }
        Bitmap srcImage;
        string filename_only;
        int Width, Height;

        const int BinaryFeaturesCount = 11; //6 intensity features + 5 blob features
        const int HistFeaturesCount = 17; //Histogram features
        const int AllFeaturesCount = BinaryFeaturesCount * 3 + HistFeaturesCount;

        public ImageProcessor()
        {
        }

        public ImageProcessor(string imgpath)
        {
            try
            {
                this.imgpath = imgpath;
                this.filename = Path.GetFileName(imgpath);
                this.filename_only = Path.GetFileNameWithoutExtension(imgpath);
                this.srcImage = (Bitmap)Bitmap.FromFile(imgpath);
                this.Width = srcImage.Width;
                this.Height = srcImage.Height;
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show("Could not locate file " + this.imgpath);
            }
        }

        public Bitmap grayImage(Bitmap image)
        {
            Bitmap returnMap = new Bitmap(image.Width, image.Height,
                            PixelFormat.Format32bppArgb);
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0,
                             image.Width, image.Height),
                             ImageLockMode.ReadOnly,
                             PixelFormat.Format32bppArgb);
            BitmapData bitmapData2 = returnMap.LockBits(new Rectangle(0, 0,
                                     returnMap.Width, returnMap.Height),
                                     ImageLockMode.ReadOnly,
                                     PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* p1 = (byte*)bitmapData1.Scan0;
                byte* p2 = (byte*)bitmapData2.Scan0;

                for (int i = 0; i < bitmapData1.Height; i++)
                {
                    for (int j = 0; j < bitmapData1.Width; j++)
                    {
                        p2[0] = (byte)(.299 * p1[2] + .587 * p1[1] + .114 * p1[0]);
                        p2[1] = p2[0];
                        p2[2] = p2[0];

                        //4 bytes per pixel
                        p1 += 4;
                        p2 += 4;
                    }
                }
            }//end unsafe
            returnMap.UnlockBits(bitmapData2);
            image.UnlockBits(bitmapData1);
            return returnMap;
        }

        public Bitmap medianFilter(Bitmap b)
        {
            Bitmap returnMap = new Bitmap(b.Width, b.Height,
                                PixelFormat.Format32bppArgb);
            BitmapData bitmapData1 = b.LockBits(new Rectangle(0, 0,
                             b.Width, b.Height),
                             ImageLockMode.ReadOnly,
                             PixelFormat.Format32bppArgb);
            BitmapData bitmapData2 = returnMap.LockBits(new Rectangle(0, 0,
                                     returnMap.Width, returnMap.Height),
                                     ImageLockMode.ReadOnly,
                                     PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* p1 = (byte*)bitmapData1.Scan0;
                byte* p2 = (byte*)bitmapData2.Scan0;

                int stride = bitmapData1.Stride;
                int nOffset = stride - b.Width * 3;

                p1 += stride;
                p2 += stride;
                ArrayList list;

                for (int y = 1; y < b.Height - 1; ++y)
                {
                    p1 += 4;
                    p2 += 4;

                    for (int x = 1; x < b.Width - 1; ++x)
                    {
                        //for blue color
                        list = new ArrayList();
                        list.Add((p1 - stride - 4)[0]);
                        list.Add((p1 - stride)[0]);
                        list.Add((p1 + 4 - stride)[0]);
                        list.Add((p1 - 4)[0]);
                        list.Add((p1)[0]);
                        list.Add((p1 + 4)[0]);
                        list.Add((p1 + stride - 4)[0]);
                        list.Add((p1 + stride)[0]);
                        list.Add((p1 + 4 + stride)[0]);
                        list.Sort();
                        p2[0] = (byte)list[4];

                        //for greencolor
                        list = new ArrayList();
                        list.Add((p1 - stride - 4)[1]);
                        list.Add((p1 - stride)[1]);
                        list.Add((p1 + 4 - stride)[1]);
                        list.Add((p1 - 4)[1]);
                        list.Add((p1)[1]);
                        list.Add((p1 + 4)[1]);
                        list.Add((p1 + stride - 4)[1]);
                        list.Add((p1 + stride)[1]);
                        list.Add((p1 + 4 + stride)[1]);
                        list.Sort();
                        p2[1] = (byte)list[4];

                        //for red color
                        list = new ArrayList();
                        list.Add((p1 - stride - 4)[2]);
                        list.Add((p1 - stride)[2]);
                        list.Add((p1 + 4 - stride)[2]);
                        list.Add((p1 - 4)[2]);
                        list.Add((p1)[2]);
                        list.Add((p1 + 4)[2]);
                        list.Add((p1 + stride - 4)[2]);
                        list.Add((p1 + stride)[2]);
                        list.Add((p1 + 4 + stride)[2]);
                        list.Sort();
                        p2[2] = (byte)list[4];

                        p2[3] = p1[3];

                        p1 += 4;
                        p2 += 4;
                    }
                    p1 += 4;
                    p2 += 4;

                }
            }

            returnMap.UnlockBits(bitmapData2);
            b.UnlockBits(bitmapData1);
            return returnMap;
        }

        public Bitmap resizeImage(Bitmap imgToResize, double nPercentW, double nPercentH)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            double nPercent = 0;

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage(b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return b;
        }

        public Bitmap otsuThreshold(Bitmap image, out int threshIntensity_Otsu)
        {
            Otsu ot = new Otsu();
            Bitmap temp = (Bitmap)image.Clone();
            ot.Convert2GrayScaleFast(temp);
            threshIntensity_Otsu = ot.getOtsuThreshold((Bitmap)temp);
            ot.threshold(temp, threshIntensity_Otsu);
            return temp;
        }

        public int getGreenThresholdVal(Bitmap image, double greenFactor)
        {
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0,
                             image.Width, image.Height),
                             ImageLockMode.ReadOnly,
                             PixelFormat.Format32bppArgb);
            int count = 0;
            int[] hg = new int[256];
            for (int k = 0; k < 256; k++)
                hg[k] = 0;

            int maxg = 0;

            unsafe
            {
                byte* p1 = (byte*)bitmapData1.Scan0;

                int height = image.Height;
                int width = image.Width;

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        int r = p1[2];
                        int g = p1[1];
                        int b = p1[0];
                        //int t = p1[4];

                        if (g > maxg)
                            maxg = g;

                        hg[p1[1]]++;
                        count++;
                        p1 += 4;
                    }
                }
            }

            int greenThreshold = 0;
            long testCount = 0;
            long checkCount = (long)(greenFactor * count);

            while (testCount < checkCount)
                testCount += hg[greenThreshold++];

            image.UnlockBits(bitmapData1);
            //Console.WriteLine("MaxG: " + maxg + " GreenThreshold: " + greenThreshold);
            return greenThreshold;
        }

        public Bitmap greenPerThreshold(Bitmap image, double greenFactor, out int threshIntensity_green)
        {
            threshIntensity_green = getGreenThresholdVal(image, greenFactor);

            Bitmap returnMap = new Bitmap(image.Width, image.Height,
                            PixelFormat.Format32bppArgb);
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0,
                             image.Width, image.Height),
                             ImageLockMode.ReadOnly,
                             PixelFormat.Format32bppArgb);
            BitmapData bitmapData2 = returnMap.LockBits(new Rectangle(0, 0,
                                     returnMap.Width, returnMap.Height),
                                     ImageLockMode.ReadWrite,
                                     PixelFormat.Format32bppArgb);

            int count = 0;
            int maxThreshold = 127;
            int minThreshold = 50;

            unsafe
            {
                byte* p1 = (byte*)bitmapData1.Scan0;
                byte* p2 = (byte*)bitmapData2.Scan0;

                int inten;

                p1 += 4 * bitmapData1.Width;
                p2 += 4 * bitmapData2.Width;

                for (int i = 1; i < bitmapData1.Height; i++)
                {
                    for (int j = 1; j < bitmapData1.Width; j++)
                    {
                        inten = (int)Math.Sqrt(p1[2] * p1[2] * .241 + p1[1] * p1[1] * .691 + p1[0] * p1[0] * .068);

                        if ((inten > minThreshold && p1[1] > threshIntensity_green && p1[1] > p1[0] && p1[1] > p1[2]) || inten > maxThreshold)
                        {
                            //white = true;
                            p2[0] = 255;
                            p2[1] = 255;
                            p2[2] = 255;
                            count++;
                        }

                        else
                        {
                            //white = false;
                            p2[0] = 0;
                            p2[1] = 0;
                            p2[2] = 0;
                        }

                        p2[3] = p1[3];

                        //4 bytes per pixel
                        p1 += 4;
                        p2 += 4;
                    }
                }
            }//end unsafe

            //Console.WriteLine("Green Threshold : " + greenThreshold);
            returnMap.UnlockBits(bitmapData2);
            image.UnlockBits(bitmapData1);
            return returnMap;
        }

        public Bitmap skeletonizeImage(Bitmap b)
        {
            Bitmap returnMap = new Bitmap(b.Width, b.Height,
                                PixelFormat.Format32bppArgb);
            BitmapData bitmapData1 = b.LockBits(new Rectangle(0, 0,
                             b.Width, b.Height),
                             ImageLockMode.ReadOnly,
                             PixelFormat.Format32bppArgb);
            BitmapData bitmapData2 = returnMap.LockBits(new Rectangle(0, 0,
                                     returnMap.Width, returnMap.Height),
                                     ImageLockMode.ReadOnly,
                                     PixelFormat.Format32bppArgb);

            int noffset = bitmapData1.Stride - (bitmapData1.Width * 4);

            unsafe
            {
                byte* p1 = (byte*)bitmapData1.Scan0;
                byte* p2 = (byte*)bitmapData2.Scan0;

                int stride = bitmapData1.Stride;

                p1 += stride;
                p2 += stride;

                int[] list = new int[9];

                for (int y = 1; y < b.Height - 1; ++y)
                {
                    p1 += 4;
                    p2 += 4;

                    for (int x = 1; x < b.Width - 1; ++x)
                    {
                        //for red color
                        list[0] = ((p1 - stride - 4)[2]);
                        list[1] = ((p1 - stride)[2]);
                        list[2] = ((p1 + 4 - stride)[2]);
                        list[3] = ((p1 - 4)[2]);
                        list[4] = ((p1)[2]);
                        list[5] = ((p1 + 4)[2]);
                        list[6] = ((p1 + stride - 4)[2]);
                        list[7] = ((p1 + stride)[2]);
                        list[8] = ((p1 + 4 + stride)[2]);

                        //Here, the morphological shape is the all neighboring pixels being foreground(white)
                        var tmp = list[0] & list[1] & list[2] & list[3] & list[4] & list[5] & list[6] & list[7];

                        if (tmp == list[0]) //Its a hit. To skeletonize, we change the pixel to black
                            p2[0] = p2[1] = p2[2] = p2[3] = (byte)0;
                        else //its a miss, we leave the pixel as it is
                        {
                            p2[0] = p1[0];
                            p2[1] = p2[2] = p2[3] = p2[0];
                        }
                        p1 += 4;
                        p2 += 4;
                    }
                }
            }

            returnMap.UnlockBits(bitmapData2);
            b.UnlockBits(bitmapData1);
            return returnMap;
        }

        public void getBlobFeature(Bitmap bmp, int CGx, int CGy, out double uniformity, out double nonUniformity_std, out double symmetry, out int pix_cnt_skeleton)
        {
            Bitmap skeletonImg = skeletonizeImage(bmp);
            BitmapData bitmapData1 = skeletonImg.LockBits(new Rectangle(0, 0,
                             bmp.Width, bmp.Height),
                             ImageLockMode.ReadOnly,
                             PixelFormat.Format32bppArgb);

            double tmpNonUniformityDiff = 0;
            double tmpNonUniformityStd = 0;
            double tmpNonSymmetrySum = 0;

            double assumedRadius = (CGx + CGy) / 2; //used to compute uniformity
            double sumDiff = 0;

            double prevDistance = assumedRadius; //used to compute symmetry

            int count_whitepixel = 0;

            unsafe
            {
                byte* p1 = (byte*)bitmapData1.Scan0;

                for (int i = 0; i < bitmapData1.Height; i++)
                {
                    for (int j = 0; j < bitmapData1.Width; j++)
                    {
                        if (p1[0] == 0) //Wanted to check if the pixel is a foreground. Here, the value 0 is used because in the input image foreground region is in black
                        {
                            double currDist = Math.Sqrt((CGx - j) * (CGx - j) + (CGy - i) * (CGy - i));
                            double diffFromRadius = Math.Abs(assumedRadius - currDist);

                            sumDiff += diffFromRadius;

                            tmpNonUniformityStd += diffFromRadius * diffFromRadius;
                            double allowableDiff = 0.15 * assumedRadius;
                            if (diffFromRadius > allowableDiff)
                            {
                                tmpNonUniformityDiff += 1;
                            }

                            tmpNonSymmetrySum += (Math.Abs(prevDistance - currDist) < allowableDiff) ? 0 : 1;
                            prevDistance = currDist;
                            count_whitepixel++;
                        }
                        p1 += 4;
                    }
                }
            }//end unsafe

            tmpNonUniformityStd /= count_whitepixel;
            tmpNonUniformityStd = Math.Sqrt(tmpNonUniformityStd);

            tmpNonSymmetrySum = tmpNonSymmetrySum / count_whitepixel;
            tmpNonUniformityDiff = tmpNonUniformityDiff / count_whitepixel;

            skeletonImg.UnlockBits(bitmapData1);
            uniformity = 1 - tmpNonUniformityDiff;
            nonUniformity_std = tmpNonUniformityStd;
            symmetry = 1 - tmpNonSymmetrySum;
            pix_cnt_skeleton = count_whitepixel;
        }

        public double[] getStatsUsingMask(Bitmap origImage, Bitmap binImage)
        {
            BitmapData bitmapData1 = origImage.LockBits(new Rectangle(0, 0,
                             origImage.Width, origImage.Height),
                             ImageLockMode.ReadOnly,
                             PixelFormat.Format32bppArgb);

            BitmapData bitmapData2 = binImage.LockBits(new Rectangle(0, 0,
                             binImage.Width, binImage.Height),
                             ImageLockMode.ReadOnly,
                             PixelFormat.Format32bppArgb);

            double avg_inten_white = 0;
            double avg_inten_dark = 0;

            double std_inten_white = 0;
            double std_inten_dark = 0;

            int white_pix_cnt = 0;

            unsafe
            {
                byte* p1 = (byte*)bitmapData1.Scan0;
                byte* p2 = (byte*)bitmapData2.Scan0;

                for (int i = 0; i < bitmapData1.Height; i++)
                {
                    for (int j = 0; j < bitmapData1.Width; j++)
                    {
                        double tmp = (.299 * p1[2] + .587 * p1[1] + .114 * p1[0]);
                        if (p2[0] > 0)
                        {
                            avg_inten_white += tmp;
                            white_pix_cnt++;
                        }
                        else
                        {
                            avg_inten_dark += tmp;
                        }
                        p1 += 4;
                        p2 += 4;
                    }
                }

                avg_inten_white = (avg_inten_white == 0) ? 0 : (avg_inten_white / white_pix_cnt);

                double dark_pix_cnt = origImage.Width * origImage.Height - white_pix_cnt;
                avg_inten_dark = (avg_inten_dark == 0) ? 0 : (avg_inten_dark / dark_pix_cnt);

                p1 = (byte*)bitmapData1.Scan0;
                p2 = (byte*)bitmapData2.Scan0;

                for (int i = 0; i < bitmapData1.Height; i++)
                {
                    for (int j = 0; j < bitmapData1.Width; j++)
                    {
                        double this_inten = (.299 * p1[2] + .587 * p1[1] + .114 * p1[0]);

                        if (p2[0] > 0)
                        {
                            std_inten_white += (this_inten - avg_inten_white) * (this_inten - avg_inten_white);
                        }
                        else
                        {
                            std_inten_dark += (this_inten - avg_inten_dark) * (this_inten - avg_inten_dark);
                        }
                        p1 += 4;
                        p2 += 4;
                    }
                }

                std_inten_white = (std_inten_white == 0) ? 0 : (Math.Sqrt(std_inten_white / white_pix_cnt));
                std_inten_dark = (std_inten_dark == 0) ? 0 : (Math.Sqrt(std_inten_dark / dark_pix_cnt));

                double[] inten = new double[5];
                int c = 0;

                inten[c++] = white_pix_cnt;
                inten[c++] = avg_inten_white;
                inten[c++] = std_inten_white;
                inten[c++] = avg_inten_dark;
                inten[c++] = std_inten_dark;

                origImage.UnlockBits(bitmapData1);
                binImage.UnlockBits(bitmapData2);

                return inten;
            }
        }
        
        public double[] getBlobsSummary(Bitmap srcImage)
        {
            int count_blobs = 0;

            double largestBlobArea = 0, largestBlobFullness = 0;
            double avgBlobArea = 0, avgBlobFullness = 0;

            ConnectedComponentLabeling target = new ConnectedComponentLabeling();
            var blobs = target.Process(srcImage);
            int blobsCount = blobs.Count;

            foreach (var blob in blobs)
            {
                Blob _blob = blob.Value;
                Bitmap bmp = _blob.image;

                if (blobsCount == 0)
                {
                    largestBlobArea = _blob.Area;
                    largestBlobFullness = _blob.Fullness;
                }

                else
                {
                    avgBlobArea += _blob.Area;
                    avgBlobFullness += _blob.Fullness;
                }

                count_blobs++;

                if (count_blobs == 6) break;
            }

            if (count_blobs == 0)
                avgBlobArea = avgBlobFullness = 0;
            else {
                avgBlobArea = Convert.ToDouble(avgBlobArea / count_blobs);
                avgBlobFullness = Convert.ToDouble(avgBlobFullness / count_blobs);
            }

            double[] blobFeatures = new double[5];
            int fcnt = 0;

            blobFeatures[fcnt++] = largestBlobArea;
            blobFeatures[fcnt++] = largestBlobFullness;
            blobFeatures[fcnt++] = avgBlobArea;
            blobFeatures[fcnt++] = avgBlobFullness;
            blobFeatures[fcnt++] = blobs.Count;
            
            return blobFeatures;
        }

        //Get green color channel in an image
        public Bitmap getGreenChannelImage(Bitmap img)
        {
            Bitmap bmpout = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            BitmapData bmpdata = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmpoutdata = bmpout.LockBits(new Rectangle(0, 0, bmpout.Width, bmpout.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* inptr = (byte*)bmpdata.Scan0;
                byte* outptr = (byte*)bmpoutdata.Scan0;

                for (int i = 0; i < bmpdata.Height; i++){
                    for (int j = 0; j < bmpdata.Width; j++){
                        outptr[0] = inptr[1];
                        outptr[1] = inptr[1];
                        outptr[2] = inptr[1];
                        outptr[3] = inptr[3];

                        inptr += 4;  outptr += 4;
                    }
                }
            }

            img.UnlockBits(bmpdata);
            bmpout.UnlockBits(bmpoutdata);
            return bmpout;
        }

        public double[] extractFeatures()
        {
            double[] features = new double[AllFeaturesCount];
            int fcnt = 0;

            Bitmap srcImage = (Bitmap)Bitmap.FromFile(imgpath);
            int Width = srcImage.Width;
            int Height = srcImage.Height;

            Bitmap resizedImage = resizeImage(srcImage, (1.0 * 320) / Width, (1.0 * 240) / Height);
            Bitmap medianImage = medianFilter(resizedImage);

            //Otsu thresholding
            int ThresholdIntensity;
            Bitmap binImage_tmp = otsuThreshold(medianImage, out ThresholdIntensity);
            Bitmap binImage = medianFilter(binImage_tmp);

            //Add intensity features using Otsu
            features[fcnt++] = ThresholdIntensity;
            double[] tmpFeatures = getStatsUsingMask(medianImage, binImage);

            for (int j = 0; j < tmpFeatures.Length; j++)
                features[fcnt++] = tmpFeatures[j];

            //Add region (blob) features using Otsu
            tmpFeatures = getBlobsSummary(binImage);
            for (int j = 0; j < tmpFeatures.Length; j++)
                features[fcnt++] = tmpFeatures[j];

            //G90 thresholding
            binImage_tmp = greenPerThreshold(medianImage, 0.9, out ThresholdIntensity);
            binImage = medianFilter(binImage_tmp);

            //Add intensity features using G90
            features[fcnt++] = ThresholdIntensity;
            tmpFeatures = getStatsUsingMask(medianImage, binImage);

            for (int j = 0; j < tmpFeatures.Length; j++)
                features[fcnt++] = tmpFeatures[j];

            //Add region (blob) features using Otsu
            tmpFeatures = getBlobsSummary(binImage);
            for (int j = 0; j < tmpFeatures.Length; j++)
                features[fcnt++] = tmpFeatures[j];

            //G100 thresholding
            binImage_tmp = greenPerThreshold(medianImage, 1.0, out ThresholdIntensity);
            binImage = medianFilter(binImage_tmp);

            //Add intensity features using G99
            features[fcnt++] = ThresholdIntensity;
            tmpFeatures = getStatsUsingMask(medianImage, binImage);

            for (int j = 0; j < tmpFeatures.Length; j++)
                features[fcnt++] = tmpFeatures[j];

            //Add region (blob) features using G99
            tmpFeatures = getBlobsSummary(binImage);
            for (int j = 0; j < tmpFeatures.Length; j++)
                features[fcnt++] = tmpFeatures[j];

            HistogramFeaturesExtractor hx = new HistogramFeaturesExtractor(medianImage);
            tmpFeatures = hx.getHistogramFeatures();
            for (int j = 0; j < tmpFeatures.Length; j++)
                features[fcnt++] = tmpFeatures[j];

            return features;
        }
    }
}