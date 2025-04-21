using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Match3Game))]
public class Match3GameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Match3Game game = (Match3Game)target;

        if (GUILayout.Button("Print Board in Inspector"))
        {
            string board = "";
            for (int y = game.Size.y - 1; y >= 0; y--)
            {
                for (int x = 0; x < game.Size.x; x++)
                {
                    board += game[x, y].ToString().Substring(0, 1) + " ";
                }
                board += "\n";
            }
            Debug.Log($"[Board]\n{board}");
        }
    }
}
