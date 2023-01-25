using System;
using UnityEditor;
using UnityEngine;

namespace LobstersUnited.HumbleDI.Editor {

    public class DependencyTreeEditor : EditorWindow {
        [MenuItem("/Humble DI/Dependency Tree")]
        public static void Open() {
            var wnd = GetWindow<DependencyTreeEditor>();
            wnd.titleContent = new GUIContent("DependencyTreeEditor");
        }

        public void OnGUI() {
            
            // TODO: implement IMGUI version of the tree view first
            
        }
    }
    
}