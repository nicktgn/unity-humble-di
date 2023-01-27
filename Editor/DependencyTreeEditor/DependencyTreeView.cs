using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobstersUnited.HumbleDI.Editor {
    
    internal class DependencyTreeView : TreeView {

        public DependencyTreeView(TreeViewState state) : base(state) {
	        Reload();
        }
        
        // -------------------------------- //
        #region TreeView overrides
        
        protected override TreeViewItem BuildRoot() {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            return root;
        }
        
		protected override IList<TreeViewItem> BuildRows(TreeViewItem root) {
			var rows = GetRows() ?? new List<TreeViewItem> (200);
			
			Scene scene = SceneManager.GetSceneAt(0);

			// We use the GameObject instanceIDs as ids for items as we want to 
			// select the game objects and not the transform components.
			rows.Clear ();
			var gameObjectRoots = scene.GetRootGameObjects();
			foreach (var gameObject in gameObjectRoots) {
				var item = CreateTreeViewItemForGameObject(gameObject);
				root.AddChild(item);
				rows.Add(item);
				if (gameObject.transform.childCount > 0) {
					if (IsExpanded(item.id)) {
						AddChildrenRecursive(gameObject, item, rows);
					}
					else {
						item.children = CreateChildListForCollapsedParent();
					}
				}
			}

			SetupDepthsFromParentsAndChildren(root);
			return rows;
		}

		void AddGameObjectRecursive(TreeViewItem parent, GameObject go, IList<TreeViewItem> rows) {
			var item = CreateTreeViewItemForGameObject(go);
			parent.AddChild(item); 
			rows.Add(item);
		}

		void AddChildrenRecursive(GameObject go, TreeViewItem item, IList<TreeViewItem> rows) {
			int childCount = go.transform.childCount;

			item.children = new List<TreeViewItem>(childCount);
			for (int i = 0; i < childCount; ++i) {
				var childTransform = go.transform.GetChild(i);
				var childItem = CreateTreeViewItemForGameObject(childTransform.gameObject);
				item.AddChild(childItem);
				rows.Add(childItem);

				if (childTransform.childCount > 0) {
					if (IsExpanded(childItem.id)) {
						AddChildrenRecursive(childTransform.gameObject, childItem, rows);
					}
					else {
						childItem.children = CreateChildListForCollapsedParent();
					}
				}
			}
		}

		protected override IList<int> GetAncestors(int id) {
			// The backend needs to provide us with this info since the item with id
			// may not be present in the rows
			var transform = GetGameObject(id).transform;

			List<int> ancestors = new List<int>();
			while (transform.parent != null) {
				var parent = transform.parent;
				ancestors.Add(parent.gameObject.GetInstanceID());
				transform = parent;
			}

			return ancestors;
		}

		protected override IList<int> GetDescendantsThatHaveChildren(int id) {
			Stack<Transform> stack = new Stack<Transform>();

			var start = GetGameObject(id).transform;
			stack.Push(start);

			var parents = new List<int>();
			while (stack.Count > 0) {
				Transform current = stack.Pop();
				parents.Add(current.gameObject.GetInstanceID());
				for (int i = 0; i < current.childCount; ++i) {
					if (current.childCount > 0)
						stack.Push(current.GetChild(i));
				}
			}

			return parents;
		}
		
		// Custom GUI
		protected override void RowGUI(RowGUIArgs args) {
			Event evt = Event.current;
			extraSpaceBeforeIconAndLabel = 18f;

			// GameObject isStatic toggle 
			var gameObject = GetGameObject(args.item.id);
			if (gameObject == null)
				return;

			Rect toggleRect = args.rowRect;
			toggleRect.x += GetContentIndent(args.item);
			toggleRect.width = 16f;

			// Ensure row is selected before using the toggle (usability)
			if (evt.type == EventType.MouseDown && toggleRect.Contains(evt.mousePosition))
				SelectionClick(args.item, false);
			
			EditorGUI.BeginChangeCheck();
			bool isStatic = EditorGUI.Toggle(toggleRect, gameObject.isStatic); 
			if (EditorGUI.EndChangeCheck ())
				gameObject.isStatic = isStatic;

			// Text
			base.RowGUI(args);
		}

		// Selection
		protected override void SelectionChanged(IList<int> selectedIds) {
			Selection.instanceIDs = selectedIds.ToArray();
		}
		
		#endregion

		// ------------------------------- //
		#region Private methods

		static TreeViewItem CreateTreeViewItemForGameObject(GameObject gameObject) {
			// We can use the GameObject instanceID for TreeViewItem id, as it ensured to be unique among other items in the tree.
			// To optimize reload time we could delay fetching the transform.name until it used for rendering (prevents allocating strings 
			// for items not rendered in large trees)
			// We just set depth to -1 here and then call SetupDepthsFromParentsAndChildren at the end of BuildRootAndRows to set the depths.
			return new TreeViewItem(gameObject.GetInstanceID(), -1, gameObject.name);
		}
		
		static GameObject GetGameObject (int instanceID) {
			return (GameObject) EditorUtility.InstanceIDToObject(instanceID);
		}

		#endregion
    }
}
