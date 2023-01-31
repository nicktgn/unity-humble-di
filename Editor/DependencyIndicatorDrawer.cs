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
using UnityEditor;
using UnityEngine;

namespace LobstersUnited.HumbleDI.Editor {
    
    internal static class DependencyIndicatorDrawer {

        static Texture2D ICON_RESOLVED = Resources.Load("d_Resolved@64") as Texture2D;
        static Texture2D ICON_EXTERNAL = Resources.Load("d_External@64") as Texture2D;
        static Texture2D ICON_OPTIONAL = Resources.Load("d_Optional@64") as Texture2D;
        static Texture2D ICON_UNRESOLVED = Resources.Load("d_Unresolved@64") as Texture2D;
        
        static float ICON_SIZE = 10;
        
        static float ICON_Y = 4;
        static GUIContent SPEARATOR_LABEL = new GUIContent(" | ");
        static float SEPARATOR_X = EditorStyles.label.CalcSize(SPEARATOR_LABEL).x;
        static float SPACING_X = 2;
        
        public static void DrawShortStats(Rect rowRect, DependencyStats stats) {
            var hasOptional = stats.Optional > 0;
            var hasExternal = stats.External > 0;
            var hasUnresolved = stats.Unresolved > 0;
            var hasExternalOrOptional = hasExternal || hasOptional;

            var optionalLabel = new GUIContent(hasOptional ? stats.Optional.ToString() : string.Empty);
            var optionalWidth = hasOptional ? EditorStyles.label.CalcSize(optionalLabel).x : 0;

            var externalLabel = new GUIContent(hasExternal ? stats.External.ToString() : string.Empty);
            var externalWidth = hasExternal ? EditorStyles.label.CalcSize(externalLabel).x : 0;

            var externalOptionalWidth = 
                optionalWidth + (hasOptional ? ICON_SIZE + SPACING_X : 0) + 
                externalWidth + (hasExternal ? ICON_SIZE + SPACING_X : 0) + 
                (hasExternalOrOptional ? SEPARATOR_X : 0) + 
                (hasExternal && hasOptional ? SPACING_X : 0);
            
            var blockEndX = rowRect.x + rowRect.width;
            
            // draw resolved or unresolved;
            var unresolvedLabel = new GUIContent(hasUnresolved ? stats.Unresolved.ToString() : string.Empty);
            var unresolvedLabelWidth = EditorStyles.label.CalcSize(unresolvedLabel).x; 
            var iconRect = new Rect(
                blockEndX - externalOptionalWidth - unresolvedLabelWidth - ICON_SIZE - SPACING_X, 
                rowRect.y + ICON_Y, 
                ICON_SIZE, 
                ICON_SIZE
            );
            GUI.DrawTexture(iconRect, hasUnresolved ? ICON_UNRESOLVED : ICON_RESOLVED);
            
            Rect labelRect;
            if (hasUnresolved) {
                labelRect = new Rect(
                    blockEndX - externalOptionalWidth - unresolvedLabelWidth - (!hasExternalOrOptional ? 15 : 0),
                    rowRect.y,
                    unresolvedLabelWidth + (!hasExternalOrOptional ? 20 : 0),
                    rowRect.height
                );
                EditorGUI.LabelField(labelRect, unresolvedLabel);
            }

            // draw external and optional
            if (hasExternalOrOptional) {
                labelRect = new Rect(blockEndX - externalOptionalWidth, rowRect.y, SEPARATOR_X, rowRect.height);
                EditorGUI.LabelField(labelRect, SPEARATOR_LABEL);

                if (hasExternal) {
                    iconRect.x = labelRect.x + labelRect.width;
                    GUI.DrawTexture(iconRect, ICON_EXTERNAL);

                    labelRect.x = iconRect.x + iconRect.width + SPACING_X;
                    labelRect.width = externalWidth;
                    EditorGUI.LabelField(labelRect, externalLabel);
                }

                if (hasOptional) {
                    iconRect.x = labelRect.x + labelRect.width + (hasExternal ? SPACING_X : 0);
                    GUI.DrawTexture(iconRect, ICON_OPTIONAL);

                    labelRect.x = iconRect.x + iconRect.width + SPACING_X;
                    labelRect.width = externalWidth;
                    EditorGUI.LabelField(labelRect, externalLabel);
                }
            }
        }
        
        public static void DrawDependencyIndicator(Rect rowRect, DependencyManager dependencies, DependencyInfo dep) {
            var tex = !dependencies.IsDependencyResolved(dep, true)
                ? ICON_UNRESOLVED
                : dep.IsExternal
                    ? ICON_EXTERNAL
                    : dep.IsOptional
                        ? ICON_OPTIONAL
                        : null;

            if (!tex)
                return;

            var realIndent = Math.Max(EditorGUI.indentLevel - 1, 0);
            var indentWidth = realIndent * DrawerUtils.singleIndentSpacing;
            
            var iconRect = rowRect;
            iconRect.width = ICON_SIZE;
            iconRect.height = ICON_SIZE;
            iconRect.y += ICON_Y;
            iconRect.x += indentWidth + SPACING_X + 2;
            GUI.DrawTexture(iconRect, tex);
        }
    }
}
