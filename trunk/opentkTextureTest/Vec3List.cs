using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarterKit
{
    class Vec3List
    {
        public List<Vec3> list;

        public Vec3List()
        {
            list = new List<Vec3>();
        }

        public Vec3List(float[] floatArray)
        {
            list = new List<Vec3>();

            for (int i = 0; i < floatArray.Length; i += 3)
            {
                Vec3 vec = new Vec3(floatArray[i],floatArray[i+1],floatArray[i+2]);
                list.Add(vec);
            }
        }

        public void Clean()
        {
            int i = 0;
            while (i<list.Count)
            {
                if ((list.ElementAt(i).X == 0.0f) && (list.ElementAt(i).Y == 0.0f) && (list.ElementAt(i).Z == 0.0f))
                {
                    list.RemoveAt(i);
                    
                }
                else
                    i++;
            }
        }

        public void Fill(int size)
        {
            Random r = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

            while (list.Count < size * size)
                list.Add(new Vec3(100.0f, 100.0f, 100.0f));
        }

        public void Sort()
        {
            list.Sort(Vec3.Compare);
        }

        public float[] ToFloatArray()
        {
            float[] floatArray = new float[list.Count*3];
            
            for (int i = 0; i < list.Count;i++)
            {
                floatArray[i*3] = list.ElementAt(i).X;
                floatArray[i * 3+1] = list.ElementAt(i).Y;
                floatArray[i * 3+2] = list.ElementAt(i).Z;
            }

            return floatArray;
        }
    }
}
