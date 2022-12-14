// MIT License
//
// Copyright (c) 2022 Nick Tsygankov
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI {
    
    [Serializable]
    public class InterfaceDependencies : ISerializationCallbackReceiver {
        
        // drawer support
        public bool isFoldout = true;
        
        public int validationLevel;
        
        [SerializeField] Object target;
        [SerializeField] string[] fieldNames;
        [SerializeField] string[] fieldTypes;
        [SerializeField] Object[] mappedObjects;
        [SerializeField] ReferenceSource[] mappedSources;

        public InterfaceDependencies(Object target, int validationLevel = 0) {
            this.target = target;
            this.validationLevel = validationLevel;
        }

        internal void SetParent(Object target) {
            this.target = target;
        }

        public void OnBeforeSerialize() {
            if (target != null)
                Serialize(target);
        }
        
        public void OnAfterDeserialize() {
            if (target != null)
                Deserialize(target);
        }

        public void InitData(int count) {
            fieldNames = new string[count];
            fieldTypes = new string[count];
            mappedObjects = new Object[count];
            mappedSources = new ReferenceSource[count];
        }

        public void Serialize(object obj) {
            var fieldsArr = obj.GetType().GetInterfaceFields().ToArray();
            var count = fieldsArr.Length;
            
            InitData(count);
            for (var i = 0; i < count; i++) {
                var field = fieldsArr[i];
                fieldNames[i] = field.Name;
                fieldTypes[i] = validationLevel > 0
                    ? field.GetType().AssemblyQualifiedName
                    : null;
        
                var unityObj = field.GetValue(obj) as Object;
                mappedObjects[i] = unityObj;
        
                var source = ReferenceSource.NONE;
                if (unityObj is ScriptableObject) {
                    source = ReferenceSource.ASSET;
                } else if (unityObj is Component) {
                    source = ReferenceSource.SCENE;
                }
                mappedSources[i] = source;
            }
        }
        
        public void Deserialize(object obj) {
            var dict = obj.GetType()
                .GetInterfaceFields().ToDictionary(f => f.Name);
            var count = fieldNames.Length;

            for (var i = 0; i < count; i++) {
                var field = dict.GetValueOrDefault(fieldNames[i]);
                if (field == null)
                    continue;

                if (!ValidateType(validationLevel, field, fieldTypes[i])) {
                    Debug.LogWarning($"Failed to validate interface field '{fieldNames[i]}' mapping in ${obj}");
                    continue;
                }
                
                // Unity doesn't serialize nulls directly, so if serialized value a special "null" object,
                //  catch the exception
                try {
                    field.SetValue(obj, mappedObjects[i]);
                } 
                #pragma warning disable CS0168
                catch (ArgumentException e) {
                    // pass
                }
                #pragma warning restore CS0168
            }
        }

        /// <summary>
        /// Validates if serialized type matches the type of the field
        /// </summary>
        /// <param name="level">
        ///    0 - no type validation <br/>
        ///    1 - quick validation (string comparison) <br/>
        ///    2 - full validation (find type and match)
        /// </param>
        /// <param name="field"></param>
        /// <param name="typeName"></param>
        /// <returns>true if types match; false otherwise</returns>
        bool ValidateType(int level, FieldInfo field, string typeName) {
            return level switch {
                1 => field.GetType().AssemblyQualifiedName == typeName,
                2 => field.GetType().IsEquivalentTo(Type.GetType(typeName)),
                _ => true,
            };
        }
        
        
    }
}
