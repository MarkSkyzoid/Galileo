using UnityEngine;
using System.Collections;

namespace Galileo
{
    public static class ContactResolver
    {
		private static void ResolveVelocities(Contact contact, int iterations)
		{
			Body a = contact.A;
			Body b = contact.B;

			for(int c = 0; c < contact.ContactsCount; ++ c)
			{
				if(a.MassInfo.InverseMass + b.MassInfo.InverseMass == 0.0f)
					return;
				
				Vector3 ra = contact.Point[c] - a.Transform.position;
				Vector3 rb = contact.Point[c] - b.Transform.position;
				
				Vector3 va = Mathf.Equals(a.MassInfo.InverseMass, 0.0f) ? Vector3.zero : a.Velocity + Vector3.Cross (a.AngularVelocity, ra);
				Vector3 vb = Mathf.Equals(b.MassInfo.InverseMass, 0.0f) ? Vector3.zero : b.Velocity + Vector3.Cross (b.AngularVelocity, rb);
				Vector3 dv = va - vb;
				
				float separating_velocity = Vector3.Dot (dv, contact.Normal[c]);

				float jn = 0.0f;

				if(separating_velocity > 0.0f)
					return;
				
				{
					float e = (a.MaterialInfo.Restitution + b.MaterialInfo.Restitution) * 0.5f;
					
					float linear_norm_div = contact.Normal[c].sqrMagnitude * (a.MassInfo.InverseMass + b.MassInfo.InverseMass);
					
					Vector3 a_angular_norm_div = Vector3.zero;
					if(Mathf.Equals(a.MassInfo.InverseMass, 0.0f) == false)
					{
						Vector3 a_cross_n = Vector3.Cross(ra, contact.Normal[c]);
						a_cross_n = a.MassInfo.InverseInertia.MultiplyVector(a_cross_n);
						a_angular_norm_div = Vector3.Cross(a_cross_n, ra);
					}
					
					Vector3 b_angular_norm_div = Vector3.zero;
					if(Mathf.Equals(b.MassInfo.InverseMass, 0.0f) == false)
					{
						Vector3 b_cross_n = Vector3.Cross(rb, contact.Normal[c]);
						b_cross_n = b.MassInfo.InverseInertia.MultiplyVector(b_cross_n);
						b_angular_norm_div = Vector3.Cross(b_cross_n, rb);
					}
					
					float angular_norm_div = Vector3.Dot(a_angular_norm_div + b_angular_norm_div, contact.Normal[c]);

					
					float norm_div = linear_norm_div + angular_norm_div + 0.000001f;
					jn = -1.0f * (1.0f + e) * Vector3.Dot (dv, contact.Normal[c]) / norm_div;
					jn /= contact.ContactsCount;

					if(Mathf.Equals(a.MassInfo.InverseMass, 0.0f) == false)
					{
						a.Velocity = a.Velocity + contact.Normal[c] * (jn * a.MassInfo.InverseMass);

						Matrix3x3 inverse_inertia_world = a.GetInverseInertiaWorld();
					
						Vector3 impulsive_torque = Vector3.Cross(ra, contact.Normal[c] * (jn));
						Vector3 angular_change = inverse_inertia_world.MultiplyVector(impulsive_torque);
					
						a.AngularVelocity += angular_change;
					}
					if(Mathf.Equals(b.MassInfo.InverseMass, 0.0f) == false)
					{
						b.Velocity = b.Velocity - contact.Normal[c] * (jn * b.MassInfo.InverseMass);

						Matrix3x3 inverse_inertia_world = b.GetInverseInertiaWorld();

						Vector3 impulsive_torque = Vector3.Cross(contact.Normal[c] * (jn), rb);
						Vector3 angular_change = inverse_inertia_world.MultiplyVector(impulsive_torque);
						
						b.AngularVelocity += angular_change;
					}
				}

				{
					Vector3 tangent = dv - contact.Normal[c] * Vector3.Dot (dv, contact.Normal[c]);
					tangent.Normalize();

					float linear_tang_div = (a.MassInfo.InverseMass + b.MassInfo.InverseMass);
					
					Vector3 a_angular_tang_div = Vector3.zero;
					if(Mathf.Equals(a.MassInfo.InverseMass, 0.0f) == false)
					{
						Vector3 a_cross_t = Vector3.Cross(ra, tangent);
						a_cross_t = a.MassInfo.InverseInertia.MultiplyVector(a_cross_t);
						a_angular_tang_div = Vector3.Cross(a_cross_t, ra);
					}
					
					Vector3 b_angular_tang_div = Vector3.zero;
					if(Mathf.Equals(b.MassInfo.InverseMass, 0.0f) == false)
					{
						Vector3 b_cross_t = Vector3.Cross(rb, tangent);
						b_cross_t = b.MassInfo.InverseInertia.MultiplyVector(b_cross_t);
						b_angular_tang_div = Vector3.Cross(b_cross_t, rb);
					}

					float angular_tang_div = Vector3.Dot(a_angular_tang_div + b_angular_tang_div, tangent);
					
					
					float tang_div = linear_tang_div + angular_tang_div;
					if(Mathf.Equals(tang_div, 0.0f) == false)
					{
						float jt = -1.0f * Vector3.Dot (dv, tangent) / tang_div;

						float mu = (a.MaterialInfo.StaticFriction + b.MaterialInfo.StaticFriction) * 0.5f;//Mathf.Sqrt(a.MaterialInfo.StaticFriction * a.MaterialInfo.StaticFriction + b.MaterialInfo.StaticFriction * b.MaterialInfo.StaticFriction);

						Vector3 friction_impulse = Vector3.zero;
						if(Mathf.Abs(jt) < jn * mu)
						{
							friction_impulse = jt * tangent;
						}
						else
						{
							float dynamic_friction = (a.MaterialInfo.DynamicFriction + b.MaterialInfo.DynamicFriction) * 0.5f;//Mathf.Sqrt(a.MaterialInfo.DynamicFriction * a.MaterialInfo.DynamicFriction + b.MaterialInfo.DynamicFriction * b.MaterialInfo.DynamicFriction);
							friction_impulse = -jn * tangent * dynamic_friction;
						}

						if(friction_impulse.magnitude != 0.0f)
						{
							GlobalDebugRenderer.Instance.AddCommand(new DebugLineCommand(contact.Point[c], contact.Point[c] + friction_impulse * 5.0f, Color.green));
							GlobalDebugRenderer.Instance.AddCommand(new DebugLineCommand(contact.Point[c], contact.Point[c] + contact.Normal[c].normalized * 2.0f, Color.blue));
						}


						if(Mathf.Equals(a.MassInfo.InverseMass, 0.0f) == false)
						{
							a.Velocity = a.Velocity + (friction_impulse * a.MassInfo.InverseMass);
							a.AngularVelocity = a.AngularVelocity + (a.GetInverseInertiaWorld().MultiplyVector(Vector3.Cross(ra, friction_impulse)));
						}
						if(Mathf.Equals(b.MassInfo.InverseMass, 0.0f) == false)
						{
							b.Velocity = b.Velocity - (friction_impulse * b.MassInfo.InverseMass);
							b.AngularVelocity = b.AngularVelocity - (b.GetInverseInertiaWorld().MultiplyVector(Vector3.Cross(rb, friction_impulse)));
						}
					}
				}
			}
		}

