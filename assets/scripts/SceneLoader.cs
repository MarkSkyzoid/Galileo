// Author: Marco Vallario.
// Loads a scene from an XML file.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace Galileo
{
	public static class SceneLoader  
	{
		private static Vector3 Vector3FromString(string vec3_string) 
		{
			Vector3 outVector3;
			
			string[] splitString = vec3_string.Split("," [0]);
			
			outVector3.x = float.Parse(splitString[0]);
			outVector3.y = float.Parse(splitString[1]);
			outVector3.z = float.Parse(splitString[2]);
			
			return outVector3;
		}


		public static bool LoadScene(string resource_path, PhysicsSimulation sim)
		{
			bool result = true;

			//TextAsset xml_file = Resources.Load (resource_path) as TextAsset;
		
		
			XmlDocument xml_doc = new XmlDocument ();
			xml_doc.Load (resource_path);
			XmlNodeList scene_list = xml_doc.GetElementsByTagName ("scene");

			foreach(XmlNode scene_info in scene_list)
			{
				XmlAttribute gravity_attribute = scene_info.Attributes["gravity"];
				if(gravity_attribute != null)
				{
					Vector3 gravity = Vector3FromString(gravity_attribute.Value);
					sim.ChangeWorldGravity(gravity);
				}
				else
				{
					sim.ChangeWorldGravity(PhysicsWorld.DefaultGravity);
				}

				XmlNodeList scene_content = scene_info.ChildNodes;

				foreach(XmlNode obj in scene_content)
				{
					PhysicsSimulation.EObjectType type = PhysicsSimulation.EObjectType.e_Plane;

					switch(obj.Name)
					{
					case "box" : type = PhysicsSimulation.EObjectType.e_Cube; break;
					case "sphere": type = PhysicsSimulation.EObjectType.e_Sphere; break;
					case "plane": default: break;
					}

					Galileo.Material mat = Galileo.Material.Static;
					if(obj.Attributes["material"] != null)
					{
						switch(obj.Attributes["material"].Value)
						{
						case "rubber": mat = Galileo.Material.Rubber; break;
						case "rock": mat = Galileo.Material.Rock; break;
						case "static": mat = Galileo.Material.Static; break;
						case "pillow": mat = Galileo.Material.Pillow; break;
						case "metal": mat = Galileo.Material.Metal; break;
						case "wood": mat = Galileo.Material.Wood; break;
						default: break;
						}
					}

					Vector3 pos = Vector3.zero;
					if(obj.Attributes["position"] != null)
						pos = Vector3FromString(obj.Attributes["position"].Value);

					Vector3 normal = Vector3.up;
					if(obj.Attributes["normal"] != null)
						normal = Vector3FromString(obj.Attributes["normal"].Value);

					float radius = 0.5f;
					Vector3 half_extents = new Vector3(0.5f, 0.5f, 0.5f);

					if(obj.Attributes["halfSize"] != null)
						half_extents = Vector3FromString(obj.Attributes["halfSize"].Value);

					if(obj.Attributes["radius"] != null)
						radius = float.Parse(obj.Attributes["radius"].Value);

					sim.AddObject(type, pos, normal, mat, half_extents, radius);
				}
			}
			return result;
		}
	
	}
}