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
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI {
    

    [Serializable]
    internal struct IFaceFieldInfo {
        // Name of the field inside the target object 
        public string name;
        // Assembly qualified name of the field type
        public string type;
        // Category of the field type
        public IFaceFieldCategory category;
        // lenght of the collection (if category is list or an array)
        public int length;
        // if it is an array or list type specifies if it should be initialized (even if length is 0)
        public bool isInit;
        
        public IFaceFieldInfo(string name, string type, IFaceFieldCategory category, int length, bool isInit) {
            this.name = name;
            this.type = type;
            this.category = category;
            this.length = length;
            this.isInit = isInit;
        }

        [CanBeNull]
        public Type Type {
            get {
                try {
                    return Type.GetType(type);
                } catch {
                    return null;
                }
            }
        }

        public bool IsSingular => category == IFaceFieldCategory.SINGULAR;

        public override string ToString() {
            return $"IFaceFieldInfo {{ name={name}, category={category}, length={length}, isInit={isInit} type={type} }}";
        }
    }

    [Serializable]
    public class InterfaceDependencies : ISerializationCallbackReceiver {
        
        static readonly string ARRAY_DATA = "Array.data[";
        static readonly string BRACKET = "[";
        static readonly char BRACKET_CHAR = '[';
        
        [NonSerialized] object serializeLock = new object();
        
        // drawer support
        public bool isFoldout = true;
        
        public int validationLevel;
        
        // Unity Object that contains the target object interface fields of which we want to serialize/deserialize 
        [SerializeField] Object target;
        // Nested path of the field that holds reference to the target object inside the Unity Object 
        [SerializeField] string targetPath;
        // Info about each field we want to serialize
        [SerializeField] IFaceFieldInfo[] fieldInfos;
        // Mapped Unity Objects assigned to the fields (or each element of each field if fields are collections)
        [SerializeField] Object[] mappedObjects;
        
        // TODO: do we need these?
        // Mapped paths to references of each field type inside each mapped Unity Object
        // [SerializeField] string[] mappedPaths;
        // Sources of each mapped Unity Object (scene or assets)
        // [SerializeField] ReferenceSource[] mappedSources;

        /// <summary>
        /// Create new InterfaceDependencies object
        /// </summary>
        /// <param name="target">target Unity Object</param>
        /// <param name="iDepsPath">path of the InterfaceDependencies field (incl. field's name) inside the target Unity Object</param>
        /// <param name="validationLevel">level of validation to be performed for each mapped field on deserialization</param>
        public InterfaceDependencies(Object target, string iDepsPath = "", int validationLevel = 0) {
            Debug.Log("Is this ever called?");
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
            EnsureLockExists();
            // This might be running not on the main thread, need to avoid `== null` comparisons
            // Debug.Log($"{target} serialize: ");
            try {
                lock (serializeLock) {
                    var targetObj = GetTargetObject(target, targetPath);
                    Serialize(targetObj);    
                }
            } catch (Exception e) {
                Debug.Log($"Serialize fail: {e}");
                // ignored
            }
        }
        
        public void OnAfterDeserialize() {
            EnsureLockExists();
            // This might be running not on the main thread, need to avoid `== null` comparisons
            try {
                lock (serializeLock) {
                    var targetObj = GetTargetObject(target, targetPath);
                    Deserialize(targetObj);
                }
            } catch (Exception e) {
                Debug.Log($"Deserialize fail: {e}");
                // ignored
            }
        }

        // -------------------------------------- //
        #region Static Methods

        void EnsureLockExists() {
            if (serializeLock == null) {
                serializeLock = new object();
            }
        }

        /// <summary>
        /// Determines the path to object containing InterfaceDependencies inside the target Unity Object
        /// </summary>
        /// <param name="iDepsPath">path of the InterfaceDependencies field</param>
        internal static string GetTargetObjectPath(string iDepsPath) {
            if (string.IsNullOrEmpty(iDepsPath))
                return null;
            var idx = iDepsPath.LastIndexOf('.');
            return idx < 0
                ? null
                : iDepsPath[..idx].Replace(ARRAY_DATA, BRACKET);
        }

        /// <summary>
        /// Gets the object (that contains InterfaceDependencies) inside the Unity Object based on the field path.
        /// If field path is null or empty, returns the Unity Object itself.
        /// </summary>
        /// <param name="target">Unity Object</param>
        /// <param name="targetPath">field path inside the Unity Object where actual target object is</param>
        /// <returns>object</returns>
        internal static object GetTargetObject(Object target, string targetPath) {
            // do this first to trigger null exception if target is null
            Type targetObjType;
            try {
                targetObjType = target.GetType();
            } catch {
                return null;
            }

            if (string.IsNullOrEmpty(targetPath)) {
                return target;
            }
            
            var splitPath = targetPath.Split('.');
            var targetObj = (object) target;
            foreach (var fieldName in splitPath) {
                // if array or list index
                if (fieldName[0] == BRACKET_CHAR) {
                    try {
                        var idx = int.Parse(fieldName[1..^1]);
                        targetObj = (targetObj as IEnumerable<object>).ElementAt(idx);
                        targetObjType = targetObj.GetType();
                    } catch {
                        return null;
                    }
                }
                // if normal field
                else {
                    var field = targetObjType.GetField(fieldName, Utils.ALL_INSTANCE_FIELDS);
                    if (field == null) {
                        return null;
                    }
                    targetObj = field.GetValue(targetObj);
                    targetObjType = field.GetType();
                }
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
        internal static object GetTargetObjectRelativeToIDeps(Object target, string iDepsPath) {
            return GetTargetObject(target, GetTargetObjectPath(iDepsPath));
        }
        
        internal static IFaceFieldCategory GetIFaceFieldTypeCategory(Type type) {
            if (type.IsArray && type.GetElementType()!.IsInterface) {
                return IFaceFieldCategory.ARRAY;
            }
            if (type.IsList() && type.GetGenericArguments().FirstOrDefault()!.IsInterface) {
                return IFaceFieldCategory.LIST;
            }
            // TODO: support for more collections or collection interfaces?
            if (type.IsIEnumerable()) {
                return IFaceFieldCategory.UNSUPPORTED;
            }
            if (type.IsInterface) {
                return IFaceFieldCategory.SINGULAR;
            }
            return IFaceFieldCategory.UNSUPPORTED;
        }
        
        internal static IEnumerable<FieldInfo> GetCompatibleFields(Type type) {
            var fields = type.GetFields(Utils.ALL_INSTANCE_FIELDS);
            foreach (var field in fields) {
                var cat = GetIFaceFieldTypeCategory(field.FieldType);
                if (cat == IFaceFieldCategory.UNSUPPORTED)
                    continue;
                
                yield return field;
            }
        }
        
        #endregion

        // -------------------------------------- //
        #region Private Methods
        
        void Serialize(object obj) {
            var fieldsArr = GetCompatibleFields(obj.GetType()).ToArray();
            var count = fieldsArr.Length;

            // init data
            fieldInfos = new IFaceFieldInfo[count];
            var mappedObjectList = new List<Object>();
            
            for (var i = 0; i < count; i++) {
                var field = fieldsArr[i];
                var info = new IFaceFieldInfo();
                info.name = field.Name;
                info.type = field.FieldType.AssemblyQualifiedName;
                var cat = info.category = GetIFaceFieldTypeCategory(field.FieldType);

                var fieldVal = field.GetValue(obj);

                // if field is array or list
                if (cat is IFaceFieldCategory.ARRAY or IFaceFieldCategory.LIST) {
                    var list = new CollectionWrapper(field, obj);
                    info.length = list.Count;
                    info.isInit = list.Object != null;
                    mappedObjectList.AddRange(list.Select(item => item as Object));
                }
                // if field is singular
                else {
                     mappedObjectList.Add(fieldVal as Object);  
                }
                
                fieldInfos[i] = info;
            }
            
            // convert mapped object list to array
            mappedObjects = mappedObjectList.ToArray();
        }
        
        void Deserialize(object obj) {
            Dictionary<string, FieldInfo> dict;
            dict = GetCompatibleFields(obj.GetType()).ToDictionary(f => f.Name);
            if (fieldInfos == null) {
                Debug.Log("Why would this be null????");
                Debug.Log(Thread.CurrentThread.ManagedThreadId);
            }
            var count = fieldInfos.Length;

            var mappedIndex = 0;
            for (var i = 0; i < count; i++) {
                var info = fieldInfos[i];
                var field = dict.GetValueOrDefault(info.name);
                if (field == null)
                    continue;
            
                if (!ValidateType(validationLevel, field, info.type)) {
                    Debug.LogWarning($"Failed to validate interface field '{info.name}' type in ${obj}");
                    continue;
                }

                if (info.IsSingular) {
                    SetSingularValue(field, obj, mappedObjects[mappedIndex]);
                    mappedIndex++;
                } else {
                    // assign null if not supposed to be initialized
                    if (!info.isInit) {
                        SetSingularValue(field, obj, null);
                        continue;
                    }
                    
                    var list = new CollectionWrapper(field, obj);
                    list.Create(info.length);
                    for (var j = 0; j < info.length; j++) {
                        SetListItemValue(list, mappedObjects[mappedIndex], j);
                        mappedIndex++;
                    }
                }
            }
        }

        void SetSingularValue(FieldInfo field, object obj, Object value) {
            // Unity doesn't serialize nulls directly, so if serialized value a special "null" object,
            //  catch the exception
            try {
                field.SetValue(obj, value);
                return;
            } 
            #pragma warning disable CS0168
            catch (ArgumentException e) {
                // ignored
            }
            #pragma warning restore CS0168
            
            // retry with null
            field.SetValue(obj, null);
        }

        void SetListItemValue(CollectionWrapper list, Object value, int index) {
            // Unity doesn't serialize nulls directly, so if serialized value a special "null" object,
            //  catch the exception
            try {
                if (list.IsArray) {
                    list[index] = value;
                }
                else {
                    list.Add(value);
                }
                return;
            }
            #pragma warning disable CS0168
            catch (ArgumentException e) {
                // can be thrown when calling list.Add
            } catch (InvalidCastException e) {
                // can be thrown when assigning to array item
            } catch (UnityException e) {
                // can be thrown when calling list.Add
            }
            #pragma warning restore CS0168

            // retry with null
            if (list.IsArray) {
                list[index] = null;
            } else {
                list.Add(null);
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
        
        #endregion
    }
}
