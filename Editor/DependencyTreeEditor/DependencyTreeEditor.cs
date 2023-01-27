using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LobstersUnited.HumbleDI.Editor {

    internal class DependencyTreeEditor : EditorWindow {
        
        [MenuItem("/Humble DI/Dependency Tree")]
        public static void Open() {
            var wnd = GetWindow<DependencyTreeEditor>();
            wnd.titleContent = new GUIContent("DependencyTreeEditor");
            wnd.Show();
        }

        [SerializeField] TreeViewState treeViewState;

        DependencyTreeView treeView;
    
        SearchField searchField;
        
        void OnEnable () {
            // Check whether there is already a serialized view state (state 
            // that survived assembly reloading)
            treeViewState ??= new TreeViewState();

            treeView = new DependencyTreeView(treeViewState);
            
            searchField = new SearchField();
            searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
        }
        
        void OnGUI() {
            DrawToolbar();
            var toolbarRect = GUILayoutUtility.GetLastRect();

            var searchRect = toolbarRect;
            searchRect.x = 4;
            searchRect.y += searchRect.height + DrawerUtils.verticalSpacing;
            searchRect.height = 20f;
            searchRect.width -= 8;
            DrawSearchBar(searchRect);

            var treeRect = searchRect;
            treeRect.x = 0;
            treeRect.y += treeRect.height;
            treeRect.height = position.height - toolbarRect.height - searchRect.height - DrawerUtils.verticalSpacing;
            treeRect.width = position.width;
            DrawTreeView(treeRect);
        }
        
        void OnSelectionChange() {
            treeView?.SetSelection(Selection.instanceIDs);
            Repaint();
        }

        void OnHierarchyChange() {
            treeView?.Reload();
            Repaint();
        }
        
        void DrawToolbar() {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        
        void DrawSearchBar(Rect rect) {
            treeView.searchString = searchField.OnGUI(rect, treeView.searchString);
        }

        void DrawTreeView(Rect rect) {
            treeView.OnGUI(rect);
        }
    }
    
}