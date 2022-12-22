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
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI {

    // internal class TargetNullException : Exception { }
    //
    // internal class ValidationFailedException : Exception {
    //     object target;
    //     string fieldName;
    //
    //     public ValidationFailedException(string fieldName, object target) {
    //         
    //     }
    // }

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
        
        public IFaceFieldInfo(string name, string type, IFaceFieldCategory category, int length = 0) {
            this.name = name;
            this.type = type;
            this.category = category;
            this.length = length;
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

        public bool IsArr => category == IFaceFieldCategory.ARRAY;
        
        public bool IsList => category == IFaceFieldCategory.LIST;
        
        public bool IsSingular => category == IFaceFieldCategory.SINGULAR;

        public override string ToString() {
            return $"IFaceFieldInfo {{ name={name}, category={category}, length={length}, type={type} }}";
        }
    }

    [Serializable]
    public class InterfaceDependencies : ISerializationCallbackReceiver {

        // static readonly Type ILIST_TYPE = typeof(IList<>);
        // static readonly PropertyInfo COUNT_PROP = ILIST_TYPE.GetProperty("Count");
        // static readonly PropertyInfo ITEM_PROP = ILIST_TYPE.GetProperty("Item");
        static readonly string COUNT_PROP = "Count";
        static readonly string ITEM_PROP = "Item";
        static readonly string ADD_METHOD = "Add";
        
        static readonly string ARRAY_DATA = "Array.data[";
        static readonly string BRACKET = "[";
        static readonly char BRACKET_CHAR = '[';
        
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
            } catch {
                // ignored
            }
        }
        
        public void OnAfterDeserialize() {
            // This might be running not on the main thread, need to avoid `== null` comparisons
            try {
                var targetObj = GetTargetObject(target, targetPath);
                Deserialize(targetObj);
            } catch {
                // ignored
            }
        }

        // -------------------------------------- //
        #region Static Methods
        
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

                // if field is array or collection
                if (cat != IFaceFieldCategory.SINGULAR) {
                    Array arr = null;
                    PropertyInfo itemProp = null;
                    if (cat == IFaceFieldCategory.ARRAY) {
                        arr = fieldVal as Array;
                        info.length = arr!.Length;
                    }
                    else if (cat == IFaceFieldCategory.LIST) {
                        var countProp = field.FieldType.GetProperty(COUNT_PROP);
                        info.length = (int) countProp!.GetValue(fieldVal);
                        itemProp =  field.FieldType.GetProperty(ITEM_PROP);
                    }

                    for (var j = 0; j < info.length; j++) {
                        indexArgs[0] = j;
                        var item = cat == IFaceFieldCategory.ARRAY 
                            ? arr!.GetValue(j)
                            : itemProp!.GetValue(fieldVal, indexArgs);
                        mappedObjectList.Add(item as Object);
                    }
                }
                // if field is singular
                else {
                     mappedObjectList.Add(fieldVal as Object);  
                }
                
                fieldInfos[i] = info;
                
                // var source = ReferenceSource.NONE;
                // if (unityObj is ScriptableObject) {
                //     source = ReferenceSource.ASSET;
                // } else if (unityObj is Component) {
                //     source = ReferenceSource.SCENE;
                // }
                // mappedSources[i] = source;
            }
            
            // convert mapped object list to array
            mappedObjects = mappedObjectList.ToArray();
        }

        object[] indexArgs = { 0 };
        object[] valueArgs = { null };
        
        void Deserialize(object obj) {
            Dictionary<string, FieldInfo> dict;
            dict = GetCompatibleFields(obj.GetType()).ToDictionary(f => f.Name);
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
                    var isArr = info.IsArr;
                    var list = Activator.CreateInstance(field.FieldType, info.length);
                    MethodInfo addMethod = isArr ? null : field.FieldType.GetMethod(ADD_METHOD);
                    
                    for (var j = 0; j < info.length; j++) {
                        SetListItemValue(addMethod, list, isArr, mappedObjects[mappedIndex], j);
                        mappedIndex++;
                    }
                    field.SetValue(obj, list);
                }
            }
        }

        void SetSingularValue(FieldInfo field, object obj, Object value) {
            // Unity doesn't serialize nulls directly, so if serialized value a special "null" object,
            //  catch the exception
            try {
                field.SetValue(obj, value);
            } 
            #pragma warning disable CS0168
            catch (ArgumentException e) {
                // ignored
            }
            #pragma warning restore CS0168
        }

        void SetListItemValue(MethodInfo addMethod, object list, bool isArr, Object value, int index) {
            // Unity doesn't serialize nulls directly, so if serialized value a special "null" object,
            //  catch the exception
            try {
                if (isArr) {
                    (list as Array)!.SetValue(value, index);
                } else {
                    valueArgs[0] = value;
                    addMethod.Invoke(list, valueArgs);
                }
            } 
            #pragma warning disable CS0168
            catch (ArgumentException e) {
                // ignored
            }
            #pragma warning restore CS0168
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
