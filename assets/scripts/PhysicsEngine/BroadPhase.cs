using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// Base class for different BroadPhase solver.
namespace Galileo
{
    public struct Pair
    {
        Body a;
        public Body A
        {
            get { return a; }
        }

        Body b;
        public Body B
        {
            get { return b; }
        }

        public Pair(Body A, Body B)
        {
            this.a = A;
            this.b = B;
        }
    }

    public class BroadPhase  
    {
        protected List<Pair> pairs = new List<Pair>();
        public List<Pair> Pairs
        {
            get { return pairs; }
        }

        public virtual void GeneratePairs(List<Body> bodies) { }
    }


    public class BoundingSphere
    {
        public float Radius;
        public Vector3 Center = Vector3.zero;

        public BoundingSphere(float radius, Vector3 center)
        {
            this.Radius = radius;
            this.Center = center;
        }
    }

    public class BroadPhaseSphere : BroadPhase
    {
        private bool SphereToSphere(BoundingSphere A, BoundingSphere B)
        {
            Vector3 position_A = A.Center;
            Vector3 position_B = B.Center;

            float distance = (position_A - position_B).sqrMagnitude;

            bool result = distance <= ((A.Radius + B.Radius) * (A.Radius + B.Radius));

            return result;
        }

        // Generates pairs. No duplicate pair will be present.
        public override void GeneratePairs(List<Body> bodies)
        {
            pairs.Clear();
               
            for(int i = 0; i < bodies.Count; ++i)
            {
                for (int j = i + 1; j < bodies.Count; ++j)
                {
                    Body A = bodies[i];
                    Body B = bodies[j];
                    
                    BoundingSphere sphere_A = A.Shape.GetBoundingSphere();
                    BoundingSphere sphere_B = B.Shape.GetBoundingSphere();

                    if (SphereToSphere(sphere_A, sphere_B))
                        pairs.Add(new Pair(A, B));
                }
            }
        }
    }
}
