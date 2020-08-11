using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A collection of classes to make the methods more general
namespace Habrador_Computational_Geometry
{
    //Base class
    public abstract class Curve
    {
        //All child classes need to have these methods
        public abstract MyVector3 GetInterpolatedValue(float t);

        public abstract float CalculateDerivative(float t);
    }



    //
    // Bezier Quadratic
    //
    public class BezierQuadratic : Curve
    {
        //Start and end point
        public MyVector3 posA;
        public MyVector3 posB;
        //Handle connected to start and end points
        public MyVector3 handle;


        public BezierQuadratic(MyVector3 posA, MyVector3 posB, MyVector3 handle)
        {
            this.posA = posA;
            this.posB = posB;
            
            this.handle = handle;
        }



        //Get interpolated value at point t
        public override MyVector3 GetInterpolatedValue(float t)
        {
            MyVector3 interpolatedValue = _Interpolation.BezierQuadratic(posA, posB, handle, t);

            return interpolatedValue;
        }



        //
        // Derivative
        //

        public override float CalculateDerivative(float t)
        {
            //Choose how to calculate the derivative
            //float derivative = InterpolationHelpMethods.EstimateDerivative(this, t);

            float derivative = ExactDerivative(t);

            return derivative;
        }



        //Derivative at point t
        public float ExactDerivative(float t)
        {
            MyVector3 A = posA;
            MyVector3 B = handle;
            MyVector3 C = posB;

            //Layer 1
            //(1-t)A + tB = A - At + Bt
            //(1-t)B + tC = B - Bt + Ct

            //Layer 2
            //(1-t)(A - At + Bt) + t(B - Bt + Ct)
            //A - At + Bt - At + At^2 - Bt^2 + Bt - Bt^2 + Ct^2
            //A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2 
            //A - t(2(A - B)) + t^2(A - 2B + C)

            //Derivative: -(2(A - B)) + t(2(A - 2B + C))

            MyVector3 derivativeVector = t * (2f * (A - 2f * B + C));

            derivativeVector += -2f * (A - B);


            float derivative = MyVector3.Magnitude(derivativeVector);


            return derivative;
        }
    }



    //
    // Bezier Cubic
    //
    public class BezierCubic : Curve
    {
        //Start and end points
        public MyVector3 posA;
        public MyVector3 posB;
        //Handles connected to the start and end points
        public MyVector3 handleA;
        public MyVector3 handleB;


        public BezierCubic(MyVector3 posA, MyVector3 posB, MyVector3 handleA, MyVector3 handleB)
        {
            this.posA = posA;
            this.posB = posB;

            this.handleA = handleA;
            this.handleB = handleB;
        }



        //Get interpolated value at point t
        public override MyVector3 GetInterpolatedValue(float t)
        {
            MyVector3 interpolatedValue = _Interpolation.BezierCubic(posA, posB, handleA, handleB, t);

            return interpolatedValue;
        }



        //
        // Derivative
        //
        public override float CalculateDerivative(float t)
        {
            //Choose how to calculate the derivative
            //float derivative = InterpolationHelpMethods.EstimateDerivative(this, t);

            float derivative = ExactDerivative(t);

            return derivative;
        }



        //Actual derivative at point t
        public float ExactDerivative(float t)
        {
            MyVector3 A = posA;
            MyVector3 B = handleA;
            MyVector3 C = handleB;
            MyVector3 D = posB;

            //Layer 1
            //(1-t)A + tB = A - At + Bt
            //(1-t)B + tC = B - Bt + Ct
            //(1-t)C + tD = C - Ct + Dt

            //Layer 2
            //(1-t)(A - At + Bt) + t(B - Bt + Ct) = A - At + Bt - At + At^2 - Bt^2 + Bt - Bt^2 + Ct^2 = A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2 
            //(1-t)(B - Bt + Ct) + t(C - Ct + Dt) = B - Bt + Ct - Bt + Bt^2 - Ct^2 + Ct - Ct^2 + Dt^2 = B - 2Bt + 2Ct + Bt^2 - 2Ct^2 + Dt^2

            //Layer 3
            //(1-t)(A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2) + t(B - 2Bt + 2Ct + Bt^2 - 2Ct^2 + Dt^2)
            //A - 2At + 2Bt + At^2 - 2Bt^2 + Ct^2 - At + 2At^2 - 2Bt^2 - At^3 + 2Bt^3 - Ct^3 + Bt - 2Bt^2 + 2Ct^2 + Bt^3 - 2Ct^3 + Dt^3
            //A - 3At + 3Bt + 3At^2 - 6Bt^2 + 3Ct^2 - At^3 + 3Bt^3 - 3Ct^3 + Dt^3
            //A - 3t(A - B) + t^2(3A - 6B + 3C) + t^3(-A + 3B - 3C + D)
            //A - 3t(A - B) + t^2(3(A - 2B + C)) + t^3(-(A - 3(B - C) - D)

            //The derivative: -3(A - B) + 2t(3(A - 2B + C)) + 3t^2(-(A - 3(B - C) - D)
            //-3(A - B) + t(6(A - 2B + C)) + t^2(-3(A - 3(B - C) - D)

            MyVector3 derivativeVector = t * t * (-3f * (A - 3f * (B - C) - D));

            derivativeVector += t * (6f * (A - 2f * B + C));

            derivativeVector += -3f * (A - B);


            float derivative = MyVector3.Magnitude(derivativeVector);


            return derivative;
        }



        //
        // Get a Transform (includes position and orientation) at point t
        //
        public InterpolationTransform GetTransform(float t)
        {
            //Same as when we calculate t
            MyVector3 interpolation_1_2 = _Interpolation.BezierQuadratic(posA, handleB, handleA, t);
            MyVector3 interpolation_2_3 = _Interpolation.BezierQuadratic(posA, posB, handleB, t);

            MyVector3 finalInterpolation = _Interpolation.BezierLinear(interpolation_1_2, interpolation_2_3, t);

            //This direction is always tangent to the curve
            MyVector3 forwardDir = MyVector3.Normalize(interpolation_2_3 - interpolation_1_2);

            //A simple way to get the other directions is to use LookRotation with just forward dir as parameter
            //Then the up direction will always be the world up direction, and it calculates the right direction 
            Quaternion orientation = Quaternion.LookRotation(forwardDir.ToVector3());


            InterpolationTransform trans = new InterpolationTransform(finalInterpolation, orientation);

            return trans;
        }
    }
}
