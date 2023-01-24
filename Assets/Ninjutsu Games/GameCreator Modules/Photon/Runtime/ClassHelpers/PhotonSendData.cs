using System.Collections;
using System.Collections.Generic;
using GameCreator.Variables;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace NJG.PUN
{
	[System.Serializable]
	public class PhotonSendData
	{
		[System.Serializable]
		public class VariableData
		{
			public enum VariableType
			{
				String = 1,
				Number = 2,
				Bool = 3,
				Color = 4,
				Vector2 = 5,
				Vector3 = 6,
			}
			public VariableType target = VariableType.Number;
			public NumberProperty numberProperty = new NumberProperty();
			public StringProperty stringProperty = new StringProperty();
			public BoolProperty boolProperty = new BoolProperty();
			public Vector3Property vector3Property = new Vector3Property();
			public Vector2Property vector2Property = new Vector2Property();
			public ColorProperty colorProperty = new ColorProperty();
		}
		public VariableData[] customData = new VariableData[0];

		public object[] ToArray(GameObject invoker)
		{
			int max = customData.Length;
			object[] table = new object[max];

			for (int i = 0; i < max; i++)
			{
				var data = customData[i];
				switch (data.target)
				{
					case VariableData.VariableType.Bool: table[i] = data.boolProperty.GetValue(invoker); break;
					// case VariableData.VariableType.Color: table[i] = data.colorProperty.GetValue(invoker); break;
					case VariableData.VariableType.Color: table[i] = ColorUtility.ToHtmlStringRGBA(data.colorProperty.GetValue(invoker)); break;
					case VariableData.VariableType.Number: table[i] = data.numberProperty.GetValue(invoker); break;
					case VariableData.VariableType.String: table[i] = data.stringProperty.GetValue(invoker); break;
					case VariableData.VariableType.Vector2: table[i] = data.vector2Property.GetValue(invoker); break;
					case VariableData.VariableType.Vector3: table[i] = data.vector3Property.GetValue(invoker); break;
				}
			}
			return table;
		}

		public override string ToString()
		{
			string result = string.Empty;
			int max = customData.Length;

			for (int i = 0; i < max; i++)
			{
				var data = customData[i];
				result += $"# {i}. Type: {data.target}";
				switch (data.target)
				{
					case VariableData.VariableType.Bool: result += $" Data:{data.boolProperty.GetValue(null)}"; break;
					// case VariableData.VariableType.Color: table[i] = data.colorProperty.GetValue(invoker); break;
					case VariableData.VariableType.Color: result += $" Data:{ColorUtility.ToHtmlStringRGBA(data.colorProperty.GetValue(null))}"; break;
					case VariableData.VariableType.Number: result += $" Data:{data.numberProperty.GetValue(null)}"; break;
					case VariableData.VariableType.String: result += $" Data:{data.stringProperty.GetValue(null)}"; break;
					case VariableData.VariableType.Vector2: result += $" Data:{data.vector2Property.GetValue(null)}"; break;
					case VariableData.VariableType.Vector3: result += $" Data:{data.vector3Property.GetValue(null)}"; break;
				}
			}
			return result;
		}
	}
}
