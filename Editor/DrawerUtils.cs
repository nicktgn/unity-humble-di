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
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LobstersUnited.HumbleDI.Editor {
    
    internal static class DrawerUtils {
        
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
        
        public static bool IsRepaint => Event.current.type == EventType.Repaint;

        public static bool DetectHover(Rect rect) {
            var currentEvent = Event.current;
            return rect.Contains(currentEvent.mousePosition) && 
                   currentEvent.type != EventType.MouseDown && 
                   currentEvent.type != EventType.MouseUp;
        }

        public static bool HasKBFocus(int id) {
            return GUIUtility.keyboardControl == id;
        }

        public static bool HasDnD(int id) {
            return DragAndDrop.activeControlID == id;
        }
        
        public static bool ProcessFocus(Rect rect, int id, Action onFocus = null) {
            var currentEvent = Event.current;
            if (!rect.Contains(currentEvent.mousePosition))
                return false;
            if (currentEvent.type == EventType.MouseDown) {
                GUIUtility.hotControl = id;
                GUIUtility.keyboardControl = id;
                return true;
            } 
            if (currentEvent.type == EventType.MouseUp){
                GUIUtility.hotControl = 0;
            }
            return false;
        }
        
        public static Vector2? ProcessMouseDown(Rect rect, bool use = true) {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition)) {
                if (use) 
                    currentEvent.Use();
                return currentEvent.mousePosition;
            }
            return null;
        }

        public static void ProcessDragAndDrop(int id, Rect fieldRect, bool allowSceneObjects, Func<Object, Object> validateCb, Action<Object> onDrop) {
            var eventType = Event.current.type;
            switch (eventType) {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (fieldRect.Contains(Event.current.mousePosition) && GUI.enabled) {
                        Object obj = DragAndDrop.objectReferences.FirstOrDefault();
                        Object validatedObj = validateCb(obj);
                        
                        if (validatedObj != null) {
                            // If scene objects are not allowed and object is a scene object then clear
                            if (!allowSceneObjects && !EditorUtility.IsPersistent(validatedObj))
                                validatedObj = null;
                        }

                        if (validatedObj != null) {
                            if (DragAndDrop.visualMode == DragAndDropVisualMode.None)
                                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                            if (eventType == EventType.DragPerform) {
                                onDrop(validatedObj);

                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                            }
                            else {
                                DragAndDrop.activeControlID = id;
                            }
                            Event.current.Use();
                        }
                    }
                    break;
            }
        }
        
        public static float DrawSeparatorLine(Rect line) {
            var indent = DrawerUtils.IndentWidth;
            line.x += 15 + indent;
            line.width -= 16 + indent;
            line.height = 2.0f;
            EditorGUI.DrawRect(line, Color.gray);
            return line.height;
        }

        public static void DrawBox(Rect rect) {
            var indent = IndentWidth;
            
            // adjust for indent
            var boxPos = rect;
            boxPos.x += indent;
            boxPos.width -= indent;
            GUI.Box(boxPos, GUIContent.none, EditorStyles.helpBox);
        }

        public static bool DrawFoldout(Rect pos, bool value, string label) {
            var id = GUIUtility.GetControlID(FocusType.Keyboard, pos);
            
            var indent = IndentWidth;
            
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
            var labelClick = ProcessMouseDown(labelPos);
            if (labelClick != null) {
                result = !result;
            }

            // draw label
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            if (HasKBFocus(id)) {
                labelStyle.normal.textColor = EditorStyles.foldout.active.textColor;
            }
            EditorGUI.LabelField(labelPos, label, labelStyle);

            return result;
        }
    }
}
