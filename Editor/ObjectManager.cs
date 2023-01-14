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
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI.Editor {
    
    internal class ObjectManager {
        
        /// <summary>
        /// True if the target inspected object is persistent (stored on disk, like SO asset); false if it's a scene object
        /// </summary>
        public bool IsPersistent { get; }

        /// <summary>
        /// Unity Object target (target of the Inspector)
        /// </summary>
        public Object Target { get; private set; }

        /// <summary>
        /// Actual c# object which contains InterfaceDependencies field. Can be nested inside of `target` or `target` itself
        ///  depending on the `SerializedProperty.propertyPath` 
        /// </summary>
        public object ActualTarget { get; private set; }
        
        /// <summary>
        /// Caching the assigned values of the interface fields to avoid serialization / deserialization timing bugs
        /// </summary>
        Dictionary<FieldInfo, Object> fieldCache;

        public ObjectManager(Object target, string iDepsFieldPath) {
            Target = target;
            ActualTarget = InterfaceDependencies.GetTargetObjectRelativeToIDeps(target, iDepsFieldPath);

            IsPersistent = EditorUtility.IsPersistent(target);

            fieldCache = new Dictionary<FieldInfo, Object>();
        }

        ~ObjectManager() {
            Cleanup();
        }

        public void Cleanup() {
            Target = null;
            ActualTarget = null;
            fieldCache.Clear();
        }

        public InterfaceDependencies BindInterfaceDependencies(FieldInfo iDepsField, string iDepsFieldPath) {
            var obj = iDepsField.GetValue(ActualTarget);
            switch (obj) {
                case null:
                    obj = new InterfaceDependencies(Target, iDepsFieldPath);
                    iDepsField.SetValue(ActualTarget, obj);
                    break;
                case InterfaceDependencies iDeps:
                    iDeps.SetTarget(Target, iDepsFieldPath);
                    break;
            }
            return obj as InterfaceDependencies;
        }
        
        public void OpenObjectPicker(Type type, Action<Object> pickCallback) {
            InterfaceSearchProvider.OpenInterfacePicker(type, !IsPersistent, pickedObject => {
                var componentOrSO = Utils.FindComponentOrSO(type, pickedObject);
                pickCallback(componentOrSO);
            });
        }

        public void RecordUndo() {
            Undo.RecordObject(Target, "Set interface field ref");
        }

        public void RecordUndoHierarchy() {
            Undo.RegisterFullObjectHierarchyUndo(Target, "Set interface field list item");
        }

        public void SetObjectToField(FieldInfo field, Object pickedObj) {
            RecordUndo();
            field.SetValue(ActualTarget, pickedObj);
            
            fieldCache[field] = pickedObj;
        }

        public Object GetMappedObjectForField(FieldInfo field) {
            var obj = fieldCache.GetValueOrDefault(field);
            if (obj == null) {
                obj = field.GetValue(ActualTarget) as Object;
                fieldCache[field] = obj;
            }
            return obj;
        }
        
        public GUIContent GetContentFromObject(Object obj, Type type) {
            var typeName = type.GetNameWithGenerics();
            var text = obj == null 
                ? $"None ({typeName})" 
                : $"{obj.name} ({obj.GetType().Name}) ({typeName})";
            var img = obj == null
                ? null
                : EditorGUIUtility.GetIconForObject(obj);
            return new GUIContent(text, img);
        }

        
    }
}
