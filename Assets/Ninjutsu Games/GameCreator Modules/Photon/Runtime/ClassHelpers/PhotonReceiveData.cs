using System.Collections;
using System.Collections.Generic;
using GameCreator.Variables;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace NJG.PUN
{
    [System.Serializable]
    public class PhotonReceiveData
    {
        [System.Serializable]
        public class VariableData
        {
            // public bool debug;
            public PhotonSendData.VariableData.VariableType target;

            // [VariableFilter(Variable.DataType.Number, Variable.DataType.Bool, Variable.DataType.Color, 
            // 	Variable.DataType.String, Variable.DataType.Vector2, Variable.DataType.Vector3)]
            // [SerializeField] private VariableProperty variable = new VariableProperty(Variable.VarType.LocalVariable);

            [VariableFilter(Variable.DataType.Number)]
            public VariableProperty numberVariable = new VariableProperty();

            [VariableFilter(Variable.DataType.String)]
            public VariableProperty stringVariable = new VariableProperty();

            [VariableFilter(Variable.DataType.Bool)]
            public VariableProperty boolVariable = new VariableProperty();

            [VariableFilter(Variable.DataType.Vector3)]
            public VariableProperty vector3Variable = new VariableProperty();

            [VariableFilter(Variable.DataType.Vector2)]
            public VariableProperty vector2Variable = new VariableProperty();

            [VariableFilter(Variable.DataType.Color)]
            public VariableProperty colorVariable = new VariableProperty();

            // [SerializeField] private PhotonCustomData.VariableData.VariableType target;

            public VariableData()
            {
                // variable = new VariableProperty(Variable.VarType.LocalVariable);
                // variable.global = new HelperGlobalVariable();
                // variable.local = new HelperLocalVariable();
                // variable.list = new HelperGetListVariable();
                var variable = GetVariable();
                if(variable == null) SetupVariable();
                // variable = GetVariable();
                // Debug.LogFormat("Variable Data {0} target: {1}",variable, target);

                // if(variable == null) return;
                // GetVariable().global = new HelperGlobalVariable();
                // GetVariable().global.allowTypesMask |= (1 << (int) target);
            }

            private void SetupVariable()
            {
                switch (target)
                {
                    case PhotonSendData.VariableData.VariableType.Bool:
                        boolVariable = new VariableProperty();
                        PostSetup(boolVariable);
                        break;
                    case PhotonSendData.VariableData.VariableType.Color:
                        colorVariable = new VariableProperty();
                        colorVariable.Set(Color.white);
                        PostSetup(colorVariable);

                        break;
                    case PhotonSendData.VariableData.VariableType.Number:
                        numberVariable = new VariableProperty();
                        PostSetup(numberVariable);

                        break;
                    case PhotonSendData.VariableData.VariableType.String:
                        stringVariable = new VariableProperty();
                        PostSetup(stringVariable);

                        break;
                    case PhotonSendData.VariableData.VariableType.Vector2:
                        vector2Variable = new VariableProperty();
                        PostSetup(vector2Variable);

                        break;
                    case PhotonSendData.VariableData.VariableType.Vector3:
                        vector3Variable = new VariableProperty();
                        PostSetup(vector3Variable);

                        break;
                }
                
                
            }

            private void PostSetup(VariableProperty prop)
            {
                prop.global = new HelperGlobalVariable();
                prop.global.allowTypesMask |= (1 << (int) target);
                
                prop.local = new HelperLocalVariable();
                prop.local.allowTypesMask |= (1 << (int) target);
            }

            public VariableProperty GetVariable()
            {
                switch (target)
                {
                    case PhotonSendData.VariableData.VariableType.Bool: return boolVariable;
                    case PhotonSendData.VariableData.VariableType.Color: return colorVariable;
                    case PhotonSendData.VariableData.VariableType.Number: return numberVariable;
                    case PhotonSendData.VariableData.VariableType.String: return stringVariable;
                    case PhotonSendData.VariableData.VariableType.Vector2: return vector2Variable;
                    case PhotonSendData.VariableData.VariableType.Vector3: return vector3Variable;
                }

                return null;
            }
        }

        public VariableData[] customData = new VariableData[0];

        public void FromObject(GameObject invoker, object[] instantiationData)
        {
            bool warning = false;
            try
            {
                int max = customData.Length;
                for (int i = 0; i < max; i++)
                {
                    var data = customData[i];
                    var variable = data.GetVariable();
                    if (variable == null)
                    {
                        warning = true;
                        continue;
                    }

                    var varType = variable.GetVariableDataType(invoker);
                    Debug.LogWarningFormat("ReceivedData {0} = {1} type: {2}", i, instantiationData[i], varType);
                    if (varType == Variable.DataType.Color)
                    {
                        Color color = Color.white;
                        bool parsed = ColorUtility.TryParseHtmlString(instantiationData[i].ToString(), out color);
                        Debug.LogWarningFormat("Parsing color: {0} = {1} parsed: {2}", instantiationData[i], color, parsed);

                        variable.Set(color, invoker);
                    }
                    else variable.Set(instantiationData[i], invoker);
                }
            }
            catch(System.Exception)
            {
                warning = true;
            }

            if (warning)
            {
                Debug.LogWarning("Your PhotonInstantiate action is sending custom data but your On Photon Instantiate trigger doesn't match the variables configuration.");
            }
        }
        
        public string ToString(object[] instantiationData)
        {
            string result = string.Empty;
            int max = customData.Length;

            for (int i = 0; i < max; i++)
            {
                var data = customData[i];
                var variable = data.GetVariable();
                var varType = variable.GetVariableDataType(null);
                result += $"# {i}. Type: {varType} Data: {instantiationData[i]}";
            }
            return result;
        }
    }
}