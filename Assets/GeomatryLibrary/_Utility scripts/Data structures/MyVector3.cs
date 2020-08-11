using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Habrador_Computational_Geometry
{
    //Unity loves to automatically cast beween Vector2 and Vector3
    //Because theres no way to stop it, its better to use a custom struct 
    public struct MyVector3
    {
        public float x;
        public float y;
        public float z;

        public MyVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }



        //Vector operations
        public static float Dot(MyVector3 a, MyVector3 b)
        {
            float dotProduct = (a.x * b.x) + (a.y * b.y) + (a.z * b.z);

            return dotProduct;
        }

        public static float Magnitude(MyVector3 a)
        {
            float magnitude = Mathf.Sqrt(SqrMagnitude(a));

            return magnitude;
        }

        public static float SqrMagnitude(MyVector3 a)
        {
            float sqrMagnitude = (a.x * a.x) + (a.y * a.y) + (a.z * a.z);

            return sqrMagnitude;
        }

        public static float Distance(MyVector3 a, MyVector3 b)
        {
            float distance = Magnitude(a - b);

            return distance;
        }

        public static float SqrDistance(MyVector3 a, MyVector3 b)
        {
            float distance = SqrMagnitude(a - b);

            return distance;
        }

        public static MyVector3 Normalize(MyVector3 v)
        {
            float v_magnitude = Magnitude(v);

            MyVector3 v_normalized = new MyVector3(v.x / v_magnitude, v.y / v_magnitude, v.z / v_magnitude);

            return v_normalized;
        }

        public static MyVector3 Cross(MyVector3 a, MyVector3 b)
        {
            float x = (a.y * b.z) - (a.z * b.y);
            float y = (a.z * b.x) - (a.x * b.z);
            float z = (a.x * b.y) - (a.y * b.x);

            MyVector3 crossProduct = new MyVector3(x, y, z);

            return crossProduct;
        }



        //Operator overloads
        public static MyVector3 operator +(MyVector3 a, MyVector3 b)
        {
            return new MyVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static MyVector3 operator -(MyVector3 a, MyVector3 b)
        {
            return new MyVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static MyVector3 operator *(MyVector3 a, float b)
        {
            return new MyVector3(a.x * b, a.y * b, a.z * b);
        }

        public static MyVector3 operator *(float b, MyVector3 a)
        {
            return new MyVector3(a.x * b, a.y * b, a.z * b);
        }

        public static MyVector3 operator -(MyVector3 a)
        {
            return a * -1f;
        }
    }
}
