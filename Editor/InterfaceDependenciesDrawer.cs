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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI.Editor {

    internal static class DrawUtils {
        
        public static Texture2D iconPick = (Texture2D) EditorGUIUtility.IconContent("d_pick").image;

        public static float lineHeight = EditorGUIUtility.singleLineHeight;
        public static float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
        public static float pickerWidth = lineHeight + 2f;

        public static float singleIndentSpacing = 15;
        
        public static float prefixPaddingRight = 2;
        
        public static float IndentWidth => EditorGUI.indentLevel * singleIndentSpacing;

        public static void GetPickerRect(Rect fieldRect, out Rect pickerRect, out Rect iconRect) {
            pickerRect = fieldRect;
            pickerRect.x += fieldRect.width - pickerWidth;
            pickerRect.y += 0;
            pickerRect.width = pickerWidth;
            pickerRect.height -= 0;
            
            iconRect = pickerRect;
            iconRect.x += 4;
            iconRect.y += 3;
            iconRect.width -= 8;
            iconRect.height -= 6;
        }
    }


    internal class ObjectManager {

        /// <summary>
        /// Unity Object target (target of the Inspector)
        /// </summary>
        public Object target;
       
        /// <summary>
        /// Actual c# object which contains InterfaceDependencies field. Can be nested inside of `target` or `target` itself
        ///  depending on the `SerializedProperty.propertyPath` 
        /// </summary>
        public object actualTarget;

        public ObjectManager(Object target, string iDepsFieldPath) {
            this.target = target;
            actualTarget = InterfaceDependencies.GetTargetObjectRelativeToIDeps(target, iDepsFieldPath);
        }
        
        public InterfaceDependencies BindInterfaceDependencies(FieldInfo iDepsField, string iDepsFieldPath) {
            var obj = iDepsField.GetValue(actualTarget);
            switch (obj) {
                case null:
                    Debug.Log("CREATE new Instance of InterfaceDependencies");
                    obj = new InterfaceDependencies(target, iDepsFieldPath);
                    iDepsField.SetValue(actualTarget, obj);
                    break;
                case InterfaceDependencies iDeps:
                    iDeps.SetTarget(target, iDepsFieldPath);
                    break;
            }
            return obj as InterfaceDependencies;
        }

        public void OpenObjectPicker(Type type, Action<Object> pickCallback) {
            InterfaceSearchProvider.OpenInterfacePicker(type, pickedObject => {
                var componentOrSO = Utils.FindComponentOrSO(type, pickedObject);
                pickCallback(componentOrSO);
            });
        }

        public void RecordUndo() {
            Undo.RecordObject(target, "Set interface field ref");
        }

        public void RecordUndoHierarchy() {
            Undo.RegisterFullObjectHierarchyUndo(target, "Set interface field list item");
        }

        public void SetObjectToField(FieldInfo field, Object pickedObj) {
            RecordUndo();
            field.SetValue(actualTarget, pickedObj);
        }

        public Object GetMappedObjectForField(FieldInfo field) {
            return field.GetValue(actualTarget) as Object;
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

    internal class EventManager {

        public Event currentEvent;
        public EventType currentEventType;
        
        public Object dndObject;

        public ObjectManager objectManager;

        public EventManager(ObjectManager objectManager) {
            this.objectManager = objectManager;
        }

        public void ReadEvent() {
            currentEvent = Event.current;
            currentEventType = Event.current.type;
            if (!GUI.enabled && Event.current.rawType == EventType.MouseDown)
                currentEventType = Event.current.rawType;
        }

        public bool IsRepaint => currentEventType == EventType.Repaint;

        public bool DetectHover(Rect rect) {
            return rect.Contains(currentEvent.mousePosition) &&
                   (currentEventType != EventType.MouseDown || currentEventType != EventType.MouseUp);
        }

        public bool HasKBFocus(int id) {
            return GUIUtility.keyboardControl == id;
        }

        public bool HasDnD(int id) {
            return DragAndDrop.activeControlID == id;
        }

        public void ProcessFocus(Rect rect, int id, Action onFocus = null) {
            if (rect.Contains(currentEvent.mousePosition)) {
                if (Event.current.type == EventType.MouseDown) {
                    GUIUtility.hotControl = id;
                    GUIUtility.keyboardControl = id;
                    onFocus?.Invoke();
                } else if (Event.current.type == EventType.MouseDown){
                    GUIUtility.hotControl = 0;
                }
            }
        }
        
        public void HandleMouseDown(Rect rect, Action<Vector2> action, bool use = true) {
            if (Event.current.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition)) {
                if (use) 
                    Event.current.Use();
                action(currentEvent.mousePosition);
            }
        }

        public void ProcessFieldClickEvent(Type fieldType, Object obj, Rect fieldRect, Rect pickerRect, Action<Object> pickCallback) {
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
                objectManager.OpenObjectPicker(fieldType, pickCallback);
                used = true;
            }

            if (used) {
                currentEvent.Use(); 
                // GUIUtility.ExitGUI(); 
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="matchingType"></param>
        /// <param name="fieldRect"></param>
        /// <param name="onDrop">passes validated Component or ScriptableObject if has one on drop</param>
        public void ProcessDragAndDrop(int id, Type matchingType, Rect fieldRect, Action<Object> onDrop) {
            switch (currentEventType) {
                case EventType.DragUpdated:
                case EventType.DragPerform: {
                    if (!fieldRect.Contains(currentEvent.mousePosition) || !GUI.enabled)
                        return;
                    
                    var dnd = DragAndDrop.objectReferences.FirstOrDefault();
                    if (dnd == null)
                        return;
                    
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    
                    var validatedDnD = Utils.FindComponentOrSO(matchingType, dnd);
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
                            onDrop?.Invoke(dndObject);
                            GUI.changed = true;
                            dndObject = null;
                        }
                        DragAndDrop.activeControlID = 0;
                        HandleUtility.Repaint();
                    }
                    break;
            }
        }
        
    }
    
    internal class ListFieldManager {

        EventManager eventManager;
        ObjectManager objectManager;

        ReorderableList gui;
        CollectionWrapper list;

        public ListFieldManager(FieldInfo field, object target, ObjectManager objectManager, EventManager eventManager) {
            list = new CollectionWrapper(field, target);

            gui = new ReorderableList(list, null, true, false, true, true) {
                drawElementCallback = DrawElement,
                onAddCallback = OnAdd,
                onRemoveCallback = OnRemove,
                onReorderCallbackWithDetails = OnReorder,
                onCanRemoveCallback = CanRemove,
                multiSelect = false,
            };

            this.objectManager = objectManager;
            this.eventManager = eventManager;
        }

        public void Dispose() {
            list.Clear();
            objectManager = null;
            eventManager = null;
            gui = null;
            list = null;
        }

        public float GetHeight() {
            return gui.GetHeight();
        }

        public void DrawList(Rect rect) {
            gui.DoList(rect);
        }

        void DrawElement(Rect rect, int index, bool active, bool focused) {
            var id = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            var itemType = list.ItemType;

            // adjust line height
            rect.y -= (rect.height - DrawUtils.lineHeight - DrawUtils.verticalSpacing) / 2;
            rect.height = DrawUtils.lineHeight + DrawUtils.verticalSpacing;

            var obj = list[index] as Object;
            
            // get measurements
            CalculateLabelWithPrefix(rect, out var labelPos, out var fieldPos);
            DrawUtils.GetPickerRect(fieldPos, out var pickerRect, out var iconRect);
            
            // handle events
            if (focused) {
                GUIUtility.keyboardControl = id;
            }
            eventManager.ProcessFocus(rect, id, () => {
                gui.Select(index);
            });
            eventManager.ProcessDragAndDrop(id, itemType, fieldPos, dropObject => {
                SetObjectAsListItem(dropObject, index);
            });

            // Draw index label
            var label = new GUIContent(index.ToString());
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            var isHovered = eventManager.DetectHover(labelPos);
            if (eventManager.IsRepaint) {
                labelStyle.Draw(labelPos, label, id, false, isHovered);
            }

            // Draw field
            isHovered = eventManager.DetectHover(fieldPos);
            var content = objectManager.GetContentFromObject(obj, itemType);
            var fieldStyle = new GUIStyle(EditorStyles.objectField);
            if (eventManager.IsRepaint) {
                fieldStyle.Draw(fieldPos, content, id, false, isHovered);
            }
            
            // Draw picker
            var pickerStyle = new GUIStyle(EditorStyles.helpBox);
            if (eventManager.DetectHover(pickerRect)) {
                pickerStyle.normal.background = Texture2D.linearGrayTexture;
            }
            GUI.Box(pickerRect, "", pickerStyle);
            GUI.DrawTexture(iconRect, DrawUtils.iconPick);
            
            // reduce trigger area of the field because covered by the picker icon
            fieldPos.width -= DrawUtils.pickerWidth;
            eventManager.ProcessFieldClickEvent(itemType, obj, fieldPos, pickerRect, pickedObj => {
                SetObjectAsListItem(pickedObj, index);
                GUI.changed = true;
            });
        }

        void SetObjectAsListItem(Object obj, int index) {
            objectManager.RecordUndoHierarchy();
            list[index] = obj;
        }

        void CalculateLabelWithPrefix(Rect totalPosition, out Rect labelPosition, out Rect fieldPosition) {
            labelPosition = new Rect(totalPosition.x, totalPosition.y + 1, EditorGUIUtility.labelWidth, DrawUtils.lineHeight);
            fieldPosition = new Rect(
                totalPosition.x + EditorGUIUtility.labelWidth + DrawUtils.prefixPaddingRight, 
                totalPosition.y + 4, 
                totalPosition.width - EditorGUIUtility.labelWidth - DrawUtils.prefixPaddingRight, 
                DrawUtils.lineHeight
            );
            var unindent = DrawUtils.singleIndentSpacing * 3 + 5;
            fieldPosition.x -= unindent;
            fieldPosition.width += unindent;
            labelPosition.width -= unindent;
        }

        void OnAdd(ReorderableList reorderableList) {
            objectManager.RecordUndoHierarchy();
            list.Add(null);
        }

        void OnRemove(ReorderableList reorderableList) {
            objectManager.RecordUndoHierarchy();
            var idx = reorderableList.selectedIndices.FirstOrDefault();
            list.RemoveAt(idx);
        }

        void OnReorder(ReorderableList reorderableList, int index, int newIndex) {
            objectManager.RecordUndoHierarchy();
            list.Reorder(index, newIndex);
        }

        bool CanRemove(ReorderableList reorderableList) {
            return list.Count > 0;
        }
    }


    [CustomPropertyDrawer(typeof(InterfaceDependencies))]
    internal class InterfaceDependenciesDrawer : PropertyDrawer, IDisposable {

        float totalHeight;

        bool isInit;

        List<FieldInfo> iFaceFields;
        List<IFaceFieldCategory> iFaceFieldCategories;

        EventManager eventManager;
        ObjectManager objectManager;
        Dictionary<int, ListFieldManager> listManagers = new Dictionary<int, ListFieldManager>();

        SerializedProperty isFoldout;
        static readonly string isFoldoutPropName = "isFoldout";
        InterfaceDependencies iDeps;
        

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
            eventManager.ReadEvent();

            DrawDependenciesSectionGUI(position);
        }
        
        float CalculateDependencySectionHeight() {
            var lineHeight = DrawUtils.lineHeight;
            var verticalSpacing = DrawUtils.verticalSpacing;
            var headerHeight = lineHeight + verticalSpacing * 2 + 2;
            var singularFieldsHeight = (iFaceFields.Count - listManagers.Count) * (lineHeight + verticalSpacing);
            var listFieldsHeight = 0.0f;
            foreach (var list in listManagers.Values) {
                listFieldsHeight += list.GetHeight();
            }
            return headerHeight + singularFieldsHeight + listFieldsHeight;
        }

        float CalculateListHeight(ReorderableList list) {
            Debug.Log(list.GetHeight());
            var elemNum = list.count > 0 ? list.count : 1;
            return elemNum * list.elementHeight + list.footerHeight;
        }

        void DrawDependenciesSectionGUI(Rect pos) {
            var startY = pos.y; // pos.yMin;
            
            pos.y += DrawUtils.verticalSpacing;

            var sectionHeight = CalculateDependencySectionHeight();

            var foldout = isFoldout.boolValue;
            var foldoutPos = pos;
            foldoutPos.height = foldout ? sectionHeight : DrawUtils.lineHeight;
            foldout = DrawFoldout(foldoutPos, foldout, "Dependencies");
            pos.y += DrawUtils.lineHeight;
            
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
            
            var indent = DrawUtils.IndentWidth;
            var lineHeight = DrawUtils.lineHeight;

            // adjust for indent
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

            eventManager.ProcessFocus(pos, id);

            // process click on the label
            eventManager.HandleMouseDown(labelPos, mousePos => {
                result = !result;
            });

            // draw label
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            if (eventManager.HasKBFocus(id)) {
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
            var indent = DrawUtils.IndentWidth;
            line.x += 15 + indent;
            line.width -= 16 + indent;
            line.height = 2.0f;
            EditorGUI.DrawRect(line, Color.gray);
            return line.height;
        }

        float DrawDependenciesSectionContentGUI(Rect pos) {
            var startY = pos.y;
            var verticalSpacing = DrawUtils.verticalSpacing;
            
            pos.y += verticalSpacing;
            pos.y += DrawSeparatorLine(pos);
            pos.y += verticalSpacing;
            
            EditorGUI.indentLevel += 1;
            
            for (var i = 0; i < iFaceFields.Count; i++) {
                var field = iFaceFields[i];
                var cat = iFaceFieldCategories[i];
                if (cat == IFaceFieldCategory.SINGULAR) {
                    pos.y += DrawInterfacePropertyGUI(pos, i, field, objectManager.GetMappedObjectForField(field));
                } else {
                    pos.y += DrawInterfaceListGUI(pos, i, field);
                }
                pos.y += verticalSpacing;
            }
            
            EditorGUI.indentLevel -= 1;
            
            return pos.y - startY;
        }

        float DrawInterfaceListGUI(Rect pos, int index, FieldInfo listField) {
            // var id = GUIUtility.GetControlID(FocusType.Keyboard, pos);
            var startY = pos.y;

            var listMgr = listManagers.GetValueOrDefault(index);
            if (listMgr == null) {
                return pos.y - startY;
            }

            var indentWidth = DrawUtils.IndentWidth;
            pos.x += indentWidth;
            pos.width -= indentWidth;

            listMgr.DrawList(pos);
            pos.y += listMgr.GetHeight();

            return pos.y - startY;
        }

        float DrawInterfacePropertyGUI(Rect pos, int index, FieldInfo field, Object obj) {
            var id = GUIUtility.GetControlID(FocusType.Keyboard, pos);
            var startY = pos.y;

            var lineHeight = DrawUtils.lineHeight;
            var pickerWidth = DrawUtils.pickerWidth;

            pos.height = lineHeight;
    
            // process focus
            eventManager.ProcessFocus(pos, id);
            
            // Draw label
            var label = ObjectNames.NicifyVariableName(field.Name);
            var labelPos = pos;
            var labelStyle = new GUIStyle(EditorStyles.label);
            
            if (eventManager.HasKBFocus(id)) {
                labelStyle.normal = EditorStyles.label.focused;
            }
            var fieldPos = EditorGUI.PrefixLabel(labelPos, id, new GUIContent(label), labelStyle);
            labelPos.width = EditorGUIUtility.labelWidth;
            
            eventManager.ProcessDragAndDrop(id, field.FieldType, fieldPos, dropObject => {
                // drop object is already a validated container or scriptable object here 
                objectManager.SetObjectToField(field, dropObject);
            });

            // Draw field
            var isHovered = eventManager.DetectHover(fieldPos);
            var content = objectManager.GetContentFromObject(obj, field.FieldType);
            var fieldStyle = new GUIStyle(EditorStyles.objectField);
            if (eventManager.IsRepaint) {
                fieldStyle.Draw(fieldPos, content, id, eventManager.HasDnD(id), isHovered);
            }
            
            // Draw picker
            DrawUtils.GetPickerRect(fieldPos, out var pickerRect, out var iconRect);
            var pickerStyle = new GUIStyle(EditorStyles.helpBox);
            if (eventManager.DetectHover(pickerRect)) {
                pickerStyle.normal.background = Texture2D.linearGrayTexture;
            }
            GUI.Box(pickerRect, "", pickerStyle);
            GUI.DrawTexture(iconRect, DrawUtils.iconPick);

            // reduce trigger area of the field because covered by the picker icon
            fieldPos.width -= pickerWidth;
            eventManager.ProcessFieldClickEvent(field.FieldType, obj, fieldPos, pickerRect, pickedObj => {
                objectManager.SetObjectToField(field, pickedObj);
                GUI.changed = true;
            });

            pos.y += lineHeight;
            return pos.y - startY;
        }

        // --------------------------------- //
        #region SETUP

        public void Enable(Object target, FieldInfo iDepsField, string iDepsFieldPath) {
            if (isInit) return;

            objectManager = new ObjectManager(target, iDepsFieldPath);
            eventManager = new EventManager(objectManager);
            iDeps = objectManager.BindInterfaceDependencies(iDepsField, iDepsFieldPath);
            
            EnumerateIFaceFields();
            isInit = true;
        }

        public void Disable() {
            isInit = false;
            objectManager = null;
            eventManager = null;
            
            iFaceFields.Clear();
            iFaceFieldCategories.Clear();

            foreach (var mgr in listManagers.Values) {
                mgr.Dispose();
            }
            listManagers.Clear();
        }
        

        void EnumerateIFaceFields() {
            iFaceFields = new List<FieldInfo>();
            iFaceFieldCategories = new List<IFaceFieldCategory>();
            
            var type = objectManager.actualTarget.GetType();
            var count = 0;
            foreach (var field in InterfaceDependencies.GetCompatibleFields(type)){
                iFaceFields.Add(field);
                
                var cat = InterfaceDependencies.GetIFaceFieldTypeCategory(field.FieldType);
                iFaceFieldCategories.Add(cat);
                if (cat is IFaceFieldCategory.LIST or IFaceFieldCategory.ARRAY) {
                    var listMgr = new ListFieldManager(field, objectManager.actualTarget, objectManager, eventManager);
                    listManagers.Add(count, listMgr);
                }
                count++;
            }
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
