using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrystalX
{
    class ImageRegistration
    {
        private void initialize()
        {
            //myerror = new int[1200, 400];
            //tracePointy = new float[4];
            //tracePointx = new float[4];

            //motionVector = new float[maxMotionParameterSize];
            //previousMotionVector = new float[maxMotionParameterSize];

            ////This for loop initalizes the motion vector arrays to
            ////{0,0,1,0,0,1,0,0} the loop form is to allow for easy changing
            ////of maxMotionParameterSize while keeping the form.
            //int count = 1;
            //for (int i = 0; i < maxMotionParameterSize; i++)
            //{
            //    if (count == 2)
            //    {
            //        motionVector[i] = 1;
            //        count = 1;
            //    }
            //    else
            //    {
            //        motionVector[i] = 0;
            //        count++;
            //    }
            //}

            ////If we are using frame grabber then use it to pull out the frames
            ////else use the normal ReadWriteFrame method to get them.

            ////create the mask arrays that will hold the data
            //mask1 = new bool[imageHeight, imageWidth];
            //mask2 = new bool[imageHeight, imageWidth];

            //Mask1 = new bool[imageHeight, imageWidth];
            //Mask2 = new bool[imageHeight, imageWidth];

            ////Set the width and height of both frames
            //image1Width = imageWidth;
            //image1Height = imageHeight;
            //image2Width = imageWidth;
            //image2Height = imageHeight;

            ////set the offets of the images aka the center of the image
            //image1Offsetx = image1Width / 2;
            //image1Offsety = image1Height / 2;
            //image2Offsetx = image2Width / 2;
            //image2Offsety = image2Height / 2;

            ////set up the trace points
            //tracePointy[0] = 28;
            //tracePointx[0] = 134;
            //tracePointy[1] = 30;
            //tracePointx[1] = 92;
            //tracePointy[2] = 31;
            //tracePointx[2] = 83;
            //tracePointy[3] = 100;
            //tracePointx[3] = 100;

        }

        void test()
        {
            //    float error;
            //    error = estimator.estimateHierarchicalMotion(frame1[0], frame2[0], image1Width, image1Height, image2Width, image2Height, image1Offsety, image1Offsetx, image2Offsety, image2Offsetx,
            //        imageTop, imageBottom, imageLeft, imageRight, image2Top, image2Bottom, image2Left, image2Right, // all 0
            //        motionVector, // output
            //        previousMotionVector, // same as motionVector
            //        motionModel, //0
            //        tracePointy, tracePointx, // not important
            //        motionBoundary, // not important
            //        false,
            //        oldError, myerror, // 0
            //        mask1, mask2, // not used
            //        useMask, // false
            //        maskFileName // not used
            //        );
            //    motionParameters[weight, maxMotionParameterSize] = error;
            //}
        }
    }
}
