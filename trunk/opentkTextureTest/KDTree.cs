﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using System.Text;
using System.Threading.Tasks;

namespace StarterKit
{


    class KDTree
    {
        private KDNode root;
        public List<Vector4> mainData;
        public List<Vector4> secData;
        private int nodes = 0;
        private int leaves = 0;
        
        public KDTree()
        {
            root = new KDNode();
            root.k = 0;
            root.visited = false;
            mainData = new List<Vector4>();
            secData = new List<Vector4>();
            //this.points = pts;

        }

        public void Balance(List<Vector3> pts)
        {
            KDNode current = root;

            
            List<Vector3> points = pts;
            List<List<Vector3>> pointsStack = new List<List<Vector3>>();
            pointsStack.Add(points);

            List<Vector3> leftPoints = new List<Vector3>();
            List<Vector3> rightPoints = new List<Vector3>();
            List<Vector3> parentPoints = new List<Vector3>();

            List<float> X = new List<float>();
            List<float> Y = new List<float>();
            List<float> Z = new List<float>();

            int k = 0;

            while (current != null)
            {
                points = new List<Vector3>(pointsStack.Last());

                if (points.Count > 1)
                {
                    X.Clear();
                    Y.Clear();
                    Z.Clear();

                    for (int i = 0; i < points.Count; i++)
                    {
                        X.Add(points.ElementAt(i).X);
                        Y.Add(points.ElementAt(i).Y);
                        Z.Add(points.ElementAt(i).Z);
                    }

                    Vector3 dif = new Vector3(X.Max() - X.Min(), Y.Max() - Y.Min(), Z.Max() - Z.Min());
                    

                    leftPoints.Clear();
                    rightPoints.Clear();

                    float maxDim = Math.Max(dif.X, Math.Max(dif.Y, dif.Z));

                    if (maxDim == dif.X)
                    {
                        current.point = new Vector3(X.Average(), 0.0f, 0.0f);

                        for (int i = 0; i < points.Count; i++)
                            if (points.ElementAt(i).X < current.point.X)
                                leftPoints.Add(points.ElementAt(i));
                            else
                                rightPoints.Add(points.ElementAt(i));
                    }
                    else if (maxDim == dif.Y)
                    {
                        current.point = new Vector3(0.0f, Y.Average(), 0.0f);


                        for (int i = 0; i < points.Count; i++)
                            if (points.ElementAt(i).Y < current.point.Y)
                                leftPoints.Add(points.ElementAt(i));
                            else
                                rightPoints.Add(points.ElementAt(i));
                    }
                    else if (maxDim == dif.Z)
                    {
                        current.point = new Vector3(0.0f, 0.0f, Z.Average());

                        for (int i = 0; i < points.Count; i++)
                            if (points.ElementAt(i).Z < current.point.Z)
                                leftPoints.Add(points.ElementAt(i));
                            else
                                rightPoints.Add(points.ElementAt(i));

                    }

                    if (!current.visited)
                    {
                        nodes++;
                        current.left = new KDNode();
                        current.right = new KDNode();
                        current.left.parent = current;
                        current.right.parent = current;
                    }
                    
           
                    current.photon = false;

                }
                else
                {
                    current.point = points.ElementAt(0);
                    current.photon = true;

                    leaves++;
                }

                current.visited = true;

                if ((current.left!=null) && (!current.left.visited))
                {
                    current.left.parent = current;
                    //current.left.parentk = current.k;
                    /*k++;
                    current.leftk = k;
                    current.left.k = k;*/
                    

                    current = current.left;
                    
                    pointsStack.Add(new List<Vector3>(leftPoints));
                    //parentPoints = new List<Vector3>(points);
                    //points = new List<Vector3>(leftPoints);
                }
                else
                {
                    if ((current.right != null) && (!current.right.visited))
                    {
                        current.right.parent = current;
                        /*current.right.parentk = current.k;
                        k++;
                        current.rightk = k;
                        current.right.k = k;*/

                        current = current.right;

                        //parentPoints = new List<Vector3>(points);
                        //points = new List<Vector3>(rightPoints);

                        pointsStack.Add(new List<Vector3>(rightPoints));

                    }
                    else
                    {
                        current = current.parent;
                        pointsStack.RemoveAt(pointsStack.Count-1);
                    }
                }

                

            }





        }

        /*public float BuildData(List<Vector4> mainData, List<Vector4> secData, float par)
        {
            float ph = 0.0f;
            if (root.photon)
                ph = 1.0f;
            Vector4 mainDataVec = new Vector4(root.point.X, root.point.Y, root.point.Z, ph);
            mainData.Add(mainDataVec);
            //GC.Collect();
            

            float left;
            float right;

            if (!root.photon)
            {
                float currentNumber = mainData.Count - 1;
                left = root.left.BuildData(mainData, secData, currentNumber);
                right = root.right.BuildData(mainData, secData, currentNumber);
            }
            else
            {
                left = -1.0f;
                right = -1.0f;
            }

            secData.Add(new Vector4(left, right, par, 0.0f));
            GC.Collect();
            

            return secData.Count - 1;
        }*/

    }
}
