using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Galileo
{
	public class PhysicsWorld
	{
		#region Members
		private List<Body> bodies = new List<Body>();

        private Vector3 gravity;

        private BroadPhase broadPhase = new BroadPhaseSphere();

        private Vector3 extents = new Vector3();
		#endregion

        #region Accessors
        public Vector3 Gravity 
        {
            get { return gravity; }
            set { gravity = value; }
        }

		public static Vector3 DefaultGravity = new Vector3(0.0f, -9.8f, 0.0f);

        public UnityEngine.Vector3 Extents
        {
            get { return extents; }
        }
        #endregion

        #region Constructors
        public PhysicsWorld()
        {
			this.gravity = DefaultGravity;
            this.extents = new Vector3(500.0f, 500.0f, 500.0f);
        }

        public PhysicsWorld(Vector3 gravity, Vector3 extents)
        {
            this.gravity = gravity;
            this.extents = extents;
        }
        #endregion

        public void AddBody(Body b)
        {
            b.World = this;
			b.IsAwake = true;
            b.UpdateTransforms();
            bodies.Add(b);
        }

		public void RemoveBody(Body b)
		{
			bodies.Remove (b);
		}

        #region Helpers
        public Quaternion MultQuatByVec(Vector3 lhs, Quaternion rhs)
        {
            return new Quaternion(
                           rhs.w * lhs.x + rhs.z * lhs.y - rhs.y * lhs.z,
                           rhs.w * lhs.y + rhs.x * lhs.z - rhs.z * lhs.x,
                           rhs.w * lhs.z + rhs.y * lhs.x - rhs.x * lhs.y,
                           -(rhs.x * lhs.x + rhs.y * lhs.y + rhs.z * lhs.z));
        }

        public static Quaternion MultQuatByReal(Quaternion quat, float r)
        {
            return new Quaternion(quat.x / r, quat.y / r, quat.z / r, quat.w / r);
        }

        public static Quaternion AddQuaternions(Quaternion lhs, Quaternion rhs)
        {
            return new Quaternion(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w);
        }

        public static Quaternion AddScaledVectorToQuaternion(Quaternion q, Vector3 v, float s)
        {
            Quaternion q1 = new Quaternion(v.x * s, v.y * s, v.z * s, 0.0f);
            Quaternion result = q;

            q1 *= result;

            result.w += q1.w * 0.5f;
            result.x += q1.x * 0.5f;
            result.y += q1.y * 0.5f;
            result.z += q1.z * 0.5f;


            return result;
        }

        public static Quaternion NormalizeQuaternion(Quaternion q)
        {
            float d = q.w * q.w + q.x * q.x + q.y * q.y + q.z * q.z;
            // If a zero-length Quaternion, return identity.
            if (d == 0.0)
            {
                return Quaternion.identity;
            }
            d = (1.0f / Mathf.Sqrt(d));

            return new Quaternion(q.x * d, q.y * d, q.z * d, q.w * d);
        }
        #endregion

		public void Step(float dt)
		{
            float inv_dt = (dt > 0.0f) ? (1.0f / dt) : 0.0f; // Just the inverse of delta time.

            // Broadphase.
            broadPhase.GeneratePairs(bodies);
            List<Pair> pairs = broadPhase.Pairs;

            // Generate Contacts.

			const int k_num_iterations = 1;
			for (int i = 0; i < k_num_iterations; ++i) 
			{
				CollisionData collistion_data = new CollisionData ();
				foreach (Pair pair in pairs) 
				{
					CollisionDetector.Collide (pair.A.Shape, pair.B.Shape, collistion_data);
				}


				if (collistion_data != null && collistion_data.Contacts.Count > 0) 
				{
					foreach (Contact c in collistion_data.Contacts) 
					{
						for(int c_i = 0; i < c.ContactsCount; ++i)
						{
							GlobalDebugRenderer.Instance.AddCommand (new DebugSphereCommand (c.Point[c_i], 0.05f, true, Color.red));
							//Debug.DrawLine (c.A.Transform.position, c.B.Transform.position);
						}
					}
						//  Debug.Break();
				}


				// Apply Impulses.
				foreach (Contact c in collistion_data.Contacts) 
				{
					ContactResolver.ResolveContact (c, k_num_iterations);
				}
			}

            // Integration.
            foreach(Body b in bodies)
            {
                Galileo.Transform t = b.Transform;

                MassData mass_info = b.MassInfo;
                
				// Ignore bodies with infinite mass.
                if (mass_info.InverseMass == 0.0f)
                    continue;

				// Ignore sleeping boodies.
				if(b.IsAwake == false)
					continue;

                b.AddForce(gravity * b.MassInfo.Mass);

                b.Velocity = b.Velocity * 0.99f;
                b.Velocity += dt * (mass_info.InverseMass * b.Force);

                t.position += dt * b.Velocity;


				b.AngularMomentum = b.GetInverseInertiaWorld().MultiplyVector(b.Torque);

				b.AngularVelocity *= 0.99f;
				b.AngularVelocity += b.AngularMomentum * dt;

                //b.AngularMomentum = b.AngularMomentum * 0.99f;
                //b.AngularMomentum += dt * (b.Torque);

                //b.AngularVelocity = b.GetInverseInertiaWorld() * b.AngularMomentum;//
                //b.AngularVelocity += dt * (b.GetInverseInertiaWorld().MultiplyVector(b.Torque));


                b.Transform.orientation = NormalizeQuaternion(AddScaledVectorToQuaternion(b.Transform.orientation, b.AngularVelocity, dt));

                // Reset force and torque.
                b.Force  = Vector3.zero;
                b.Torque = Vector3.zero;


				// Awake/sleep control.
				{
					float current_motion = b.Velocity.sqrMagnitude + b.AngularVelocity.sqrMagnitude;

					float bias = Mathf.Pow(0.5f, dt);
					b.Motion = b.Motion * bias + (1.0f - bias) * current_motion;


					if(b.Motion < Body.SleepEpsilon)
						b.IsAwake = false;
					else if(b.Motion > 10.0f * Body.SleepEpsilon)
						b.Motion = 10.0f * Body.SleepEpsilon;
				}

				// Unity sync.
                GlobalDebugRenderer.Instance.AddCommand(new DebugSphereCommand(t.position, b.Shape.GetBoundingSphere().Radius, true, b.IsAwake ? Color.green : Color.yellow));

                b.UpdateTransforms(); // Update the matrices.
            }
		}
    }
}
