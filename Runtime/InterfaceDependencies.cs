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
    
    public class TargetObjectNotFoundException : Exception {
        
    }

    [Serializable]
    public class InterfaceDependencies : ISerializationCallbackReceiver {
        
        // drawer support
        public bool isFoldout = true;
        
        public int validationLevel;
        
        // Unity Object that contains the target object interface fields of which we want to serialize/deserialize 
        [SerializeField] Object target;
        // Nested path of the field that holds reference to the target object inside the Unity Object 
        [SerializeField] string targetPath;
        // Names of the fields inside the target object 
        [SerializeField] string[] fieldNames;
        // Assembly qualified names of the types for each of the fields we want to serialize
        [SerializeField] string[] fieldTypes;
        // Mapped Unity Objects assigned to the fields
        [SerializeField] Object[] mappedObjects;
        // TODO:
        // Mapped paths to references of each field type inside each mapped Unity Object
        [SerializeField] string[] mappedPaths;
        // Sources of each mapped Unity Object (scene or assets)
        [SerializeField] ReferenceSource[] mappedSources;

        /// <summary>
        /// Create new InterfaceDependencies object
        /// </summary>
        /// <param name="target">target Unity Object</param>
        /// <param name="iDepsPath">path of the InterfaceDependencies field (incl. field's name) inside the target Unity Object</param>
        /// <param name="validationLevel">level of validation to be performed for each mapped field on deserialization</param>
        public InterfaceDependencies(Object target, string iDepsPath = "", int validationLevel = 0) {
            SetTarget(target, iDepsPath);
            this.validationLevel = validationLevel;
        }

        /// <summary>
        /// Set target Unity Object in which (can be nested) InterfaceDependencies field reside. 
        /// </summary>
        /// <param name="target">target Unity Object</param>
        /// <param name="iDepsPath">path of the InterfaceDependencies field (incl. field's name) inside the target Unity Object</param>
        internal void SetTarget(Object target, string iDepsPath) {
            this.target = target;
            targetPath = GetTargetObjectPath(iDepsPath);
        }

        public void OnBeforeSerialize() {
            // This might be running not on the main thread, need to avoid `== null` comparisons
            try {
                var targetObj = GetTargetObject(target, targetPath);
                Serialize(targetObj);
            } 
            #pragma warning disable CS0168
            catch (NullReferenceException e) {
                // pass
            }
            #pragma warning restore CS0168
        }
        
        public void OnAfterDeserialize() {
            // This might be running not on the main thread, need to avoid `== null` comparisons
            try {
                var targetObj = GetTargetObject(target, targetPath);
                Deserialize(targetObj);
            }
            #pragma warning disable CS0168
            catch (NullReferenceException e) {
                // pass
            }
            #pragma warning restore CS0168
        }

        /// <summary>
        /// Determines the path to object containing InterfaceDependencies inside the target Unity Object
        /// </summary>
        /// <param name="iDepsPath">path of the InterfaceDependencies field</param>
        public static string GetTargetObjectPath(string iDepsPath) {
            var idx = iDepsPath.LastIndexOf('.');
            return idx < 0 ? null : iDepsPath[..idx];
        }

        /// <summary>
        /// Gets the object (that contains InterfaceDependencies) inside the Unity Object based on the field path.
        /// If field path is null or empty, returns the Unity Object itself.
        /// </summary>
        /// <param name="target">Unity Object</param>
        /// <param name="targetPath">field path inside the Unity Object where actual target object is</param>
        /// <returns>object</returns>
        public static object GetTargetObject(Object target, string targetPath) {
            if (string.IsNullOrEmpty(targetPath)) {
                return target;
            }
            
            var splitPath = targetPath.Split('.');
            var targetObj = (object) target;
            var targetObjType = target.GetType();
            foreach (var fieldName in splitPath) {
                var field = targetObjType.GetField(fieldName, Utils.ALL_INSTANCE_FIELDS);
                if (field == null) {
                    return null;
                }
                targetObj = field.GetValue(targetObj);
                targetObjType = field.GetType();
            }
            return targetObj;
        }
        
        /// <summary>
        /// Gets the object (that contains InterfaceDependencies) inside the Unity Object based on the field path
        /// of the InterfaceDependencies field (includes field itself).
        /// If field path is null, empty or contains only name of InterfaceDependency field itself, returns the provided Unity Object itself.
        /// </summary>
        /// <param name="target">Unity Object</param>
        /// <param name="targetPath">field path inside the Unity Object where actual target object is</param>
        /// <returns>object</returns>
        public static object GetTargetObjectRelativeToIDeps(Object target, string iDepsPath) {
            return GetTargetObject(target, GetTargetObjectPath(iDepsPath));
        }

        void InitData(int count) {
            fieldNames = new string[count];
            fieldTypes = new string[count];
            mappedObjects = new Object[count];
            mappedSources = new ReferenceSource[count];
        }

        void Serialize(object obj) {
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
        
        void Deserialize(object obj) {
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