		private static void ResolvePositions(Contact contact, int iterations)
		{
			const float k_angular_limit = 0.0004f;
			Body[] bodies = new Body[2] {contact.A, contact.B};
			float[] signs = new float[2] {1.0f, -1.0f};

			for(int i = 0; i < 2 ; ++i) 
			{
				Body a = bodies[i];
				float sign = signs[i];

				for(int c = 0; c < contact.ContactsCount; ++c)
				{
					Vector3 ra = contact.Point[c] - a.Transform.position;
					if(Mathf.Equals(a.MassInfo.InverseMass, 0.0f) == false)
					{
						// Angular inertia.
						Matrix3x3 inverse_inertia_world = a.GetInverseInertiaWorld();
						Vector3 angular_inertia_world = Vector3.Cross(ra, contact.Normal[c]);
						angular_inertia_world = inverse_inertia_world.MultiplyVector(angular_inertia_world);
						angular_inertia_world = Vector3.Cross(angular_inertia_world, ra);
						float angular_inertia = Vector3.Dot(angular_inertia_world, contact.Normal[c]);
						
						//Linear inertia.
						float linear_inertia = a.MassInfo.InverseMass;
						
						float total_inertia = linear_inertia + angular_inertia;

						float inverse_inertia = Mathf.Equals(total_inertia, 0.0f) ? 0.0f : 1.0f / total_inertia;
						float linear_move = sign * contact.Penetration[c] * linear_inertia * inverse_inertia;

						float angular_move = sign* contact.Penetration[c] * angular_inertia * inverse_inertia;

						Vector3 projection = ra + (contact.Normal[c] * Vector3.Dot(-ra, contact.Normal[c]));
						float max_magnitude = projection.magnitude * k_angular_limit;
						if(angular_move < -max_magnitude)
						{
							float total_move = angular_move + linear_move;
							angular_move = -max_magnitude;
							linear_move = total_move - angular_move;
						}
						else if(angular_move > max_magnitude)
						{
							float total_move = angular_move + linear_move;
							angular_move = max_magnitude;
							linear_move = total_move - angular_move;
						}

						Vector3 angular_change = Vector3.zero;
						if(Mathf.Equals(angular_move, 0.0f) == false)
						{
							Vector3 impulsive_torque = Vector3.Cross(ra, contact.Normal[c]);
							angular_change = Mathf.Equals(angular_inertia, 0.0f) ? Vector3.zero : inverse_inertia_world.MultiplyVector(impulsive_torque) * (angular_move / angular_inertia) / contact.ContactsCount;
						}

						a.Transform.position += contact.Normal[c] * linear_move / contact.ContactsCount;
						a.Transform.orientation = PhysicsWorld.NormalizeQuaternion(PhysicsWorld.AddScaledVectorToQuaternion(a.Transform.orientation, angular_change, 1.0f));
					}
				}
			}
		}

		private static void ResolveAwakeState(Contact contact)
		{
			Body a = contact.A;
			Body b = contact.B;

			if (a == null || b == null)
				return;

			// Collisions with static objects shouldn't affect the awake state.
			if (Mathf.Equals (a.MassInfo.InverseMass, 0.0f) || Mathf.Equals (b.MassInfo.InverseMass, 0.0f))
				return;

			bool a_awake = a.IsAwake;
			bool b_awake = b.IsAwake;

			// If the awake states are different, wake up the sleeping object.
			if(a_awake ^ b_awake)
			{
				if(a_awake)
					b.IsAwake = true;
				else
					a.IsAwake = true;
			}
		}

        public static void ResolveContact(Contact contact, int iterations)
        {
			ResolveAwakeState (contact);
			ResolveVelocities(contact, iterations);
			ResolvePositions (contact, iterations);
        }
    }
}