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
using UnityEditor;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI.Editor {
    
    [CustomPropertyDrawer(typeof(InterfaceDependencies))]
    internal class InterfaceDependenciesDrawer : PropertyDrawer, IDisposable {
        
        static Texture2D iconPick = (Texture2D) EditorGUIUtility.IconContent("d_pick").image;
        
        float verticalSpacing;
        float lineHeight;
        float totalHeight;
        float pickerWidth => lineHeight + 2f;
        float indentWidth => EditorGUI.indentLevel * 15;

        bool isInit;
        
        /// <summary>
        /// Unity Object target (target of the Inspector)
        /// </summary>
        Object target;
       
        /// <summary>
        /// Actual c# object which contains InterfaceDependencies field. Can be nested inside of `target` or `target` itself
        ///  depending on the `SerializedProperty.propertyPath` 
        /// </summary>
        object actualTarget;
        
        List<FieldInfo> iFaceFields;

        Event currentEvent;
        EventType currentEventType;

        SerializedProperty isFoldout;
        static readonly string isFoldoutPropName = "isFoldout";
        InterfaceDependencies iDeps;

        Object dndObject;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            isFoldout = property.FindPropertyRelative(isFoldoutPropName);

            // TODO: use this to check if we're editing asset or scene object
            // EditorUtility.IsPersistent();
            
            property.serializedObject.Update();

            Enable(property.serializedObject.targetObject, fieldInfo, property.propertyPath);

            DrawInterfaceDependenciesGUI(position);
            
            property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return totalHeight;
        }

        public void DrawInterfaceDependenciesGUI (Rect position) {
            ReadEvent();
            
            DrawDependenciesSectionGUI(position);
        }
        
        float CalculateDependencySectionHeight() {
            return lineHeight + verticalSpacing * 2 + 2 + iFaceFields.Count * (lineHeight + verticalSpacing);
        }

        void DrawDependenciesSectionGUI(Rect pos) {
            var startY = pos.y; // pos.yMin;
            
            pos.y += verticalSpacing;

            var sectionHeight = CalculateDependencySectionHeight();

            var foldout = isFoldout.boolValue;
            var foldoutPos = pos;
            foldoutPos.height = foldout ? sectionHeight : lineHeight;
            foldout = DrawFoldout(foldoutPos, foldout, "Dependencies");
            pos.y += lineHeight;
            
            if (foldout) {
                pos.y += DrawDependenciesSectionContentGUI(pos);
            }
            SaveFoldoutState(foldout);

            // EditorGUI.indentLevel = initIndentLevel;

            // update full height of the section
            totalHeight = pos.y - startY;
        }

        bool DrawFoldout(Rect pos, bool value, string label) {
            var id = GUIUtility.GetControlID(FocusType.Keyboard, pos);
            
            // adjust for indent
            var indent = indentWidth;
            var boxPos = pos;
            boxPos.x += indent;
            boxPos.width -= indent;
            GUI.Box(boxPos, GUIContent.none, EditorStyles.helpBox);

            var togglePos = pos;
            togglePos.x += 4 + indent;
            togglePos.width = lineHeight;
            togglePos.height = lineHeight;
            var result = EditorGUI.Toggle(togglePos, value, EditorStyles.foldout);
            // if toggles, bring focus back to whole foldout
            if (result != value) {
                GUIUtility.keyboardControl = id;
            } 
            
            var labelPos = pos;
            labelPos.x += 4 + lineHeight;
            labelPos.height = lineHeight;
            labelPos.width = pos.width - togglePos.width - 8;

            ProcessFocus(pos, id);

            // process click on the label
            HandleMouseDown(labelPos, mousePos => {
                result = !result;
            });

            // draw label
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            if (HasKBFocus(id)) {
                labelStyle.normal.textColor = EditorStyles.foldout.active.textColor;
            }
            EditorGUI.LabelField(labelPos, label, labelStyle);

            return result;
        }

        void SaveFoldoutState(bool foldout) {
            if (foldout == iDeps.isFoldout)
                return;
            // TODO: consider if this is necessary
            // EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            iDeps.isFoldout = foldout;
        }

        float DrawSeparatorLine(Rect line) {
            var indent = indentWidth;
            line.x += 15 + indent;
            line.width -= 16 + indent;
            line.height = 2.0f;
            EditorGUI.DrawRect(line, Color.gray);
            return line.height;
        }

        float DrawDependenciesSectionContentGUI(Rect pos) {
            var startY = pos.y;
            
            pos.y += verticalSpacing;
            pos.y += DrawSeparatorLine(pos);
            pos.y += verticalSpacing;
            
            EditorGUI.indentLevel += 1;
            
            for (var i = 0; i < iFaceFields.Count; i++) {
                var prop = iFaceFields[i];
                pos.y += DrawInterfacePropertyGUI(pos, i, prop, GetMappedObjectForField(prop));
                pos.y += verticalSpacing;
            }
            
            EditorGUI.indentLevel -= 1;
            
            return pos.y - startY;
        }
        
        float DrawInterfacePropertyGUI(Rect pos, int index, FieldInfo field, Object obj) {
            var id = GUIUtility.GetControlID(FocusType.Keyboard, pos);
            var startY = pos.y;

            pos.height = lineHeight;
    
            // process focus
            ProcessFocus(pos, id);
            
            // Draw label
            var label = ObjectNames.NicifyVariableName(field.Name);
            var labelPos = pos;
            var labelStyle = new GUIStyle(EditorStyles.label);
            
            if (HasKBFocus(id)) {
                labelStyle.normal = EditorStyles.label.focused;
            }
            var fieldPos = EditorGUI.PrefixLabel(labelPos, id, new GUIContent(label), labelStyle);
            labelPos.width = EditorGUIUtility.labelWidth;
            
            ProcessDragAndDrop(id, field, fieldPos);
            
            // Draw field
            var isHovered = DetectHover(fieldPos);
            var content = GetContentFromObject(obj, field.FieldType);
            var fieldStyle = new GUIStyle(EditorStyles.objectField);
            if (currentEventType == EventType.Repaint) {
                fieldStyle.Draw(fieldPos, content, id, HasDnD(id), isHovered);
            }
            // reduce trigger area of the field because covered by the picker icon
            fieldPos.width -= pickerWidth;
            
            // Draw picker
            var pickerPos = fieldPos;
            pickerPos.x += fieldPos.width;
            pickerPos.y += 0;
            pickerPos.width = pickerWidth;
            pickerPos.height -= 0;
            var pickerStyle = new GUIStyle(EditorStyles.helpBox);
            if (DetectHover(pickerPos)) {
                pickerStyle.normal.background = Texture2D.linearGrayTexture;
            }
            GUI.Box(pickerPos, "", pickerStyle);
            var icon = pickerPos;
            icon.x += 4;
            icon.y += 3;
            icon.width -= 8;
            icon.height -= 6;
            GUI.DrawTexture(icon, iconPick);

            ProcessFieldClickEvent(index, field, obj, fieldPos, pickerPos);

            pos.y += lineHeight;
            return pos.y - startY;
        }

        // --------------------------------- //
        #region SETUP

        public void Enable(Object target, FieldInfo iDepsField, string iDepsFieldPath) {
            if (isInit) return;
    
            // setup measurements
            lineHeight = EditorGUIUtility.singleLineHeight;
            verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

            this.target = target;
            actualTarget = InterfaceDependencies.GetTargetObjectRelativeToIDeps(target, iDepsFieldPath);
            
            BindInterfaceDependencies(iDepsField, iDepsFieldPath);
            EnumerateIFaceFields();
            isInit = true;
        }

        public void Disable() {
            isInit = false;
            target = null;
            actualTarget = null;
            currentEvent = null;
            iFaceFields.Clear();
        }

        void BindInterfaceDependencies(FieldInfo iDepsField, string iDepsFieldPath) {
            var obj = iDepsField.GetValue(actualTarget);
            switch (obj) {
                case null:
                    obj = new InterfaceDependencies(target, iDepsFieldPath);
                    iDepsField.SetValue(actualTarget, obj);
                    break;
                case InterfaceDependencies iDeps:
                    iDeps.SetTarget(target, iDepsFieldPath);
                    break;
            }
            iDeps = obj as InterfaceDependencies;
        }

        void EnumerateIFaceFields() {
            iFaceFields = new List<FieldInfo>();
            
            var type = actualTarget.GetType();
            foreach (var field in type.GetInterfaceFields()){
                iFaceFields.Add(field);
            }
        }

        #endregion

        // --------------------------------- //
        #region Event Processing

        void ReadEvent() {
            currentEvent = Event.current;
            currentEventType = currentEvent.type;
            if (!GUI.enabled && Event.current.rawType == EventType.MouseDown)
                currentEventType = Event.current.rawType;
        }

        bool DetectHover(Rect rect) {
            return rect.Contains(currentEvent.mousePosition) &&
                   (currentEventType != EventType.MouseDown || currentEventType != EventType.MouseUp);
        }

        bool HasKBFocus(int id) {
            return GUIUtility.keyboardControl == id;
        }

        bool HasDnD(int id) {
            return DragAndDrop.activeControlID == id;
        }

        void ProcessFocus(Rect rect, int id) {
            if (rect.Contains(currentEvent.mousePosition)) {
                if (Event.current.type == EventType.MouseDown) {
                    GUIUtility.hotControl = id;
                    GUIUtility.keyboardControl = id;
                } else if (Event.current.type == EventType.MouseDown){
                    GUIUtility.hotControl = 0;
                }
            }
        }
        
        void HandleMouseDown(Rect rect, Action<Vector2> action, bool use = true) {
            if (Event.current.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition)) {
                if (use) 
                    Event.current.Use();
                action(currentEvent.mousePosition);
            }
        }

        void ProcessFieldClickEvent(int index, FieldInfo field, Object obj, Rect fieldRect, Rect pickerRect) {
            if (currentEventType != EventType.MouseDown)
                return;

            var used = false;
            if (fieldRect.Contains(currentEvent.mousePosition)) {
                // Ping the object
                if (obj != null) {
                    // Selection.objects = new[] { obj };
                    EditorGUIUtility.PingObject(obj);
                }
                used = true;
            }

            if (pickerRect.Contains(currentEvent.mousePosition)) {
                OpenObjectPicker(index, field, obj);
                used = true;
            }

            if (used) {
                currentEvent.Use();
                GUIUtility.ExitGUI(); 
            }
        }
        
        void ProcessDragAndDrop(int id, FieldInfo field, Rect fieldRect) {
            switch (currentEventType) {
                case EventType.DragUpdated:
                case EventType.DragPerform: {
                    if (!fieldRect.Contains(currentEvent.mousePosition) || !GUI.enabled)
                        return;
                    
                    var dnd = DragAndDrop.objectReferences.FirstOrDefault();
                    if (dnd == null)
                        return;
                    
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    
                    var validatedDnD = FindComponentOrSO(field, dnd);
                    if (validatedDnD == null) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        dndObject = null;
                        return;
                    }

                    dndObject = validatedDnD;
                    
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.activeControlID = id;
                    GUI.changed = true;
                    Event.current.Use();
                    break;
                }
                case EventType.DragExited:
                    if (GUI.enabled) {
                        if (fieldRect.Contains(currentEvent.mousePosition) && dndObject != null) {
                            // object can be set
                            SetObjectToField(field, dndObject, false);
                            GUI.changed = true;
                            dndObject = null;
                        }
                        DragAndDrop.activeControlID = 0;
                        HandleUtility.Repaint();
                    }
                    break;
            }
        }
        
        #endregion
        
        // --------------------------------- //
        #region Process Object Reference
        
        void OpenObjectPicker(int index, FieldInfo field, Object obj) {
            var type = field != null ? field.FieldType : null;
            InterfaceSearchProvider.OpenInterfacePicker(type, (pickedObj) => {
                SetObjectToField(field, pickedObj);
                GUI.changed = true;
            });
        }

        void SetObjectToField(FieldInfo field, Object pickedObj, bool needValidation = true) {
            if (field == null)
                return;

            var val = needValidation ? FindComponentOrSO(field, pickedObj) : pickedObj;
                
            Undo.RecordObject(target, "Set interface field ref");
            field.SetValue(actualTarget, val);
        }

        Object GetMappedObjectForField(FieldInfo prop) {
            // TODO: or dnd
            return prop.GetValue(actualTarget) as Object;
        }

        GUIContent GetContentFromObject(Object obj, Type type) {
            var typeName = type.GetNameWithGenerics();
            var text = obj == null 
                ? $"None ({typeName})" 
                : $"{obj.name} ({obj.GetType().Name}) ({typeName})";
            var img = obj == null
                ? null
                : EditorGUIUtility.GetIconForObject(obj);
            return new GUIContent(text, img);
        }

        Object FindComponentOrSO(FieldInfo field, Object obj) {
            var gameObject = obj as GameObject;
            if (gameObject) {
                var cmp = gameObject.GetComponents<Component>().Where(c => {
                    // TODO: consider using field.FieldType.IsAssignableFrom(c.GetType())
                    var iList = c.GetType().GetInterfaces();
                    return iList.Any(i => i == field.FieldType);
                }).FirstOrDefault();
                return cmp;
            }
            var so = obj as ScriptableObject;
            if (so) {
                // TODO: consider using field.FieldType.IsAssignableFrom(so.GetType())
                var hasRequiredIFace = so.GetType().GetInterfaces().Any(i => i == field.FieldType);
                if (hasRequiredIFace) return so;
            }
            return null;
        }
        
        #endregion

        // --------------------------------- //
        #region CLEANUP

        void ReleaseUnmanagedResources() {
            Disable();
        }
        void Dispose(bool disposing) {
            ReleaseUnmanagedResources();
            if (disposing) {
                isFoldout?.Dispose();
            }
        }
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~InterfaceDependenciesDrawer() {
            Dispose(false);
        }
        
        #endregion
    }
}
