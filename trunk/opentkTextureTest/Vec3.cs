using System;

namespace StarterKit
{
    class Vec3
    {
        public float X, Y, Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        static public int Compare(Vec3 left, Vec3 right )
        {	
	        if ( left.X > right.X ) return 1;
	        else if ( left.X == right.X && left.Y > right.Y ) return 1;
	        else if ( left.X == right.X && left.Y == right.Y && left.Z > right.Z ) return 1;
            else if (left.X == right.X && left.Y == right.Y && left.Z == right.Z) return 0;
	        return -1;
        }
    }
}
