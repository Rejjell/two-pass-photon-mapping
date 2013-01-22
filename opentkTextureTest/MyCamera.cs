using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace StarterKit
{
    class MyCamera
    {
        private Vector3 position;
        private Vector3 view;
        private Vector3 right;
        private Vector3 up;
        private Vector2 scale;

        private float movementSpeed = 0.1f;

        public MyCamera()
        {
            position = new Vector3(0.0F, 0.0F, -21.0F);
            view = new Vector3(0.0F, 0.0F, 1.0F);
            scale = new Vector2(0.555F, 0.555F);

            view = Vector3.Normalize(view);
            Vector3 down = new Vector3(0, -1, 0);
            right = Vector3.Normalize(Vector3.Cross(view, down));
            up = Vector3.Normalize(Vector3.Cross(view, right));
        }

        private Vector3 Multiplicate(Vector3 v, float[,] a)
        {
            Vector3 rv;

            rv.X = v.X * a[0, 0] + v.Y * a[1, 0] + v.Z * a[2, 0];
            rv.Y = v.X * a[0, 1] + v.Y * a[1, 1] + v.Z * a[2, 1];
            rv.Z = v.X * a[0, 2] + v.Y * a[1, 2] + v.Z * a[2, 2];

            return rv;
        }

        private Vector3 Rotate(Vector3 v, Vector3 axis, float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            float[,] rotateMatrix =
                {
                    {cos+(1-cos)*axis.X*axis.X, (1-cos)*axis.X*axis.Y-sin*axis.Z, (1-cos)*axis.X*axis.Z+sin*axis.Y},
                    {(1-cos)*axis.X*axis.Y+sin*axis.Z, cos+(1-cos)*axis.Y*axis.Y, (1-cos)*axis.Y*axis.Z-sin*axis.X},
                    {(1-cos)*axis.X*axis.Z-sin*axis.Y, (1-cos)*axis.Y*axis.Z+sin*axis.X, cos+(1-cos)*axis.Z*axis.Z}
                };
            return Multiplicate(v, rotateMatrix);
        }

        public void RotateAroundUp(float angle)
        {
            view = Rotate(view, up, angle);
            right = Rotate(right, up, angle);
        }

        public void RotateAroundView(float angle)
        {
            up = Rotate(up, view, angle);
            right = Rotate(right, view, angle);
        }

        public void RotateAroundRight(float angle)
        {
            view = Rotate(view, right, angle);
            up = Rotate(up, right, angle);
        }

        public void MoveUp()
        {
            position += movementSpeed * up;
        }

        public void MoveDown()
        {
            position -= movementSpeed * up;
        }

        public void MoveRight()
        {
            position += movementSpeed * right;
        }

        public void MoveLeft()
        {
            position -= movementSpeed * right;
        }

        public void MoveForward()
        {
            position += movementSpeed * view;
        }

        public void MoveBack()
        {
            position -= movementSpeed * view;
        }

        public void ScalePlus()
        {
            scale.X += 0.01f;
            scale.Y += 0.01f;
        }

        public void ScaleMinus()
        {
            scale.X -= 0.01f;
            scale.Y -= 0.01f;
        }

        public Vector3 GetPosition()
        {
            return position;
        }

        public Vector3 GetUp()
        {
            return up;
        }

        public Vector3 GetRight()
        {
            return right;
        }

        public Vector3 GetView()
        {
            return view;
        }

        public Vector2 GetScale()
        {
            return scale;
        }

        public string GetCoords()
        {
            return position.ToString() + "\n" + view.ToString() + "\n" + scale.ToString();
        }


    }
}
