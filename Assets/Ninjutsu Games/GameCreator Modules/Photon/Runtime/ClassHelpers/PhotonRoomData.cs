using System.Collections;
using System.Collections.Generic;
using GameCreator.Variables;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace NJG.PUN
{
	[System.Serializable]
	public class PhotonRoomData
	{
		[System.Serializable]
		public class VariableRoomData
		{
			public enum VariableType
			{
				String = 1,
				Number = 2,
				Bool = 3
			}
			public string key;
			public VariableType target = VariableType.String;
			public NumberProperty numberProperty = new NumberProperty();
			public StringProperty stringProperty = new StringProperty();
			public BoolProperty boolProperty = new BoolProperty();
		}

		public string title;
		public VariableRoomData[] customData = new VariableRoomData[0];

		public PhotonRoomData(string title)
		{
			this.title = title;
		}

		public Hashtable ToHashtable(GameObject invoker)
		{
			int max = customData.Length;
			Hashtable table = new Hashtable();
			for (int i = 0; i < max; i++)
			{
				var data = customData[i];
				switch (data.target)
				{
					case VariableRoomData.VariableType.Bool: table.Add(data.key, data.boolProperty.GetValue(invoker)); break;
					case VariableRoomData.VariableType.Number: table.Add(data.key, data.numberProperty.GetValue(invoker)); break;
					case VariableRoomData.VariableType.String: table.Add(data.key, data.stringProperty.GetValue(invoker)); break;
				}
			}
			return table;
		}

		public string[] GetKeys()
		{
			string[] keys = new string[customData.Length];
			for (int i = 0; i < customData.Length; i++)
			{
				keys[i] = customData[i].key;
			}
			return keys;
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
					case VariableRoomData.VariableType.Bool: result += $" Data:{data.boolProperty.GetValue(null)}"; break;
					case VariableRoomData.VariableType.Number: result += $" Data:{data.numberProperty.GetValue(null)}"; break;
					case VariableRoomData.VariableType.String: result += $" Data:{data.stringProperty.GetValue(null)}"; break;
				}
			}
			return result;
		}
	}
}
